using System.Text.RegularExpressions;
using Taleem.Application.DocumentIr;
using Taleem.Domain.Enums;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Conservative block typing from layout/script signals. Prefers PARAGRAPH over false religious/transliteration labels.
/// </summary>
public static partial class ContentBlockClassifier
{
    public static (BlockType Type, string Language, VerificationStatus Verification) Classify(
        DocumentElement element,
        DocumentElement? previous,
        DocumentElement? next,
        double pageConfidence)
    {
        var text = element.NormalizedText;
        var lang = element.LanguageHint ?? ScriptLanguageDetector.DetectLanguageHint(text, element.Script);
        var verification = pageConfidence < 0.6
            ? VerificationStatus.FlaggedLowConfidence
            : VerificationStatus.Unverified;

        if (element.ElementType == DocumentElementType.ListItem || LooksLikeListItem(text))
            return (BlockType.List, lang, verification);

        // Heading elements are labeled separately by structure builder
        if (element.Script == ScriptHint.Arabic)
        {
            // Do not aggressively tag Quran/Hadith without strong structural cues.
            if (text.Contains('﴿') || text.Contains('﴾')
                || (previous is not null && (previous.NormalizedText.Contains('﴿') || previous.NormalizedText.Contains('﴾'))))
                return (BlockType.QuranArabic, "ar", verification);

            if (HadithCue().IsMatch(text) && text.Length < 500)
                return (BlockType.HadithArabic, "ar", verification);

            return (text.Length < 180 ? BlockType.Dua : BlockType.Paragraph, lang is "ur" ? "ur" : "ar", verification);
        }

        if (LooksLikeTransliteration(text, previous, next))
            return (BlockType.Transliteration, "en", verification);

        if (ReferenceCue().IsMatch(text))
            return (BlockType.Reference, lang, verification);

        if (NoteCue().IsMatch(text))
            return (BlockType.Note, lang, verification);

        if (InstructionCue().IsMatch(text))
            return (BlockType.Instruction, lang, verification);

        if (previous is not null && previous.Script == ScriptHint.Arabic && element.Script == ScriptHint.Latin
            && text.Length < 400 && (TranslationCue().IsMatch(text) || previous.ElementType == DocumentElementType.Text))
        {
            // Adjacent Latin after Arabic may be translation — still keep as paragraph unless cue present
            if (TranslationCue().IsMatch(text))
                return (BlockType.QuranTranslation, lang, verification);
        }

        return (BlockType.Paragraph, lang, verification);
    }

    private static bool LooksLikeListItem(string text)
        => ListMarker().IsMatch(text);

    /// <summary>
    /// Transliteration is NOT ordinary English prose. Require phonetic markers + short length + Arabic neighbor.
    /// </summary>
    private static bool LooksLikeTransliteration(string text, DocumentElement? previous, DocumentElement? next)
    {
        if (text.Length is < 8 or > 160) return false;
        if (text.Count(char.IsWhiteSpace) > 18) return false; // long multi-word English prose
        if (text.Contains('.') && text.Split(' ').Length > 12) return false;

        var hasArabicNeighbor =
            previous?.Script == ScriptHint.Arabic || next?.Script == ScriptHint.Arabic;
        if (!hasArabicNeighbor) return false;

        var phoneticHits = 0;
        if (text.Contains('\'')) phoneticHits++;
        if (text.Contains('ʿ') || text.Contains('ʾ')) phoneticHits++;
        if (PhoneticCluster().IsMatch(text)) phoneticHits++;
        if (text.Any(c => c is >= '\u0100' and <= '\u024F')) phoneticHits++; // Latin extended often used in scholarly transliteration

        // Reject ordinary English sentence shape
        if (EnglishSentenceShape().IsMatch(text) && phoneticHits < 2)
            return false;

        return phoneticHits >= 2;
    }

    [GeneratedRegex(@"^(note:|n\.b\.|تنبیہ|نوٹ)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NoteCue();

    [GeneratedRegex(@"^(see |ref\.|reference:|حوالہ|رواه)", RegexOptions.IgnoreCase)]
    private static partial Regex ReferenceCue();

    [GeneratedRegex(@"^(step\s+\d+|how to\b|\d+[\.\)]\s)", RegexOptions.IgnoreCase)]
    private static partial Regex InstructionCue();

    [GeneratedRegex(@"^(translation:|meaning:|ترجمہ)", RegexOptions.IgnoreCase)]
    private static partial Regex TranslationCue();

    [GeneratedRegex(@"\b(قال رسول|حدثنا|عن .* قال)\b")]
    private static partial Regex HadithCue();

    [GeneratedRegex(@"^([\-\*\u2022\u25CF]|\d+[\.\)])\s+\S")]
    private static partial Regex ListMarker();

    [GeneratedRegex(@"\b(aa|ee|oo|kh|gh|dh|th|sh|ṣ|ṭ|ḥ|ẓ|ā|ī|ū)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PhoneticCluster();

    [GeneratedRegex(@"^(The|A|An|This|That|In|On|For|When|If|We|You|It)\b.*\b(the|and|of|to|is|are)\b", RegexOptions.IgnoreCase)]
    private static partial Regex EnglishSentenceShape();
}
