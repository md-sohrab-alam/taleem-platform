namespace Taleem.Domain.Entities;

public class ExtractedPage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceDocumentId { get; set; }
    public SourceDocument SourceDocument { get; set; } = null!;
    public int PageNumber { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public double ExtractionConfidence { get; set; } = 1.0;
}
