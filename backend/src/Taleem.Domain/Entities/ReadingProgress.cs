namespace Taleem.Domain.Entities;

public class ReadingProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserKey { get; set; } = string.Empty;
    public Guid BookId { get; set; }
    public Guid? ChapterId { get; set; }
    public Guid? TaleemUnitId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
