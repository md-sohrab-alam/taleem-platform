namespace Taleem.Domain.Entities;

public class TaleemUnitTopic
{
    public Guid TaleemUnitId { get; set; }
    public TaleemUnit TaleemUnit { get; set; } = null!;
    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}
