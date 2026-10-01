using System.Text.RegularExpressions;
using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Multi-signal heading candidates. Does not create TaleemUnits.
/// Only considers IncludeInReading elements on reading zones.
/// </summary>
public static partial class HeadingDetector
{
    public static IReadOnlyList<HeadingCandidate> Detect(LayoutDocument document, IReadOnlyList<TocEntry> toc)
    {
        var tocTitles = toc
            .Select(t => Normalize(t.Title))
            .Where(t => t.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = new List<HeadingCandidate>();

        foreach (var page in document.Pages)
        {
            if (!DocumentZoneClassifier.IsReadingZone(page.ZoneRole))
                continue;

            var content = page.Elements
                .Where(IsReadableContentLine)
                .OrderBy(e => e.ReadingOrder)
                .ToList();
            if (content.Count == 0) continue;

            var sizes = content.Where(e => e.FontSize is > 0).Select(e => e.FontSize!.Value).ToList();
            var medianSize = sizes.Count == 0 ? 0 : Percentile(sizes, 0.5);
            var p75 = sizes.Count == 0 ? 0 : Percentile(sizes, 0.75);

            for (var i = 0; i < content.Count; i++)
            {
                var el = content[i];
                var text = el.NormalizedText;
                if (text.Length is < 2 or > 120) continue;

                // Numbered instructional steps are list content, not structural headings
                if (InstructionalNumbered().IsMatch(text) && text.Length > 48)
                    continue;

                var evidence = new List<string>();
                var score = 0.0;

                if (el.FontSize is double fs && medianSize > 0 && fs >= medianSize * 1.22)
                {
                    score += 0.3;
                    evidence.Add("font-size");
                }
                else if (el.FontSize is double fs2 && p75 > 0 && fs2 >= p75 * 1.05)
                {
                    score += 0.16;
                    evidence.Add("font-size-upper");
                }

                if (el.IsBold)
                {
                    score += 0.16;
                    evidence.Add("bold");
                }

                if (text.Length <= 70)
                {
                    score += 0.08;
                    evidence.Add("short-line");
                }

                if (!text.EndsWith('.') && !text.EndsWith('。') && !text.Contains(". "))
                {
                    score += 0.1;
                    evidence.Add("no-sentence-punct");
                }
                else if (text.EndsWith('.') && text.Split(' ').Length > 8)
                {
                    score -= 0.2;
                    evidence.Add("sentence-penalty");
                }

                if (i > 0)
                {
                    var prev = content[i - 1];
                    var verticalGap = prev.BoundingBox.CenterY - el.BoundingBox.CenterY
                                      - (prev.BoundingBox.Height + el.BoundingBox.Height) / 2;
                    if (verticalGap > Math.Max(4, (el.FontSize ?? 10) * 0.55))
                    {
                        score += 0.12;
                        evidence.Add("vertical-whitespace");
                    }
                }
                else
                {
                    score += 0.06;
                    evidence.Add("page-lead");
                }

                if (StructuralHeadingKeyword().IsMatch(text))
                {
                    score += 0.16;
                    evidence.Add("structural-keyword");
                }

                var centered = Math.Abs(el.BoundingBox.CenterX - page.Width / 2) < page.Width * 0.18
                               && el.BoundingBox.Width < page.Width * 0.75;
                if (centered)
                {
                    score += 0.08;
                    evidence.Add("centered");
                }

                if (tocTitles.Contains(Normalize(text))
                    || tocTitles.Any(t => TitleMatchLoose(t, Normalize(text))))
                {
                    score += 0.28;
                    evidence.Add("toc-match");
                }

                if (text.Length > 90 || text.Count(c => c == ',') > 2)
                {
                    score -= 0.28;
                    evidence.Add("prose-penalty");
                }

                // Soft numbering: alone is weak
                if (NumberedHeading().IsMatch(text))
                {
                    score += evidence.Contains("toc-match") || evidence.Contains("structural-keyword") ? 0.12 : 0.04;
                    evidence.Add("numbering-pattern");
                }

                score = Math.Clamp(score, 0, 1);
                if (score < 0.48) continue;

                var level = InferLevel(text, score, evidence);
                var candidate = new HeadingCandidate
                {
                    Element = el,
                    Text = text,
                    LevelCandidate = level,
                    Confidence = score
                };
                foreach (var e in evidence)
                    candidate.Evidence.Add(e);
                candidates.Add(candidate);
            }
        }

        return AdaptiveFilter(candidates, document.PageCount);
    }

    /// <summary>
    /// When heading density is anomalously high, keep only stronger structural signals.
    /// </summary>
    private static IReadOnlyList<HeadingCandidate> AdaptiveFilter(
        List<HeadingCandidate> candidates,
        int pageCount)
    {
        if (candidates.Count == 0 || pageCount <= 0) return candidates;

        var density = candidates.Count / (double)pageCount;
        if (density <= 2.2) return Order(candidates);

        return Order(candidates.Where(c =>
            c.Confidence >= 0.72
            || c.Evidence.Contains("toc-match")
            || c.Evidence.Contains("structural-keyword")
            || c.LevelCandidate is HeadingLevelCandidate.Part or HeadingLevelCandidate.Section
                or HeadingLevelCandidate.Chapter).ToList());
    }

    private static List<HeadingCandidate> Order(List<HeadingCandidate> list)
        => list.OrderBy(c => c.Element.PageNumber).ThenBy(c => c.Element.ReadingOrder).ToList();

    private static bool IsReadableContentLine(DocumentElement e)
        => e.IncludeInReading
           && e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem;

    private static HeadingLevelCandidate InferLevel(string text, double score, List<string> evidence)
    {
        if (StructuralPart().IsMatch(text)) return HeadingLevelCandidate.Part;
        if (StructuralSection().IsMatch(text)) return HeadingLevelCandidate.Section;
        if (StructuralChapter().IsMatch(text)) return HeadingLevelCandidate.Chapter;
        if (evidence.Contains("toc-match") && score >= 0.6) return HeadingLevelCandidate.Chapter;
        if (score >= 0.78 && evidence.Contains("font-size")) return HeadingLevelCandidate.Chapter;
        if (score >= 0.7) return HeadingLevelCandidate.Subchapter;
        return HeadingLevelCandidate.UnitHeading;
    }

    private static double Percentile(List<double> values, double p)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var idx = (int)Math.Clamp(Math.Round((sorted.Count - 1) * p), 0, sorted.Count - 1);
        return sorted[idx];
    }

