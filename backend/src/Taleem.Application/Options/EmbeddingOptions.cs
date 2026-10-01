namespace Taleem.Application.Options;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    /// <summary>fake | openai</summary>
    public string Provider { get; set; } = "fake";
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string Model { get; set; } = "text-embedding-3-small";
    public int Dimensions { get; set; } = 1536;
}
