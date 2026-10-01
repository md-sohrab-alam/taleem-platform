namespace Taleem.Domain.Entities;

public class UserPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserKey { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
