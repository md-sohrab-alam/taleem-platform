namespace Taleem.Domain.Entities;

public class Bookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserKey { get; set; } = string.Empty;
    public Guid BookId { get; set; }
    public Guid TaleemUnitId { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
