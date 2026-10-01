namespace Taleem.Domain.Entities;

public class ReadingHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserKey { get; set; } = string.Empty;
    public Guid BookId { get; set; }
    public Guid TaleemUnitId { get; set; }
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
