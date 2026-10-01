namespace Taleem.Domain.Entities;

public class Section
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid? ParentSectionId { get; set; }
    public Section? ParentSection { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public ICollection<Section> Children { get; set; } = new List<Section>();
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
