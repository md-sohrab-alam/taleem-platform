using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Common;
using Taleem.Application.DTOs;
using Taleem.Application.Mapping;
using Taleem.Application.Services;
using Taleem.Domain.Entities;
using Taleem.Domain.Enums;
using Taleem.Infrastructure.Persistence;

namespace Taleem.Infrastructure.Services;

public sealed class PublicContentService : IPublicContentService
{
    private readonly TaleemDbContext _db;
    private readonly IEmbeddingService _embeddings;

    public PublicContentService(TaleemDbContext db, IEmbeddingService embeddings)
    {
        _db = db;
        _embeddings = embeddings;
    }

    public async Task<PagedResultDto<BookSummaryDto>> GetPublishedBooksAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.Books.AsNoTracking().Where(b => b.Status == ContentLifecycleStatus.Published);
        var total = await q.CountAsync(ct);
        var books = await q.OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        var items = books.Select(DtoMapper.ToSummary).ToList();
        return new PagedResultDto<BookSummaryDto>(items, page, pageSize, total);
    }

    public async Task<BookDetailDto?> GetBookAsync(string idOrSlug, CancellationToken ct = default)
    {
        var book = await FindPublishedBookAsync(idOrSlug, ct);
        if (book is null) return null;

        var editions = await _db.BookEditions.AsNoTracking()
            .Where(e => e.BookId == book.Id)
            .Select(e => new BookEditionDto(e.Id, e.Language, e.Translator, e.Publisher, e.Edition, e.PublicationYear, e.IsbnOrSourceIdentifier, e.RightsInformation))
            .ToListAsync(ct);

        return new BookDetailDto(
            book.Id, book.Slug, book.Title, book.Subtitle, book.Author, book.Description, book.CoverUrl,
            book.DefaultLanguage, StatusNames.Content(book.Status), book.ContentVersion, book.PublishedAt, editions);
    }

    public async Task<BookHomeDto?> GetBookHomeAsync(Guid bookId, string? userKey, CancellationToken ct = default)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId && b.Status == ContentLifecycleStatus.Published, ct);
        if (book is null) return null;

        ContinueReadingDto? cont = null;
        if (!string.IsNullOrWhiteSpace(userKey))
        {
            var progress = await _db.ReadingProgresses.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserKey == userKey && p.BookId == bookId, ct);
            if (progress?.TaleemUnitId is Guid uid)
            {
                var unit = await _db.TaleemUnits.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid, ct);
                cont = new ContinueReadingDto(uid, unit?.Title, progress.ChapterId);
            }
        }

        var chapters = await _db.Chapters.AsNoTracking()
            .Where(c => c.BookId == bookId)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new ChapterSummaryDto(
                c.Id, c.Title, c.Description, c.Slug, c.OrderIndex, c.SourcePageStart, c.SourcePageEnd, c.SectionId,
                c.TaleemUnits.Count))
            .ToListAsync(ct);

        var topics = await LoadPublicTopicsAsync(bookId, ct);

        var featuredUnits = await _db.TaleemUnits.AsNoTracking()
            .Where(u => u.BookId == bookId)
            .OrderBy(u => u.OrderIndex)
            .Take(5)
            .ToListAsync(ct);
        var featured = featuredUnits.Select(DtoMapper.ToUnitSummary).ToList();

        var suggested = new List<SuggestedTaleemDto>();
        foreach (var minutes in new[] { 5, 10, 15 })
        {
            var sel = await GetTaleemByDurationAsync(bookId, minutes, ct);
            if (sel is not null)
                suggested.Add(new SuggestedTaleemDto(minutes, sel.Units, sel.ApproximateSeconds));
        }

        var popularIds = await _db.AnalyticsEvents.AsNoTracking()
            .Where(e => e.BookId == bookId && e.EventType == AnalyticsEventType.UnitOpened && e.TaleemUnitId != null)
            .GroupBy(e => e.TaleemUnitId!.Value)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(5)
            .ToListAsync(ct);

        var popularUnits = await _db.TaleemUnits.AsNoTracking()
            .Where(u => popularIds.Contains(u.Id))
            .ToListAsync(ct);
        var popular = popularUnits.Select(DtoMapper.ToUnitSummary).ToList();

        var options = new List<ReadingOptionDto>
        {
            new("chapters", "Browse chapters", "chapters"),
            new("topics", "Browse topics", "topics"),
            new("taleem_5", "5-minute Ta'leem", "taleem?minutes=5"),
            new("taleem_10", "10-minute Ta'leem", "taleem?minutes=10"),
            new("taleem_15", "15-minute Ta'leem", "taleem?minutes=15"),
            new("search", "Search", "search"),
            new("semantic_search", "Semantic search", "semantic-search")
        };

        return new BookHomeDto(
            DtoMapper.ToSummary(book),
            cont,
            featured,
            topics,
            chapters,
            suggested,
            popular,
            options);
    }

    public async Task<IReadOnlyList<SectionDto>> GetSectionsAsync(Guid bookId, CancellationToken ct = default)
    {
        if (!await IsPublishedAsync(bookId, ct)) return [];
        return await _db.Sections.AsNoTracking()
            .Where(s => s.BookId == bookId)
            .OrderBy(s => s.OrderIndex)
            .Select(s => new SectionDto(s.Id, s.ParentSectionId, s.Title, s.OrderIndex))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ChapterSummaryDto>> GetChaptersAsync(Guid bookId, CancellationToken ct = default)
    {
        if (!await IsPublishedAsync(bookId, ct)) return [];
        return await _db.Chapters.AsNoTracking()
            .Where(c => c.BookId == bookId)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new ChapterSummaryDto(c.Id, c.Title, c.Description, c.Slug, c.OrderIndex, c.SourcePageStart, c.SourcePageEnd, c.SectionId, c.TaleemUnits.Count))
            .ToListAsync(ct);
    }

    public async Task<ChapterDetailDto?> GetChapterAsync(Guid chapterId, CancellationToken ct = default)
    {
        var chapter = await _db.Chapters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chapterId, ct);
        if (chapter is null || !await IsPublishedAsync(chapter.BookId, ct)) return null;

        var unitEntities = await _db.TaleemUnits.AsNoTracking()
            .Where(u => u.ChapterId == chapterId)
            .OrderBy(u => u.OrderIndex)
            .ToListAsync(ct);
        var units = unitEntities.Select(DtoMapper.ToUnitSummary).ToList();

        return new ChapterDetailDto(chapter.Id, chapter.BookId, chapter.Title, chapter.Description, chapter.Slug,
            chapter.OrderIndex, chapter.SourcePageStart, chapter.SourcePageEnd, chapter.SectionId, units);
    }

    public async Task<IReadOnlyList<TaleemUnitSummaryDto>> GetChapterUnitsAsync(Guid chapterId, CancellationToken ct = default)
    {
        var chapter = await _db.Chapters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chapterId, ct);
        if (chapter is null || !await IsPublishedAsync(chapter.BookId, ct)) return [];
        var unitEntities = await _db.TaleemUnits.AsNoTracking()
            .Where(u => u.ChapterId == chapterId)
            .OrderBy(u => u.OrderIndex)
            .ToListAsync(ct);
        return unitEntities.Select(DtoMapper.ToUnitSummary).ToList();
    }

    public async Task<TaleemUnitDetailDto?> GetUnitAsync(Guid unitId, CancellationToken ct = default)
    {
        var unit = await _db.TaleemUnits.AsNoTracking()
            .Include(u => u.Chapter)
            .Include(u => u.ContentBlocks)
            .Include(u => u.Topics).ThenInclude(t => t.Topic)
            .FirstOrDefaultAsync(u => u.Id == unitId, ct);
        if (unit is null || !await IsPublishedAsync(unit.BookId, ct)) return null;

        var topics = unit.Topics
            .Select(t => t.Topic)
            .Where(t => TopicQuality.IsPublicWorthy(t.DisplayName))
            .Select(t => new TopicDto(t.Id, t.Slug, TopicQuality.CleanDisplayName(t.DisplayName)))
            .OrderBy(t => t.DisplayName)
            .ToList();

        return new TaleemUnitDetailDto(
            unit.Id, unit.BookId, unit.ChapterId, unit.Title, unit.EstimatedReadingSeconds,
            unit.SourcePageStart, unit.SourcePageEnd, unit.OrderIndex,
            unit.ReviewStatus.ToString().ToUpperInvariant(),
            unit.ContentBlocks.OrderBy(b => b.OrderIndex).Select(DtoMapper.ToBlock).ToList(),
            topics,
            unit.Chapter.Title);
    }

    public async Task<IReadOnlyList<TopicDto>> GetTopicsAsync(Guid bookId, CancellationToken ct = default)
    {
        if (!await IsPublishedAsync(bookId, ct)) return [];
        return await LoadPublicTopicsAsync(bookId, ct);
    }

    public async Task<IReadOnlyList<TaleemUnitSummaryDto>> GetUnitsByTopicAsync(Guid bookId, Guid topicId, CancellationToken ct = default)
    {
        if (!await IsPublishedAsync(bookId, ct)) return [];
        var units = await _db.TaleemUnitTopics.AsNoTracking()
            .Where(t => t.TopicId == topicId && t.TaleemUnit.BookId == bookId)
            .Select(t => t.TaleemUnit)
            .OrderBy(u => u.OrderIndex)
            .ToListAsync(ct);
        return units.Select(DtoMapper.ToUnitSummary).ToList();
    }

    public async Task<TaleemSelectionDto?> GetTaleemByDurationAsync(Guid bookId, int minutes, CancellationToken ct = default)
    {
        if (!await IsPublishedAsync(bookId, ct)) return null;
        minutes = Math.Clamp(minutes, 1, 120);
        var budget = minutes * 60;

        var units = await _db.TaleemUnits.AsNoTracking()
            .Where(u => u.BookId == bookId)
            .OrderBy(u => u.Chapter.OrderIndex).ThenBy(u => u.OrderIndex)
            .ToListAsync(ct);

        var selected = new List<TaleemUnit>();
        var total = 0;
        foreach (var unit in units)
        {
            // Contiguous complete units only — never cut mid-unit, never skip holes.
            if (selected.Count == 0)
            {
                selected.Add(unit);
                total += unit.EstimatedReadingSeconds;
                if (total >= budget) break;
                continue;
            }

            if (total + unit.EstimatedReadingSeconds <= budget + 45)
            {
                selected.Add(unit);
                total += unit.EstimatedReadingSeconds;
                if (total >= budget) break;
            }
            else
            {
                break;
            }
        }

        if (selected.Count == 0 && units.Count > 0)
        {
            selected.Add(units[0]);
            total = units[0].EstimatedReadingSeconds;
        }

        return new TaleemSelectionDto(minutes, total, selected.Select(DtoMapper.ToUnitSummary).ToList());
    }

    public async Task<IReadOnlyList<KeywordSearchResultDto>> KeywordSearchAsync(Guid bookId, string query, int limit, CancellationToken ct = default)
    {
        if (!await IsPublishedAsync(bookId, ct)) return [];
        query = query.Trim();
        if (string.IsNullOrWhiteSpace(query)) return [];
        limit = Math.Clamp(limit, 1, 50);

        var pattern = $"%{query}%";
        var bookTitle = await _db.Books.Where(b => b.Id == bookId).Select(b => b.Title).FirstAsync(ct);

        var hits = await _db.SearchChunks.AsNoTracking()
            .Where(c => c.BookId == bookId &&
                        (EF.Functions.ILike(c.ChunkText, pattern) || EF.Functions.ILike(c.TaleemUnit.Title, pattern)))
            .Select(c => new
            {
                c.TaleemUnitId,
                c.ChunkText,
                c.ChunkIndex,
                UnitTitle = c.TaleemUnit.Title,
                ChapterId = c.TaleemUnit.ChapterId,
                ChapterTitle = c.TaleemUnit.Chapter.Title,
                Page = c.TaleemUnit.SourcePageStart
            })
            .ToListAsync(ct);

        // Prefer exact title, title prefix, title contains, then earlier chunks; one result per unit.
        return hits
            .OrderByDescending(h => string.Equals(h.UnitTitle, query, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(h => h.UnitTitle.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(h => h.UnitTitle.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(h => h.ChunkIndex)
            .GroupBy(h => h.TaleemUnitId)
            .Take(limit)
            .Select(g =>
            {
                var h = g.First();
                var excerpt = h.ChunkText.Length <= 240 ? h.ChunkText : h.ChunkText[..240] + "…";
                return new KeywordSearchResultDto(bookId, bookTitle, h.ChapterId, h.ChapterTitle, h.TaleemUnitId, h.UnitTitle, excerpt, h.Page, null);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SemanticSearchResultDto>> SemanticSearchAsync(SemanticSearchRequest request, CancellationToken ct = default)
    {
        var query = request.Query.Trim();
        if (string.IsNullOrWhiteSpace(query)) return [];
        var limit = Math.Clamp(request.Limit, 1, 50);

        var vector = await _embeddings.EmbedAsync(query, ct);

        // Load candidate chunks for published books (V1 in-process similarity; pgvector SQL when available).
        var chunkQuery = _db.SearchChunks.AsNoTracking()
            .Include(c => c.TaleemUnit).ThenInclude(u => u.Chapter)
            .Include(c => c.Book)
            .Where(c => c.Book.Status == ContentLifecycleStatus.Published && c.Embedding != null);

        if (request.BookId is Guid bid)
            chunkQuery = chunkQuery.Where(c => c.BookId == bid);

        var chunks = await chunkQuery.Take(2000).ToListAsync(ct);

        var ranked = chunks
            .Select(c => new { Chunk = c, Score = CosineSimilarity(vector, c.Embedding!) })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();

        var results = new List<SemanticSearchResultDto>();
        foreach (var item in ranked)
        {
            var unit = item.Chunk.TaleemUnit;
            var blocks = await _db.ContentBlocks.AsNoTracking()
                .Where(b => b.TaleemUnitId == unit.Id)
                .OrderBy(b => b.OrderIndex)
                .ToListAsync(ct);

            int? pageStart = unit.SourcePageStart;
            int? pageEnd = unit.SourcePageEnd;
            try
            {
                using var doc = JsonDocument.Parse(item.Chunk.MetadataJson);
                if (doc.RootElement.TryGetProperty("pageStart", out var ps)) pageStart = ps.GetInt32();
                if (doc.RootElement.TryGetProperty("pageEnd", out var pe)) pageEnd = pe.GetInt32();
            }
            catch { /* keep unit pages */ }

            results.Add(new SemanticSearchResultDto(
                item.Score,
                unit.BookId,
                item.Chunk.Book.Title,
                unit.ChapterId,
                unit.Chapter.Title,
                unit.Id,
                unit.Title,
                blocks.Select(DtoMapper.ToBlock).ToList(),
                item.Chunk.ChunkText.Length <= 280 ? item.Chunk.ChunkText : item.Chunk.ChunkText[..280] + "…",
                pageStart,
                pageEnd));
        }

        return results;
    }

    public async Task RecordEventAsync(AnalyticsEventRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AnalyticsEventType>(request.EventType.Replace("_", ""), true, out var type)
            && !Enum.TryParse(request.EventType, true, out type))
        {
            type = AnalyticsEventType.BookOpened;
            if (request.EventType.Equals("UnitOpened", StringComparison.OrdinalIgnoreCase)) type = AnalyticsEventType.UnitOpened;
            else if (request.EventType.Equals("UnitCompleted", StringComparison.OrdinalIgnoreCase)) type = AnalyticsEventType.UnitCompleted;
            else if (request.EventType.Equals("SearchPerformed", StringComparison.OrdinalIgnoreCase)) type = AnalyticsEventType.SearchPerformed;
            else if (request.EventType.Equals("SemanticSearchPerformed", StringComparison.OrdinalIgnoreCase)) type = AnalyticsEventType.SemanticSearchPerformed;
        }

        _db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            EventType = type,
            AnonymousUserKey = request.AnonymousUserKey,
            BookId = request.BookId,
            TaleemUnitId = request.TaleemUnitId,
            PayloadJson = request.PayloadJson
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<TopicDto>> LoadPublicTopicsAsync(Guid bookId, CancellationToken ct)
    {
        var raw = await _db.TaleemUnitTopics.AsNoTracking()
            .Where(t => t.TaleemUnit.BookId == bookId)
            .Select(t => t.Topic)
            .Distinct()
            .ToListAsync(ct);

        return raw
            .Where(t => TopicQuality.IsPublicWorthy(t.DisplayName))
            .Select(t => new TopicDto(t.Id, t.Slug, TopicQuality.CleanDisplayName(t.DisplayName)))
            .OrderBy(t => t.DisplayName)
            .ToList();
    }

    private async Task<bool> IsPublishedAsync(Guid bookId, CancellationToken ct)
        => await _db.Books.AnyAsync(b => b.Id == bookId && b.Status == ContentLifecycleStatus.Published, ct);

    private async Task<Book?> FindPublishedBookAsync(string idOrSlug, CancellationToken ct)
    {
        if (Guid.TryParse(idOrSlug, out var id))
            return await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && b.Status == ContentLifecycleStatus.Published, ct);
        return await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Slug == idOrSlug && b.Status == ContentLifecycleStatus.Published, ct);
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        var n = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < n; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom <= 0 ? 0 : dot / denom;
    }
}
