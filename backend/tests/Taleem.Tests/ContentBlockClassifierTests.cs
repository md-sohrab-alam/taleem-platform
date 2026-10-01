using Taleem.Application.DocumentIr;
using Taleem.Domain.Enums;
using Taleem.Infrastructure.Ingestion;

namespace Taleem.Tests;

public class ContentBlockClassifierTests
{
    [Fact]
    public void Ordinary_English_Prose_Is_Not_Transliteration()
    {
        var english = El("Each time we offer our prayer we should prepare carefully and with calmness.", ScriptHint.Latin, "en");
        var arabic = El("بسم الله", ScriptHint.Arabic, "ar");

        var (type, _, _) = ContentBlockClassifier.Classify(english, arabic, null, 0.9);
        Assert.Equal(BlockType.Paragraph, type);
    }

    [Fact]
    public void Phonetic_Neighbor_Of_Arabic_Can_Be_Transliteration()
    {
        var arabic = El("قل هو الله احد", ScriptHint.Arabic, "ar");
        var phonetic = El("Qul huwallahu ahad", ScriptHint.Latin, "en");
        // Strengthen phonetic markers
        phonetic = El("Qul huwa'llahu ahad aa ee", ScriptHint.Latin, "en");

        var (type, _, _) = ContentBlockClassifier.Classify(phonetic, arabic, null, 0.9);
        Assert.Equal(BlockType.Transliteration, type);
    }

    private static DocumentElement El(string text, ScriptHint script, string lang) => new()
    {
        PageNumber = 1,
        Text = text,
        NormalizedText = text,
        Script = script,
        LanguageHint = lang,
        TextDirection = ScriptLanguageDetector.DetectDirection(script),
        BoundingBox = new BoundingBox(10, 10, 200, 12),
        FontSize = 11,
        ReadingOrder = 0,
        Confidence = 0.9
    };
}
