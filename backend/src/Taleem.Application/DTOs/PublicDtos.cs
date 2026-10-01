namespace Taleem.Application.DTOs;

public sealed record BookSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string? Subtitle,
    string? Author,
    string? Description,
    string? CoverUrl,
    string DefaultLanguage,
    string Status,
    DateTimeOffset? PublishedAt);

public sealed record BookDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string? Subtitle,
    string? Author,
    string? Description,
    string? CoverUrl,
    string DefaultLanguage,
    string Status,
    int ContentVersion,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<BookEditionDto> Editions);

public sealed record BookEditionDto(
    Guid Id,
    string Language,
    string? Translator,
    string? Publisher,
    string? Edition,
    int? PublicationYear,
    string? IsbnOrSourceIdentifier,
    string? RightsInformation);

public sealed record SectionDto(Guid Id, Guid? ParentSectionId, string Title, int OrderIndex);

public sealed record ChapterSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string? Slug,
    int OrderIndex,
    int? SourcePageStart,
    int? SourcePageEnd,
    Guid? SectionId,
    int UnitCount);

public sealed record ChapterDetailDto(
    Guid Id,
    Guid BookId,
    string Title,
    string? Description,
    string? Slug,
    int OrderIndex,
    int? SourcePageStart,
    int? SourcePageEnd,
    Guid? SectionId,
    IReadOnlyList<TaleemUnitSummaryDto> Units);

public sealed record TaleemUnitSummaryDto(
    Guid Id,
    Guid ChapterId,
    string Title,
    int EstimatedReadingSeconds,
    int? SourcePageStart,
    int? SourcePageEnd,
    int OrderIndex);

public sealed record ContentBlockDto(
    Guid Id,
    string BlockType,
    string Language,
    string Text,
    int OrderIndex,
    int? SourcePage,
    string? SourceReference,
    string VerificationStatus);

public sealed record TaleemUnitDetailDto(
    Guid Id,
    Guid BookId,
    Guid ChapterId,
    string Title,
    int EstimatedReadingSeconds,
    int? SourcePageStart,
    int? SourcePageEnd,
    int OrderIndex,
    string ReviewStatus,
    IReadOnlyList<ContentBlockDto> Blocks,
    IReadOnlyList<TopicDto> Topics,
    string? ChapterTitle);

public sealed record TopicDto(Guid Id, string Slug, string DisplayName);

public sealed record BookHomeDto(
    BookSummaryDto Book,
    ContinueReadingDto? ContinueReading,
    IReadOnlyList<TaleemUnitSummaryDto> FeaturedReadings,
    IReadOnlyList<TopicDto> Topics,
    IReadOnlyList<ChapterSummaryDto> Chapters,
    IReadOnlyList<SuggestedTaleemDto> SuggestedTaleem,
    IReadOnlyList<TaleemUnitSummaryDto> PopularPlaceholder,
    IReadOnlyList<ReadingOptionDto> ReadingOptions);

public sealed record ContinueReadingDto(Guid? UnitId, string? UnitTitle, Guid? ChapterId);

public sealed record SuggestedTaleemDto(int Minutes, IReadOnlyList<TaleemUnitSummaryDto> Units, int ApproximateSeconds);

public sealed record ReadingOptionDto(string Key, string Label, string? HrefHint);

public sealed record KeywordSearchResultDto(
    Guid BookId,
    string BookTitle,
    Guid ChapterId,
    string ChapterTitle,
    Guid UnitId,
    string UnitTitle,
    string Excerpt,
    int? SourcePage,
    double? Rank);

public sealed record SemanticSearchRequest(Guid? BookId, string Query, int Limit = 5);

public sealed record SemanticSearchResultDto(
    double Similarity,
    Guid BookId,
    string BookTitle,
    Guid ChapterId,
    string ChapterTitle,
    Guid UnitId,
    string UnitTitle,
    IReadOnlyList<ContentBlockDto> CanonicalBlocks,
    string ChunkExcerpt,
    int? SourcePageStart,
    int? SourcePageEnd);

public sealed record TaleemSelectionDto(
    int RequestedMinutes,
    int ApproximateSeconds,
    IReadOnlyList<TaleemUnitSummaryDto> Units);

public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record AnalyticsEventRequest(
    string EventType,
    string? AnonymousUserKey,
    Guid? BookId,
    Guid? TaleemUnitId,
    string? PayloadJson);
