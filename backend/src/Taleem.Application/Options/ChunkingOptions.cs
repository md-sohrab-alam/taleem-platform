namespace Taleem.Application.Options;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";
    public int MaxChunkCharacters { get; set; } = 1200;
    public int MinChunkCharacters { get; set; } = 200;
    public int OverlapCharacters { get; set; } = 120;
}
