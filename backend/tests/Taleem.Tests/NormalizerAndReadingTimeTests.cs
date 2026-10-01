using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Common;
using Taleem.Infrastructure.Ingestion;

namespace Taleem.Tests;

public class NormalizerAndReadingTimeTests
{
    [Fact]
    public void Normalizer_Collapses_Whitespace()
    {
        var n = new BookNormalizer();
        var result = n.Normalize(new ExtractedPageResult(
            1,
            "Hello   world\n\n\nNext",
            "Hello   world\n\n\nNext",
            0.9,
            false,
            ["en"]));
        Assert.Equal("Hello world\n\nNext", result.NormalizedText);
    }

    [Fact]
    public void ReadingTime_Is_Positive()
    {
        var seconds = ReadingTimeCalculator.EstimateSeconds(string.Join(' ', Enumerable.Repeat("word", 320)));
        Assert.InRange(seconds, 100, 140);
    }
}
