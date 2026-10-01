using Microsoft.Extensions.Options;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Options;
using Taleem.Domain.Enums;
using Taleem.Infrastructure.Ingestion;

namespace Taleem.Tests;

public class ChunkGeneratorTests
{
    [Fact]
    public void Keeps_Quran_With_Immediate_Translation()
    {
        var generator = new LogicalChunkGenerator(Options.Create(new ChunkingOptions
        {
            MaxChunkCharacters = 500,
            MinChunkCharacters = 50,
            OverlapCharacters = 0
        }));

        var unit = new AnalyzedUnit(
            "Unit",
            0,
            0,
            1,
            1,
            60,
            [
                new AnalyzedBlock(BlockType.QuranArabic, "ar", "Arabic ayah text", 0, 1, "p.1", VerificationStatus.Unverified, Guid.NewGuid()),
                new AnalyzedBlock(BlockType.QuranTranslation, "en", "Translation text", 1, 1, "p.1", VerificationStatus.Unverified, Guid.NewGuid()),
                new AnalyzedBlock(BlockType.Paragraph, "en", new string('x', 400), 2, 1, "p.1", VerificationStatus.Unverified, Guid.NewGuid())
            ],
            ["Prayer"]);

        var chunks = generator.Generate(unit, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2.2.0");
        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.ChunkText.Contains("Arabic ayah text") && c.ChunkText.Contains("Translation text"));
        Assert.All(chunks, c => Assert.NotEmpty(c.ContentBlockIds));
    }
}
