using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Options;

namespace Taleem.Infrastructure.Embeddings;

/// <summary>
/// Deterministic local embeddings for development when no API key is configured.
/// </summary>
public sealed class FakeEmbeddingService : IEmbeddingService
{
    private readonly int _dimensions;

    public FakeEmbeddingService(IOptions<EmbeddingOptions> options)
    {
        _dimensions = options.Value.Dimensions;
    }

    public string ProviderName => "fake";
    public int Dimensions => _dimensions;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(Create(text));

    public Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<float[]>>(texts.Select(Create).ToList());

    private float[] Create(string text)
    {
        var vector = new float[_dimensions];
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        for (var i = 0; i < _dimensions; i++)
        {
            var b = bytes[i % bytes.Length];
            var b2 = bytes[(i * 3) % bytes.Length];
            vector[i] = ((b / 255f) * 2f - 1f) * 0.5f + ((b2 / 255f) * 0.1f);
        }

        // L2 normalize
        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm > 0)
        {
            for (var i = 0; i < vector.Length; i++)
                vector[i] /= norm;
        }

        return vector;
    }
}
