using Taleem.Domain.Enums;

namespace Taleem.Application.Common;

public static class StatusNames
{
    public static string Content(ContentLifecycleStatus status) => status switch
    {
        ContentLifecycleStatus.Uploaded => "UPLOADED",
        ContentLifecycleStatus.Processing => "PROCESSING",
        ContentLifecycleStatus.Draft => "DRAFT",
        ContentLifecycleStatus.NeedsReview => "NEEDS_REVIEW",
        ContentLifecycleStatus.Approved => "APPROVED",
        ContentLifecycleStatus.Published => "PUBLISHED",
        ContentLifecycleStatus.Archived => "ARCHIVED",
        ContentLifecycleStatus.Failed => "FAILED",
        _ => status.ToString().ToUpperInvariant()
    };

    public static string Job(IngestionJobStatus status) => status switch
    {
        IngestionJobStatus.Queued => "QUEUED",
        IngestionJobStatus.Running => "RUNNING",
        IngestionJobStatus.Succeeded => "SUCCEEDED",
        IngestionJobStatus.Failed => "FAILED",
        IngestionJobStatus.Cancelled => "CANCELLED",
        _ => status.ToString().ToUpperInvariant()
    };
}
