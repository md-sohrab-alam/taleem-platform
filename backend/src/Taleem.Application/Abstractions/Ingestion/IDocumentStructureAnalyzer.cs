using Taleem.Application.DocumentIr;
using Taleem.Domain.Enums;

namespace Taleem.Application.Abstractions.Ingestion;

public interface IDocumentStructureAnalyzer
{
    Task<AnalyzedBookStructure> AnalyzeAsync(
        string bookTitle,
        LayoutDocument layout,
        CancellationToken cancellationToken = default);
}

public sealed record AnalyzedBookStructure(
    IReadOnlyList<AnalyzedSection> Sections,
    IReadOnlyList<AnalyzedChapter> Chapters,
    IReadOnlyList<AnalyzedUnit> Units,
    IReadOnlyList<TocEntry> TocEntries,
    IReadOnlyList<ExcludedElementSummary> ExcludedFrontMatter,
    int HeadingCandidateCount,
    IngestionQualityReport? QualityReport);

public sealed record AnalyzedSection(string Title, int OrderIndex, int? ParentOrderIndex);

public sealed record AnalyzedChapter(
    string Title,
    string? Description,
    int OrderIndex,
    int? SectionOrderIndex,
    int SourcePageStart,
    int SourcePageEnd);

public sealed record AnalyzedUnit(
    string Title,
    int ChapterOrderIndex,
    int OrderIndex,
    int SourcePageStart,
    int SourcePageEnd,
    int EstimatedReadingSeconds,
    IReadOnlyList<AnalyzedBlock> Blocks,
    IReadOnlyList<string> TopicNames,
    double StructureConfidence = 1.0,
    bool NeedsReview = false);

public sealed record AnalyzedBlock(
    BlockType BlockType,
    string Language,
    string Text,
    int OrderIndex,
    int SourcePage,
    string? SourceReference,
    VerificationStatus VerificationStatus,
    Guid Id,
    int? SourcePageEnd = null);
