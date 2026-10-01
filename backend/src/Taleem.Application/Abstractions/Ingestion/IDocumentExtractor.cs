using Taleem.Application.DocumentIr;

namespace Taleem.Application.Abstractions.Ingestion;

public interface IDocumentExtractor
{
    Task<DocumentExtractionResult> ExtractAsync(Stream documentStream, string mimeType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Flat page text is derived FROM layout IR (never the primary representation).
/// </summary>
public sealed record ExtractedPageResult(
    int PageNumber,
    string RawText,
    string ContentText,
    double Confidence,
    bool IsScanned,
    IReadOnlyList<string> DetectedLanguages);

public sealed record DocumentExtractionResult(
    LayoutDocument Layout,
    IReadOnlyList<ExtractedPageResult> Pages,
    int PageCount);
