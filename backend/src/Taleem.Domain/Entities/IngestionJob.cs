using Taleem.Domain.Enums;

namespace Taleem.Domain.Entities;

public class IngestionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceDocumentId { get; set; }
    public SourceDocument SourceDocument { get; set; } = null!;
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Queued;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Error { get; set; }
    public string? ProgressMessage { get; set; }
    public int ProgressPercent { get; set; }
    public string ProcessorVersion { get; set; } = "1.0.0";
    /// <summary>Serialized ingestion quality report JSON.</summary>
    public string? ReportJson { get; set; }
}
