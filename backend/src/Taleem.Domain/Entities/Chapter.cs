namespace Taleem.Domain.Entities;

public class Chapter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public int OrderIndex { get; set; }
    public int? SourcePageStart { get; set; }
    public int? SourcePageEnd { get; set; }

    public ICollection<TaleemUnit> TaleemUnits { get; set; } = new List<TaleemUnit>();
}
