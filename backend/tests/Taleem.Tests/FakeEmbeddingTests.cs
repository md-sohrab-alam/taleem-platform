using Microsoft.Extensions.Options;
using Taleem.Application.Options;
using Taleem.Infrastructure.Embeddings;

namespace Taleem.Tests;

public class FakeEmbeddingTests
{
    [Fact]
    public async Task Fake_Embeddings_Are_Deterministic_And_Normalized()
    {
        var svc = new FakeEmbeddingService(Options.Create(new EmbeddingOptions { Dimensions = 32 }));
        var a = await svc.EmbedAsync("salah");
        var b = await svc.EmbedAsync("salah");
        var c = await svc.EmbedAsync("wudhu");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        var norm = Math.Sqrt(a.Sum(v => v * v));
        Assert.InRange(norm, 0.99, 1.01);
    }
}
