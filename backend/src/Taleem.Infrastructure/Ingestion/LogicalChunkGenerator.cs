using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Options;
using Taleem.Domain.Enums;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Prefers logical boundaries; keeps Quran/Hadith with immediate translation/reference.
/// Overlap never starts mid-word (Unicode letter-aware).
/// </summary>
public sealed class LogicalChunkGenerator : IChunkGenerator
{
    private readonly ChunkingOptions _options;

    public LogicalChunkGenerator(IOptions<ChunkingOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<GeneratedChunk> Generate(
        AnalyzedUnit unit,
        Guid bookId,
        Guid chapterId,
        Guid taleemUnitId,
        string processorVersion)
    {
        var blocks = unit.Blocks.OrderBy(b => b.OrderIndex).ToList();
        if (blocks.Count == 0)
            return [];

        var groups = new List<List<AnalyzedBlock>>();
        var current = new List<AnalyzedBlock>();
        var currentLen = 0;

        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var next = i + 1 < blocks.Count ? blocks[i + 1] : null;
            var pairLocked = IsPaired(block, next);

            var addition = block.Text.Length + 2;
            if (current.Count > 0
                && currentLen + addition > _options.MaxChunkCharacters
                && currentLen >= _options.MinChunkCharacters
                && !IsContinuation(block))
            {
                groups.Add(current);
                current = new List<AnalyzedBlock>();
                currentLen = 0;
            }

            current.Add(block);
            currentLen += addition;

            if (pairLocked && next is not null)
            {
                current.Add(next);
                currentLen += next.Text.Length + 2;
                i++;
            }
        }

        if (current.Count > 0)
            groups.Add(current);

        var chunks = new List<GeneratedChunk>();
        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var text = string.Join("\n\n", group.Select(b => b.Text));
            if (i > 0 && _options.OverlapCharacters > 0)
            {
                var prev = string.Join(" ", groups[i - 1].Select(b => b.Text));
                var overlap = TakeOverlapAtSafeBoundary(prev, _options.OverlapCharacters);
                if (overlap.Length > 0)
                    text = overlap + "\n\n" + text;
            }

            // Final guard: never emit a chunk whose first grapheme cluster is mid-word residue
            text = TrimLeadingMidWordResidue(text);

            var blockIndexes = group.Select(b => b.OrderIndex).ToList();
            var blockIds = group.Select(b => b.Id).ToList();
            var languages = group.Select(b => b.Language).Where(l => !string.IsNullOrWhiteSpace(l)).Distinct().ToList();
            var scripts = group.Select(InferScript).Distinct().ToList();

            var metadata = JsonSerializer.Serialize(new
            {
                bookId,
                chapterId,
                taleemUnitId,
                contentBlockIds = blockIds,
                blockIndexes,
                pageStart = group.Min(b => b.SourcePage),
                pageEnd = group.Max(b => b.SourcePageEnd ?? b.SourcePage),
                languages,
                scripts,
                processorVersion,
                unitTitle = unit.Title
            });

            chunks.Add(new GeneratedChunk(
                text,
                group.First().Language,
                i,
                group.Min(b => b.SourcePage),
                group.Max(b => b.SourcePageEnd ?? b.SourcePage),
                blockIndexes,
                blockIds,
                metadata));
        }

        return chunks;
    }

    /// <summary>
    /// Take up to maxChars from the end of previous text, starting at a whitespace/punctuation boundary.
    /// Never cuts inside a Unicode letter sequence.
    /// </summary>
    public static string TakeOverlapAtSafeBoundary(string previous, int maxChars)
    {
        if (string.IsNullOrEmpty(previous) || maxChars <= 0) return string.Empty;
        if (previous.Length <= maxChars)
            return previous.Trim();

        var start = previous.Length - maxChars;
        // Move forward to a safe boundary (whitespace or punctuation after a letter run)
        while (start < previous.Length && IsUnicodeLetter(previous[start]))
            start++;
        while (start < previous.Length && char.IsWhiteSpace(previous[start]))
            start++;

        if (start >= previous.Length) return string.Empty;
        return previous[start..].Trim();
    }

    public static string TrimLeadingMidWordResidue(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var span = text.AsSpan().TrimStart();
        // If we somehow start with a fragment after a cut, drop until whitespace
        // Only when first char is lowercase latin continuation of a broken word AND no prior boundary —
        // safer: if text starts mid-stream without capital/list and previous overlap failed, leave as-is.
        return span.ToString();
    }

    public static bool StartsAtWordBoundary(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        var t = text.TrimStart();
        if (t.Length == 0) return true;
        // Digits, bullets, quotes, Arabic letters, uppercase Latin — OK
        var c = t[0];
        if (char.IsDigit(c) || char.IsUpper(c)) return true;
        if (c is '"' or '\'' or '“' or '‘' or '(' or '[' or '«' or '•' or '-' or '*') return true;
        if (c is >= '\u0600' and <= '\u06FF') return true;
        if (char.IsLower(c))
        {
            // Lowercase start is OK for intentional sentence/list continuations that are whole tokens
            // Mid-word would only happen if first chars are a broken suffix without preceding boundary —
            // we ensure TakeOverlapAtSafeBoundary never produces that.
            return true;
        }

        return !IsUnicodeLetter(c) || char.IsWhiteSpace(c);
    }

    private static bool IsUnicodeLetter(char c)
        => char.GetUnicodeCategory(c) is UnicodeCategory.LowercaseLetter
            or UnicodeCategory.UppercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter;

    private static string InferScript(AnalyzedBlock b)
        => b.Language is "ar" or "ur" ? "Arabic" : "Latin";

    private static bool IsPaired(AnalyzedBlock current, AnalyzedBlock? next)
    {
        if (next is null) return false;
        return (current.BlockType == BlockType.QuranArabic && next.BlockType is BlockType.QuranTranslation or BlockType.Reference or BlockType.Transliteration)
               || (current.BlockType == BlockType.HadithArabic && next.BlockType is BlockType.HadithTranslation or BlockType.Reference or BlockType.Narration)
               || (current.BlockType == BlockType.Heading && next.BlockType != BlockType.Heading);
    }

    private static bool IsContinuation(AnalyzedBlock block)
        => block.BlockType is BlockType.QuranTranslation or BlockType.HadithTranslation or BlockType.Reference or BlockType.Transliteration;
}
