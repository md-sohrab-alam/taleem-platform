namespace Taleem.Domain.Entities;

public class SearchChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid TaleemUnitId { get; set; }
    public TaleemUnit TaleemUnit { get; set; } = null!;
    public string ChunkText { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public int ChunkIndex { get; set; }
    /// <summary>JSONB metadata: chapterId, pageStart, pageEnd, blockIds, etc.</summary>
    public string MetadataJson { get; set; } = "{}";
    /// <summary>Stored as pgvector; mapped in Infrastructure.</summary>
    public float[]? Embedding { get; set; }
    public string? SearchVector { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
