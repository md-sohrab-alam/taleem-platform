using Taleem.Domain.Enums;

namespace Taleem.Domain.Entities;

public class ContentBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid TaleemUnitId { get; set; }
    public TaleemUnit TaleemUnit { get; set; } = null!;
    public BlockType BlockType { get; set; } = BlockType.Paragraph;
    public string Language { get; set; } = "en";
    public string Text { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int? SourcePage { get; set; }
    public string? SourceReference { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
}
