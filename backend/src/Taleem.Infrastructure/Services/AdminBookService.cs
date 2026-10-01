using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Abstractions.Storage;
using Taleem.Application.Common;
using Taleem.Application.DocumentIr;
using Taleem.Application.DTOs;
using Taleem.Application.Mapping;
using Taleem.Application.Options;
using Taleem.Application.Services;
using Taleem.Domain.Entities;
using Taleem.Domain.Enums;
using Taleem.Infrastructure.Ingestion;
using Taleem.Infrastructure.Persistence;

namespace Taleem.Infrastructure.Services;

public sealed class AdminBookService : IAdminBookService
{
    private readonly TaleemDbContext _db;
    private readonly IObjectStorage _storage;
    private readonly IBookPublisher _publisher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<AdminBookService> _logger;

    public AdminBookService(
        TaleemDbContext db,
        IObjectStorage storage,
        IBookPublisher publisher,
        IServiceScopeFactory scopeFactory,
        IOptions<StorageOptions> storageOptions,
        ILogger<AdminBookService> logger)
    {
        _db = db;
        _storage = storage;
        _publisher = publisher;
        _scopeFactory = scopeFactory;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdminBookDto>> ListBooksAsync(CancellationToken ct = default)
    {
        var ids = await _db.Books.AsNoTracking().OrderByDescending(b => b.UpdatedAt).Select(b => b.Id).ToListAsync(ct);
        var result = new List<AdminBookDto>();
        foreach (var id in ids)
        {
            var mapped = await MapAdminBookAsync(id, ct);
            if (mapped is not null) result.Add(mapped);
        }
        return result;
    }

    public async Task<AdminBookDto> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default)
    {
        var slug = await EnsureUniqueSlugAsync(SlugHelper.Slugify(request.Slug ?? request.Title), ct);
        var book = new Book
        {
            Title = request.Title.Trim(),
            Subtitle = request.Subtitle?.Trim(),
            Author = request.Author?.Trim(),
            Description = request.Description?.Trim(),
            Slug = slug,
            DefaultLanguage = string.IsNullOrWhiteSpace(request.DefaultLanguage) ? "en" : request.DefaultLanguage.Trim(),
            Status = ContentLifecycleStatus.Uploaded
        };
        _db.Books.Add(book);
        await _db.SaveChangesAsync(ct);
        return await MapAdminBookAsync(book.Id, ct) ?? throw new InvalidOperationException("Failed to create book.");
    }

    public async Task<AdminBookDto?> GetBookAsync(Guid bookId, CancellationToken ct = default)
        => await MapAdminBookAsync(bookId, ct);

