namespace Taleem.Domain.Entities;

public class Topic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<TaleemUnitTopic> Units { get; set; } = new List<TaleemUnitTopic>();
}
