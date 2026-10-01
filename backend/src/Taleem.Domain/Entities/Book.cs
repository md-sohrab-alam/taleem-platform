using Taleem.Domain.Enums;

namespace Taleem.Domain.Entities;

public class Book
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public string DefaultLanguage { get; set; } = "en";
    public ContentLifecycleStatus Status { get; set; } = ContentLifecycleStatus.Uploaded;
    public int ContentVersion { get; set; } = 1;
    public int SchemaVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }

    public ICollection<BookEdition> Editions { get; set; } = new List<BookEdition>();
    public ICollection<SourceDocument> SourceDocuments { get; set; } = new List<SourceDocument>();
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public ICollection<TaleemUnit> TaleemUnits { get; set; } = new List<TaleemUnit>();
    public ICollection<ContentBlock> ContentBlocks { get; set; } = new List<ContentBlock>();
    public ICollection<SearchChunk> SearchChunks { get; set; } = new List<SearchChunk>();
}
