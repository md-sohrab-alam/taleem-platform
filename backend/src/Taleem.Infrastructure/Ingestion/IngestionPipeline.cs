using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Abstractions.Storage;
using Taleem.Application.Common;
using Taleem.Application.DocumentIr;
using Taleem.Domain.Entities;
using Taleem.Domain.Enums;
using Taleem.Infrastructure.Persistence;

namespace Taleem.Infrastructure.Ingestion;

public sealed class IngestionPipeline : IIngestionPipeline
{
    private readonly TaleemDbContext _db;
    private readonly IObjectStorage _storage;
    private readonly IDocumentExtractor _extractor;
    private readonly IBookNormalizer _normalizer;
    private readonly IDocumentStructureAnalyzer _structureAnalyzer;
    private readonly IChunkGenerator _chunkGenerator;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<IngestionPipeline> _logger;

    public IngestionPipeline(
        TaleemDbContext db,
        IObjectStorage storage,
        IDocumentExtractor extractor,
        IBookNormalizer normalizer,
        IDocumentStructureAnalyzer structureAnalyzer,
        IChunkGenerator chunkGenerator,
        IEmbeddingService embeddingService,
        ILogger<IngestionPipeline> logger)
    {
        _db = db;
        _storage = storage;
        _extractor = extractor;
        _normalizer = normalizer;
        _structureAnalyzer = structureAnalyzer;
        _chunkGenerator = chunkGenerator;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid ingestionJobId, CancellationToken cancellationToken = default)
    {
        var job = await _db.IngestionJobs
            .Include(j => j.SourceDocument)
            .ThenInclude(s => s.Book)
            .FirstOrDefaultAsync(j => j.Id == ingestionJobId, cancellationToken)
            ?? throw new InvalidOperationException("Ingestion job not found.");

        var source = job.SourceDocument;
        var book = source.Book;

        job.Status = IngestionJobStatus.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        job.ProgressPercent = 5;
        job.ProgressMessage = "Starting extraction";
        book.Status = ContentLifecycleStatus.Processing;
        book.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        if (book.Status == ContentLifecycleStatus.Published)
        {
            job.Status = IngestionJobStatus.Failed;
            job.Error = "Cannot reprocess a published book without unpublishing first. Published structure is preserved.";
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.ProgressMessage = "Rejected — book is published";
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            await ClearPreviousStructuredContentAsync(book.Id, source.Id, cancellationToken);

            await using var stream = await _storage.OpenReadAsync(source.StorageKey, cancellationToken);
            job.ProgressMessage = "Extracting PDF pages";
            job.ProgressPercent = 15;
            await _db.SaveChangesAsync(cancellationToken);

            var extraction = await _extractor.ExtractAsync(stream, source.MimeType, cancellationToken);
            source.PageCount = extraction.PageCount;

            foreach (var page in extraction.Pages)
            {
                var normalized = _normalizer.Normalize(page);
                _db.ExtractedPages.Add(new ExtractedPage
                {
                    SourceDocumentId = source.Id,
                    PageNumber = page.PageNumber,
                    RawText = page.RawText,
                    NormalizedText = normalized.NormalizedText,
                    ExtractionConfidence = page.Confidence
                });
            }

            job.ProgressMessage = "Analyzing layout structure";
            job.ProgressPercent = 40;
            await _db.SaveChangesAsync(cancellationToken);

            var structure = await _structureAnalyzer.AnalyzeAsync(book.Title, extraction.Layout, cancellationToken);

            var sectionMap = new Dictionary<int, Guid>();
            foreach (var s in structure.Sections)
            {
                var entity = new Section
                {
                    BookId = book.Id,
                    Title = s.Title,
                    OrderIndex = s.OrderIndex,
                    ParentSectionId = s.ParentOrderIndex is int p && sectionMap.TryGetValue(p, out var pid) ? pid : null
                };
                _db.Sections.Add(entity);
                sectionMap[s.OrderIndex] = entity.Id;
            }

            await _db.SaveChangesAsync(cancellationToken);

            var chapterMap = new Dictionary<int, Chapter>();
            foreach (var c in structure.Chapters)
            {
                var entity = new Chapter
                {
                    BookId = book.Id,
                    SectionId = c.SectionOrderIndex is int si && sectionMap.TryGetValue(si, out var sid) ? sid : null,
                    Title = c.Title,
                    Description = c.Description,
                    Slug = SlugHelper.Slugify(c.Title),
                    OrderIndex = c.OrderIndex,
                    SourcePageStart = c.SourcePageStart,
                    SourcePageEnd = c.SourcePageEnd
                };
                _db.Chapters.Add(entity);
                chapterMap[c.OrderIndex] = entity;
            }

            await _db.SaveChangesAsync(cancellationToken);

            job.ProgressMessage = "Creating units and blocks";
            job.ProgressPercent = 60;
            await _db.SaveChangesAsync(cancellationToken);

            var topicCache = await _db.Topics.ToDictionaryAsync(t => t.Slug, t => t, cancellationToken);
            var searchChunks = new List<(SearchChunk Entity, string Text)>();

            foreach (var unit in structure.Units)
            {
                if (!chapterMap.TryGetValue(unit.ChapterOrderIndex, out var chapter))
                    continue;

                var unitEntity = new TaleemUnit
                {
                    BookId = book.Id,
                    ChapterId = chapter.Id,
                    Title = unit.Title,
                    EstimatedReadingSeconds = unit.EstimatedReadingSeconds,
                    SourcePageStart = unit.SourcePageStart,
                    SourcePageEnd = unit.SourcePageEnd,
                    OrderIndex = unit.OrderIndex,
                    ReviewStatus = unit.NeedsReview ? ReviewStatus.NeedsEdits : ReviewStatus.Pending,
                    StructureConfidence = unit.StructureConfidence
                };
                _db.TaleemUnits.Add(unitEntity);
                await _db.SaveChangesAsync(cancellationToken);

                foreach (var block in unit.Blocks)
                {
                    _db.ContentBlocks.Add(new ContentBlock
                    {
                        Id = block.Id == Guid.Empty ? Guid.NewGuid() : block.Id,
                        BookId = book.Id,
                        TaleemUnitId = unitEntity.Id,
                        BlockType = block.BlockType,
                        Language = block.Language,
                        Text = block.Text,
                        OrderIndex = block.OrderIndex,
                        SourcePage = block.SourcePage,
                        SourceReference = block.SourceReference,
                        VerificationStatus = block.VerificationStatus
                    });
                }

                foreach (var topicName in unit.TopicNames)
                {
                    var slug = SlugHelper.Slugify(topicName);
                    if (!topicCache.TryGetValue(slug, out var topic))
                    {
                        topic = new Topic { Slug = slug, DisplayName = topicName };
                        _db.Topics.Add(topic);
                        await _db.SaveChangesAsync(cancellationToken);
                        topicCache[slug] = topic;
                    }

                    _db.TaleemUnitTopics.Add(new TaleemUnitTopic
                    {
                        TaleemUnitId = unitEntity.Id,
                        TopicId = topic.Id
                    });
                }

                var chunks = _chunkGenerator.Generate(
                    unit,
                    book.Id,
                    chapter.Id,
                    unitEntity.Id,
                    IngestionQualityEvaluator.ProcessorVersion);
                foreach (var chunk in chunks)
                {
                    var entity = new SearchChunk
                    {
                        BookId = book.Id,
                        TaleemUnitId = unitEntity.Id,
                        ChunkText = chunk.ChunkText,
                        Language = chunk.Language,
                        ChunkIndex = chunk.ChunkIndex,
                        MetadataJson = chunk.MetadataJson
                    };
                    _db.SearchChunks.Add(entity);
                    searchChunks.Add((entity, chunk.ChunkText));
                }

                await _db.SaveChangesAsync(cancellationToken);
            }

            job.ProgressMessage = "Generating embeddings";
            job.ProgressPercent = 80;
            await _db.SaveChangesAsync(cancellationToken);

            const int batchSize = 16;
            for (var i = 0; i < searchChunks.Count; i += batchSize)
            {
                var batch = searchChunks.Skip(i).Take(batchSize).ToList();
                var vectors = await _embeddingService.EmbedBatchAsync(batch.Select(b => b.Text).ToList(), cancellationToken);
                for (var j = 0; j < batch.Count; j++)
                    batch[j].Entity.Embedding = vectors[j];
                await _db.SaveChangesAsync(cancellationToken);
            }

            var report = structure.QualityReport ?? IngestionQualityEvaluator.Build(
                extraction.Layout,
                structure,
                structure.ExcludedFrontMatter,
                structure.HeadingCandidateCount,
                searchChunks.Count);
            report = new IngestionQualityReport
            {
                ProcessorVersion = IngestionQualityEvaluator.ProcessorVersion,
                PageCount = report.PageCount,
                SectionCount = report.SectionCount,
                ChapterCount = report.ChapterCount,
                UnitCount = report.UnitCount,
                ContentBlockCount = report.ContentBlockCount,
                SearchChunkCount = searchChunks.Count,
                TocEntryCount = report.TocEntryCount,
                ExcludedFrontMatterCount = report.ExcludedFrontMatterCount,
                HeadingCandidateCount = report.HeadingCandidateCount,
                LowConfidenceItemCount = report.LowConfidenceItemCount,
                HeadingOnlyUnitCount = report.HeadingOnlyUnitCount,
                EmptyBodyUnitCount = report.EmptyBodyUnitCount,
                UnitsPerPageAverage = report.UnitsPerPageAverage,
                HeadingOnlyUnitRatio = report.HeadingOnlyUnitRatio,
                FrontMatterOriginRatio = report.FrontMatterOriginRatio,
                HeadingConfidenceMin = report.HeadingConfidenceMin,
                HeadingConfidenceMax = report.HeadingConfidenceMax,
                HeadingConfidenceAverage = report.HeadingConfidenceAverage,
                StructureConfidenceMin = report.StructureConfidenceMin,
                StructureConfidenceMax = report.StructureConfidenceMax,
                StructureConfidenceAverage = report.StructureConfidenceAverage,
                HasStructuralAnomalies = report.HasStructuralAnomalies,
                AnomalyFlags = report.AnomalyFlags,
                DocumentZones = report.DocumentZones,
                TocEntries = report.TocEntries,
                ExcludedFrontMatter = report.ExcludedFrontMatter,
                SuspiciousUnitTitles = report.SuspiciousUnitTitles,
                UnitsPerPageDistribution = report.UnitsPerPageDistribution
            };

            book.Status = ContentLifecycleStatus.NeedsReview;
            book.ContentVersion += 1;
            book.UpdatedAt = DateTimeOffset.UtcNow;
            job.Status = IngestionJobStatus.Succeeded;
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.ProgressPercent = 100;
            job.ProcessorVersion = IngestionQualityEvaluator.ProcessorVersion;
            job.ReportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            job.ProgressMessage = report.HasStructuralAnomalies
                ? "Completed — ready for review (structural anomalies flagged)"
                : "Completed — ready for review";
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Ingestion job {JobId} succeeded for book {BookId}", job.Id, book.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion job {JobId} failed", job.Id);
            job.Status = IngestionJobStatus.Failed;
            job.Error = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.ProgressMessage = "Failed";
            book.Status = ContentLifecycleStatus.Failed;
            book.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task ClearPreviousStructuredContentAsync(Guid bookId, Guid sourceDocumentId, CancellationToken ct)
    {
        var chunks = _db.SearchChunks.Where(c => c.BookId == bookId);
        _db.SearchChunks.RemoveRange(chunks);

        var unitTopics = _db.TaleemUnitTopics.Where(t => t.TaleemUnit.BookId == bookId);
        _db.TaleemUnitTopics.RemoveRange(unitTopics);

        var blocks = _db.ContentBlocks.Where(b => b.BookId == bookId);
        _db.ContentBlocks.RemoveRange(blocks);

        var units = _db.TaleemUnits.Where(u => u.BookId == bookId);
        _db.TaleemUnits.RemoveRange(units);

        var chapters = _db.Chapters.Where(c => c.BookId == bookId);
        _db.Chapters.RemoveRange(chapters);

        var sections = _db.Sections.Where(s => s.BookId == bookId);
        _db.Sections.RemoveRange(sections);

        var pages = _db.ExtractedPages.Where(p => p.SourceDocumentId == sourceDocumentId);
        _db.ExtractedPages.RemoveRange(pages);

        await _db.SaveChangesAsync(ct);
    }
}
