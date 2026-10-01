using Taleem.Domain.Enums;

namespace Taleem.Domain.Entities;

public class TaleemUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public int EstimatedReadingSeconds { get; set; }
    public int? SourcePageStart { get; set; }
    public int? SourcePageEnd { get; set; }
    public int OrderIndex { get; set; }
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Pending;
    /// <summary>0–1 structural confidence from ingestion (heading match, body size, title quality).</summary>
    public double StructureConfidence { get; set; } = 1.0;

    public ICollection<ContentBlock> ContentBlocks { get; set; } = new List<ContentBlock>();
    public ICollection<SearchChunk> SearchChunks { get; set; } = new List<SearchChunk>();
    public ICollection<TaleemUnitTopic> Topics { get; set; } = new List<TaleemUnitTopic>();
}
