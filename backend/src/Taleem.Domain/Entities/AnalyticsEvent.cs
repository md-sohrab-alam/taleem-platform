using Taleem.Domain.Enums;

namespace Taleem.Domain.Entities;

public class AnalyticsEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AnalyticsEventType EventType { get; set; }
    public string? AnonymousUserKey { get; set; }
    public Guid? BookId { get; set; }
    public Guid? TaleemUnitId { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
