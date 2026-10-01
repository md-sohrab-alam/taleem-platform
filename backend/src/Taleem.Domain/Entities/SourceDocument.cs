namespace Taleem.Domain.Entities;

public class SourceDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public string OriginalFilename { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    /// <summary>Internal storage key (generated ID), never expose raw filesystem paths publicly.</summary>
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/pdf";
    public string FileHash { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<IngestionJob> IngestionJobs { get; set; } = new List<IngestionJob>();
    public ICollection<ExtractedPage> ExtractedPages { get; set; } = new List<ExtractedPage>();
}