    private static string Normalize(string text)
        => string.Join(' ', text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static bool TitleMatchLoose(string tocNorm, string bodyNorm)
    {
        if (tocNorm.Length < 8 || bodyNorm.Length < 8) return false;
        var a = tocNorm.Replace(",", " ");
        var b = bodyNorm.Replace(",", " ");
        a = string.Join(' ', a.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        b = string.Join(' ', b.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (a == b) return true;
        if (a.Contains(b) || b.Contains(a)) return true;
        var ta = a.Split(' ').Where(t => t.Length > 2).ToHashSet();
        var tb = b.Split(' ').Where(t => t.Length > 2).ToHashSet();
        if (ta.Count == 0 || tb.Count == 0) return false;
        return ta.Intersect(tb).Count() / (double)ta.Union(tb).Count() >= 0.75;
    }

    [GeneratedRegex(@"^(chapter|part|section|باب|فصل|حصہ|جزء)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StructuralHeadingKeyword();

    [GeneratedRegex(@"^(part|جزء|حصہ)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StructuralPart();

    [GeneratedRegex(@"^(section|فصل)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StructuralSection();

    [GeneratedRegex(@"^(chapter|باب)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StructuralChapter();

    [GeneratedRegex(@"^(\d{1,2}|[IVXLC]{1,6}|[A-Z])([\.\):-]| )")]
    private static partial Regex NumberedHeading();

    [GeneratedRegex(@"^\d{1,2}[\.\)]\s+\S")]
    private static partial Regex InstructionalNumbered();
}
