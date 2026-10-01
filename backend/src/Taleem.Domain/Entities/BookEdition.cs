namespace Taleem.Domain.Entities;

public class BookEdition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public string Language { get; set; } = "en";
    public string? Translator { get; set; }
    public string? Publisher { get; set; }
    public string? Edition { get; set; }
    public int? PublicationYear { get; set; }
    public string? IsbnOrSourceIdentifier { get; set; }
    public string? RightsInformation { get; set; }
}