    public async Task<AdminBookDto?> UpdateBookAsync(Guid bookId, UpdateBookRequest request, CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct);
        if (book is null) return null;
        book.Title = request.Title.Trim();
        book.Subtitle = request.Subtitle?.Trim();
        book.Author = request.Author?.Trim();
        book.Description = request.Description?.Trim();
        book.CoverUrl = request.CoverUrl;
        book.DefaultLanguage = request.DefaultLanguage;
        book.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await MapAdminBookAsync(bookId, ct);
    }

    public async Task<SourceDocumentDto> UploadSourceAsync(
        Guid bookId,
        Stream content,
        string originalFilename,
        string contentType,
        string? sourceUrl,
        CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct)
                   ?? throw new InvalidOperationException("Book not found.");

        var safeName = Path.GetFileName(originalFilename);
        var ext = Path.GetExtension(safeName).ToLowerInvariant();
        if (!_storageOptions.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("File type not allowed.");

        if (!_storageOptions.AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
            && !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MIME type not allowed.");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length > _storageOptions.MaxUploadBytes)
            throw new InvalidOperationException($"File exceeds max size of {_storageOptions.MaxUploadBytes} bytes.");

        buffer.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(buffer, ct)).ToLowerInvariant();
        buffer.Position = 0;
        var storageKey = await _storage.SaveAsync(buffer, ext, ct);

        var doc = new SourceDocument
        {
            BookId = bookId,
            OriginalFilename = safeName,
            SourceUrl = sourceUrl,
            StorageKey = storageKey,
            MimeType = string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType,
            FileHash = hash,
            FileSizeBytes = buffer.Length
        };
        _db.SourceDocuments.Add(doc);
        book.Status = ContentLifecycleStatus.Uploaded;
        book.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Uploaded source {SourceId} for book {BookId}", doc.Id, bookId);
        return ToSourceDto(doc);
    }

    public async Task<IngestionJobDto> StartIngestionAsync(Guid bookId, Guid sourceDocumentId, CancellationToken ct = default)
    {
        var source = await _db.SourceDocuments.Include(s => s.Book)
                         .FirstOrDefaultAsync(s => s.Id == sourceDocumentId && s.BookId == bookId, ct)
                     ?? throw new InvalidOperationException("Source document not found for book.");

        if (source.Book.Status == ContentLifecycleStatus.Published)
            throw new InvalidOperationException(
                "Cannot reprocess a published book. Unpublish first so the live published structure is not wiped.");

        var job = new IngestionJob
        {
            SourceDocumentId = source.Id,
            Status = IngestionJobStatus.Queued,
            ProgressMessage = "Queued",
            ProcessorVersion = IngestionQualityEvaluator.ProcessorVersion
        };
        _db.IngestionJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        var jobId = job.Id;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<IIngestionPipeline>();
                await pipeline.ProcessAsync(jobId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background ingestion failed for job {JobId}", jobId);
            }
        }, CancellationToken.None);

        return await MapJobAsync(job.Id, ct) ?? throw new InvalidOperationException("Job missing.");
    }

    public async Task<IngestionJobDto?> GetJobAsync(Guid jobId, CancellationToken ct = default)
        => await MapJobAsync(jobId, ct);

    public async Task<IReadOnlyList<IngestionJobDto>> ListJobsAsync(Guid bookId, CancellationToken ct = default)
    {
        var jobs = await _db.IngestionJobs.AsNoTracking()
            .Where(j => j.SourceDocument.BookId == bookId)
            .OrderByDescending(j => j.StartedAt ?? DateTimeOffset.MinValue)
            .Select(j => new { Job = j, j.SourceDocument.BookId })
            .ToListAsync(ct);

        return jobs.Select(j => new IngestionJobDto(
            j.Job.Id, j.Job.SourceDocumentId, j.BookId, StatusNames.Job(j.Job.Status),
            j.Job.StartedAt, j.Job.CompletedAt, j.Job.Error, j.Job.ProgressMessage, j.Job.ProgressPercent,
            j.Job.ProcessorVersion, j.Job.ReportJson)).ToList();
    }

    public async Task<ReviewBookDto?> GetReviewAsync(Guid bookId, CancellationToken ct = default)
    {
        var book = await MapAdminBookAsync(bookId, ct);
        if (book is null) return null;

        var sources = await _db.SourceDocuments.AsNoTracking().Where(s => s.BookId == bookId).ToListAsync(ct);
        var jobs = await ListJobsAsync(bookId, ct);
        var sections = await _db.Sections.AsNoTracking().Where(s => s.BookId == bookId).OrderBy(s => s.OrderIndex)
            .Select(s => new SectionDto(s.Id, s.ParentSectionId, s.Title, s.OrderIndex)).ToListAsync(ct);
        var chapters = await _db.Chapters.AsNoTracking().Where(c => c.BookId == bookId).OrderBy(c => c.OrderIndex)
            .Select(c => new ChapterSummaryDto(c.Id, c.Title, c.Description, c.Slug, c.OrderIndex, c.SourcePageStart, c.SourcePageEnd, c.SectionId, c.TaleemUnits.Count))
            .ToListAsync(ct);

        var units = await _db.TaleemUnits.AsNoTracking()
            .Include(u => u.Chapter)
            .Include(u => u.ContentBlocks)
            .Include(u => u.Topics).ThenInclude(t => t.Topic)
            .Where(u => u.BookId == bookId)
            .OrderBy(u => u.Chapter.OrderIndex).ThenBy(u => u.OrderIndex)
            .ToListAsync(ct);

        var unitDtos = units.Select(u => new AdminUnitReviewDto(
            u.Id, u.ChapterId, u.Chapter.Title, u.Title, u.EstimatedReadingSeconds, u.SourcePageStart, u.SourcePageEnd,
            u.OrderIndex, u.ReviewStatus.ToString().ToUpperInvariant(),
            u.ContentBlocks.OrderBy(b => b.OrderIndex).Select(DtoMapper.ToBlock).ToList(),
            u.Topics.Select(t => DtoMapper.ToTopic(t.Topic)).ToList(),
            u.StructureConfidence)).ToList();

        var latestReport = jobs
            .Where(j => !string.IsNullOrWhiteSpace(j.ReportJson))
            .OrderByDescending(j => j.CompletedAt ?? j.StartedAt)
            .Select(j => TryParseReport(j.ReportJson!))
            .FirstOrDefault(r => r is not null);

        return new ReviewBookDto(book, sources.Select(ToSourceDto).ToList(), jobs, sections, chapters, unitDtos, latestReport);
    }

    private static IngestionReportDto? TryParseReport(string json)
    {
        try
        {
            var report = JsonSerializer.Deserialize<IngestionQualityReport>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (report is null) return null;
            return new IngestionReportDto(
                report.ProcessorVersion,
                report.PageCount,
                report.SectionCount,
                report.ChapterCount,
                report.UnitCount,
                report.ContentBlockCount,
                report.SearchChunkCount,
                report.TocEntryCount,
                report.ExcludedFrontMatterCount,
                report.HeadingCandidateCount,
                report.LowConfidenceItemCount,
                report.HeadingOnlyUnitCount,
                report.EmptyBodyUnitCount,
                report.UnitsPerPageAverage,
                report.HeadingOnlyUnitRatio,
                report.FrontMatterOriginRatio,
                report.HeadingConfidenceMin,
                report.HeadingConfidenceMax,
                report.HeadingConfidenceAverage,
                report.StructureConfidenceMin,
                report.StructureConfidenceMax,
                report.StructureConfidenceAverage,
                report.HasStructuralAnomalies,
                report.AnomalyFlags,
                report.DocumentZones.Select(z => new DocumentZoneDto(z.PageNumber, z.ZoneRole, z.Confidence, z.Evidence)).ToList(),
                report.TocEntries.Select(t => new TocEntryDto(t.Title, t.PageNumber, t.SourcePage, t.Confidence)).ToList(),
                report.ExcludedFrontMatter.Select(e => new ExcludedElementDto(e.PageNumber, e.Text, e.ElementType, e.Reason)).ToList(),
                report.SuspiciousUnitTitles,
                report.UnitsPerPageDistribution);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ExtractedPageDto>> GetExtractedPagesAsync(Guid sourceDocumentId, CancellationToken ct = default)
    {
        return await _db.ExtractedPages.AsNoTracking()
            .Where(p => p.SourceDocumentId == sourceDocumentId)
            .OrderBy(p => p.PageNumber)
            .Select(p => new ExtractedPageDto(p.Id, p.PageNumber, p.RawText, p.NormalizedText, p.ExtractionConfidence))
            .ToListAsync(ct);
    }

    public async Task<AdminUnitReviewDto?> GetUnitReviewAsync(Guid unitId, CancellationToken ct = default)
    {
        var u = await _db.TaleemUnits.AsNoTracking()
            .Include(x => x.Chapter)
            .Include(x => x.ContentBlocks)
            .Include(x => x.Topics).ThenInclude(t => t.Topic)
            .FirstOrDefaultAsync(x => x.Id == unitId, ct);
        if (u is null) return null;
        return new AdminUnitReviewDto(
            u.Id, u.ChapterId, u.Chapter.Title, u.Title, u.EstimatedReadingSeconds, u.SourcePageStart, u.SourcePageEnd,
            u.OrderIndex, u.ReviewStatus.ToString().ToUpperInvariant(),
            u.ContentBlocks.OrderBy(b => b.OrderIndex).Select(DtoMapper.ToBlock).ToList(),
            u.Topics.Select(t => DtoMapper.ToTopic(t.Topic)).ToList(),
            u.StructureConfidence);
    }

    public async Task<AdminUnitReviewDto?> UpdateUnitAsync(Guid unitId, UpdateUnitRequest request, CancellationToken ct = default)
    {
        var u = await _db.TaleemUnits.FirstOrDefaultAsync(x => x.Id == unitId, ct);
        if (u is null) return null;
        u.Title = request.Title.Trim();
        u.EstimatedReadingSeconds = Math.Max(30, request.EstimatedReadingSeconds);
        u.OrderIndex = request.OrderIndex;
        u.ReviewStatus = DtoMapper.ParseReviewStatus(request.ReviewStatus);
        await _db.SaveChangesAsync(ct);
        return await GetUnitReviewAsync(unitId, ct);
    }

    public async Task<ContentBlockDto?> UpdateBlockAsync(Guid blockId, UpdateBlockRequest request, CancellationToken ct = default)
    {
        var b = await _db.ContentBlocks.FirstOrDefaultAsync(x => x.Id == blockId, ct);
        if (b is null) return null;
        b.BlockType = DtoMapper.ParseBlockType(request.BlockType);
        b.Language = request.Language;
        b.Text = request.Text; // admin-edited source text; still traceable via SourcePage/Reference
        b.OrderIndex = request.OrderIndex;
        b.SourcePage = request.SourcePage;
        b.SourceReference = request.SourceReference;
        b.VerificationStatus = VerificationStatus.EditedByAdmin;
        await _db.SaveChangesAsync(ct);
        return DtoMapper.ToBlock(b);
    }

    public async Task SetUnitTopicsAsync(Guid unitId, SetUnitTopicsRequest request, CancellationToken ct = default)
    {
        var unit = await _db.TaleemUnits.FirstOrDefaultAsync(u => u.Id == unitId, ct)
                   ?? throw new InvalidOperationException("Unit not found.");

        var existing = _db.TaleemUnitTopics.Where(t => t.TaleemUnitId == unitId);
        _db.TaleemUnitTopics.RemoveRange(existing);
        await _db.SaveChangesAsync(ct);

        foreach (var name in request.TopicNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slug = SlugHelper.Slugify(name);
            var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Slug == slug, ct);
            if (topic is null)
            {
                topic = new Topic { Slug = slug, DisplayName = name.Trim() };
                _db.Topics.Add(topic);
                await _db.SaveChangesAsync(ct);
            }

            _db.TaleemUnitTopics.Add(new TaleemUnitTopic { TaleemUnitId = unit.Id, TopicId = topic.Id });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task MergeUnitsAsync(MergeUnitsRequest request, CancellationToken ct = default)
    {
        if (request.PrimaryUnitId == request.SecondaryUnitId)
            throw new InvalidOperationException("Cannot merge a unit with itself.");

        var primary = await _db.TaleemUnits.Include(u => u.ContentBlocks).FirstOrDefaultAsync(u => u.Id == request.PrimaryUnitId, ct)
                      ?? throw new InvalidOperationException("Primary unit not found.");
        var secondary = await _db.TaleemUnits.Include(u => u.ContentBlocks).FirstOrDefaultAsync(u => u.Id == request.SecondaryUnitId, ct)
                        ?? throw new InvalidOperationException("Secondary unit not found.");

        if (primary.BookId != secondary.BookId)
            throw new InvalidOperationException("Units must belong to the same book.");

        var startOrder = primary.ContentBlocks.Count == 0 ? 0 : primary.ContentBlocks.Max(b => b.OrderIndex) + 1;
        foreach (var block in secondary.ContentBlocks.OrderBy(b => b.OrderIndex))
        {
            block.TaleemUnitId = primary.Id;
            block.OrderIndex = startOrder++;
        }

        primary.SourcePageEnd = Math.Max(primary.SourcePageEnd ?? 0, secondary.SourcePageEnd ?? 0);
        primary.EstimatedReadingSeconds += secondary.EstimatedReadingSeconds;

        var secondaryTopics = await _db.TaleemUnitTopics.Where(t => t.TaleemUnitId == secondary.Id).ToListAsync(ct);
        foreach (var t in secondaryTopics)
        {
            var exists = await _db.TaleemUnitTopics.AnyAsync(x => x.TaleemUnitId == primary.Id && x.TopicId == t.TopicId, ct);
            if (!exists)
                _db.TaleemUnitTopics.Add(new TaleemUnitTopic { TaleemUnitId = primary.Id, TopicId = t.TopicId });
            _db.TaleemUnitTopics.Remove(t);
        }

        var chunks = _db.SearchChunks.Where(c => c.TaleemUnitId == secondary.Id);
        _db.SearchChunks.RemoveRange(chunks);
        _db.TaleemUnits.Remove(secondary);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AdminUnitReviewDto?> SplitUnitAsync(SplitUnitRequest request, CancellationToken ct = default)
    {
        var unit = await _db.TaleemUnits.Include(u => u.ContentBlocks).FirstOrDefaultAsync(u => u.Id == request.UnitId, ct)
                   ?? throw new InvalidOperationException("Unit not found.");

        var moving = unit.ContentBlocks.Where(b => b.OrderIndex >= request.SplitBeforeBlockOrderIndex).OrderBy(b => b.OrderIndex).ToList();
        if (moving.Count == 0)
            throw new InvalidOperationException("No blocks to split.");

        var newUnit = new TaleemUnit
        {
            BookId = unit.BookId,
            ChapterId = unit.ChapterId,
            Title = request.NewUnitTitle.Trim(),
            OrderIndex = unit.OrderIndex + 1,
            EstimatedReadingSeconds = ReadingTimeCalculator.EstimateSeconds(string.Join('\n', moving.Select(b => b.Text))),
            SourcePageStart = moving.Min(b => b.SourcePage),
            SourcePageEnd = moving.Max(b => b.SourcePage),
            ReviewStatus = ReviewStatus.NeedsEdits
        };
        _db.TaleemUnits.Add(newUnit);
        await _db.SaveChangesAsync(ct);

        var order = 0;
        foreach (var block in moving)
        {
            block.TaleemUnitId = newUnit.Id;
            block.OrderIndex = order++;
        }

        var remaining = unit.ContentBlocks.Where(b => b.OrderIndex < request.SplitBeforeBlockOrderIndex).ToList();
        unit.EstimatedReadingSeconds = ReadingTimeCalculator.EstimateSeconds(string.Join('\n', remaining.Select(b => b.Text)));
        unit.SourcePageEnd = remaining.Count == 0 ? unit.SourcePageStart : remaining.Max(b => b.SourcePage);

        await _db.SaveChangesAsync(ct);
        return await GetUnitReviewAsync(newUnit.Id, ct);
    }

    public async Task ApproveBookAsync(Guid bookId, CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct)
                   ?? throw new InvalidOperationException("Book not found.");
        if (book.Status is ContentLifecycleStatus.Failed or ContentLifecycleStatus.Processing or ContentLifecycleStatus.Uploaded)
            throw new InvalidOperationException("Book is not ready for approval.");

        book.Status = ContentLifecycleStatus.Approved;
        book.UpdatedAt = DateTimeOffset.UtcNow;
        var units = await _db.TaleemUnits.Where(u => u.BookId == bookId && u.ReviewStatus != ReviewStatus.Rejected).ToListAsync(ct);
        foreach (var u in units)
            u.ReviewStatus = ReviewStatus.Approved;
        await _db.SaveChangesAsync(ct);
    }

    public async Task PublishBookAsync(Guid bookId, CancellationToken ct = default)
        => await _publisher.PublishAsync(bookId, ct);

    public async Task DeleteBookAsync(Guid bookId, DeleteBookConfirmation confirmation, CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct)
                   ?? throw new InvalidOperationException("Book not found.");

        if (!string.Equals(confirmation.ConfirmationSlug, book.Slug, StringComparison.Ordinal))
            throw new InvalidOperationException("Confirmation slug does not match. Deletion aborted.");

        var sources = await _db.SourceDocuments.Where(s => s.BookId == bookId).ToListAsync(ct);
        foreach (var source in sources)
            await _storage.DeleteAsync(source.StorageKey, ct);

        // Cascade deletes structured content & chunks via EF relationships.
        // Shared Topics are Restrict — remove join rows first (cascade from units), keep Topic entities.
        _db.Books.Remove(book);
        await _db.SaveChangesAsync(ct);

        // Clean orphan topics not used by any remaining unit
        var orphanTopics = await _db.Topics.Where(t => !t.Units.Any()).ToListAsync(ct);
        if (orphanTopics.Count > 0)
        {
            _db.Topics.RemoveRange(orphanTopics);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Deleted book {BookId} slug {Slug}", bookId, book.Slug);
    }

    public async Task<IReadOnlyList<TopicDto>> ListTopicsAsync(CancellationToken ct = default)
    {
        return await _db.Topics.AsNoTracking().OrderBy(t => t.DisplayName)
            .Select(t => new TopicDto(t.Id, t.Slug, t.DisplayName))
            .ToListAsync(ct);
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        var slug = baseSlug;
        var i = 2;
        while (await _db.Books.AnyAsync(b => b.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{i++}";
        }
        return slug;
    }

    private async Task<AdminBookDto?> MapAdminBookAsync(Guid bookId, CancellationToken ct)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId, ct);
        if (book is null) return null;
        var sourceCount = await _db.SourceDocuments.CountAsync(s => s.BookId == bookId, ct);
        var chapterCount = await _db.Chapters.CountAsync(c => c.BookId == bookId, ct);
        var unitCount = await _db.TaleemUnits.CountAsync(u => u.BookId == bookId, ct);
        return new AdminBookDto(
            book.Id, book.Slug, book.Title, book.Subtitle, book.Author, book.Description, book.CoverUrl,
            book.DefaultLanguage, StatusNames.Content(book.Status), book.ContentVersion,
            book.CreatedAt, book.UpdatedAt, book.PublishedAt, sourceCount, chapterCount, unitCount);
    }

    private async Task<IngestionJobDto?> MapJobAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.IngestionJobs.AsNoTracking()
            .Include(j => j.SourceDocument)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null) return null;
        return new IngestionJobDto(
            job.Id, job.SourceDocumentId, job.SourceDocument.BookId, StatusNames.Job(job.Status),
            job.StartedAt, job.CompletedAt, job.Error, job.ProgressMessage, job.ProgressPercent,
            job.ProcessorVersion, job.ReportJson);
    }

    private static SourceDocumentDto ToSourceDto(SourceDocument s) => new(
        s.Id, s.BookId, s.OriginalFilename, s.SourceUrl, s.MimeType, s.FileHash, s.PageCount, s.FileSizeBytes, s.UploadedAt);
}
