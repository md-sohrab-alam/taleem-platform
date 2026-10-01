using Taleem.Application.DTOs;

namespace Taleem.Application.Services;

public interface IPublicContentService
{
    Task<PagedResultDto<BookSummaryDto>> GetPublishedBooksAsync(int page, int pageSize, CancellationToken ct = default);
    Task<BookDetailDto?> GetBookAsync(string idOrSlug, CancellationToken ct = default);
    Task<BookHomeDto?> GetBookHomeAsync(Guid bookId, string? userKey, CancellationToken ct = default);
    Task<IReadOnlyList<SectionDto>> GetSectionsAsync(Guid bookId, CancellationToken ct = default);
    Task<IReadOnlyList<ChapterSummaryDto>> GetChaptersAsync(Guid bookId, CancellationToken ct = default);
    Task<ChapterDetailDto?> GetChapterAsync(Guid chapterId, CancellationToken ct = default);
    Task<IReadOnlyList<TaleemUnitSummaryDto>> GetChapterUnitsAsync(Guid chapterId, CancellationToken ct = default);
    Task<TaleemUnitDetailDto?> GetUnitAsync(Guid unitId, CancellationToken ct = default);
    Task<IReadOnlyList<TopicDto>> GetTopicsAsync(Guid bookId, CancellationToken ct = default);
    Task<IReadOnlyList<TaleemUnitSummaryDto>> GetUnitsByTopicAsync(Guid bookId, Guid topicId, CancellationToken ct = default);
    Task<TaleemSelectionDto?> GetTaleemByDurationAsync(Guid bookId, int minutes, CancellationToken ct = default);
    Task<IReadOnlyList<KeywordSearchResultDto>> KeywordSearchAsync(Guid bookId, string query, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<SemanticSearchResultDto>> SemanticSearchAsync(SemanticSearchRequest request, CancellationToken ct = default);
    Task RecordEventAsync(AnalyticsEventRequest request, CancellationToken ct = default);
}
