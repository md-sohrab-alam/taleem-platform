namespace Taleem.Application.DTOs;

public sealed record CreateBookRequest(
    string Title,
    string? Subtitle,
    string? Author,
    string? Description,
    string? Slug,
    string DefaultLanguage = "en");

public sealed record UpdateBookRequest(
    string Title,
    string? Subtitle,
    string? Author,
    string? Description,
    string? CoverUrl,
    string DefaultLanguage);

public sealed record AdminBookDto(
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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    int SourceDocumentCount,
    int ChapterCount,
    int UnitCount);

public sealed record AdminBookListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string Status,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

public sealed record SourceDocumentDto(
    Guid Id,
    Guid BookId,
    string OriginalFilename,
    string? SourceUrl,
    string MimeType,
    string FileHash,
    int PageCount,
    long FileSizeBytes,
    DateTimeOffset UploadedAt);

public sealed record IngestionJobDto(
    Guid Id,
    Guid SourceDocumentId,
    Guid BookId,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? Error,
    string? ProgressMessage,
    int ProgressPercent,
    string ProcessorVersion,
    string? ReportJson = null);

public sealed record ExtractedPageDto(
    Guid Id,
    int PageNumber,
    string RawText,
    string NormalizedText,
    double ExtractionConfidence);

public sealed record ReviewBookDto(
    AdminBookDto Book,
    IReadOnlyList<SourceDocumentDto> Sources,
    IReadOnlyList<IngestionJobDto> Jobs,
    IReadOnlyList<SectionDto> Sections,
    IReadOnlyList<ChapterSummaryDto> Chapters,
    IReadOnlyList<AdminUnitReviewDto> Units,
    IngestionReportDto? IngestionReport = null);

public sealed record IngestionReportDto(
    string ProcessorVersion,
    int PageCount,
    int SectionCount,
    int ChapterCount,
    int UnitCount,
    int ContentBlockCount,
    int SearchChunkCount,
    int TocEntryCount,
    int ExcludedFrontMatterCount,
    int HeadingCandidateCount,
    int LowConfidenceItemCount,
    int HeadingOnlyUnitCount,
    int EmptyBodyUnitCount,
    double UnitsPerPageAverage,
    double HeadingOnlyUnitRatio,
    double FrontMatterOriginRatio,
    double HeadingConfidenceMin,
    double HeadingConfidenceMax,
    double HeadingConfidenceAverage,
    double StructureConfidenceMin,
    double StructureConfidenceMax,
    double StructureConfidenceAverage,
    bool HasStructuralAnomalies,
    IReadOnlyList<string> AnomalyFlags,
    IReadOnlyList<DocumentZoneDto> DocumentZones,
    IReadOnlyList<TocEntryDto> TocEntries,
    IReadOnlyList<ExcludedElementDto> ExcludedFrontMatter,
    IReadOnlyList<string> SuspiciousUnitTitles,
    IReadOnlyDictionary<int, int> UnitsPerPageDistribution);

public sealed record DocumentZoneDto(int PageNumber, string ZoneRole, double Confidence, IReadOnlyList<string> Evidence);
public sealed record TocEntryDto(string Title, int? PageNumber, int SourcePage, double Confidence);
public sealed record ExcludedElementDto(int PageNumber, string Text, string ElementType, string Reason);

public sealed record AdminUnitReviewDto(
    Guid Id,
    Guid ChapterId,
    string ChapterTitle,
    string Title,
    int EstimatedReadingSeconds,
    int? SourcePageStart,
    int? SourcePageEnd,
    int OrderIndex,
    string ReviewStatus,
    IReadOnlyList<ContentBlockDto> Blocks,
    IReadOnlyList<TopicDto> Topics,
    double StructureConfidence = 1.0);

public sealed record UpdateUnitRequest(
    string Title,
    int EstimatedReadingSeconds,
    int OrderIndex,
    string ReviewStatus);

public sealed record UpdateBlockRequest(
    string BlockType,
    string Language,
    string Text,
    int OrderIndex,
    int? SourcePage,
    string? SourceReference);

public sealed record MergeUnitsRequest(Guid PrimaryUnitId, Guid SecondaryUnitId);

public sealed record SplitUnitRequest(Guid UnitId, int SplitBeforeBlockOrderIndex, string NewUnitTitle);

public sealed record SetUnitTopicsRequest(IReadOnlyList<string> TopicNames);

public sealed record DeleteBookConfirmation(string ConfirmationSlug);
