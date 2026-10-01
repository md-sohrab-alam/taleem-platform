using System.Text.RegularExpressions;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Common;
using Taleem.Application.DocumentIr;
using Taleem.Domain.Enums;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Physical IR → zones → hierarchy → semantic reading units → content blocks.
/// HeadingCandidate ≠ TaleemUnit. TOC/front matter never become reading units.
/// </summary>
public sealed partial class LayoutAwareStructureAnalyzer : IDocumentStructureAnalyzer
{
    public Task<AnalyzedBookStructure> AnalyzeAsync(
        string bookTitle,
        LayoutDocument layout,
        CancellationToken cancellationToken = default)
    {
        DocumentZoneClassifier.Classify(layout);
        var toc = TocDetector.Detect(layout);
        var excluded = FrontMatterClassifier.Classify(layout).ToList();
        EnsureReadingContentAvailable(layout, excluded);

        // Logical paragraph/list/title reconstruction before heading/unit decisions
        LogicalTextReconstructor.Reconstruct(layout, toc);

        var headings = HeadingDetector.Detect(layout, toc);
        var readingEndPage = FindReadingEndPage(layout);

        var readingElements = layout.Pages
            .Where(p => p.PageNumber <= readingEndPage)
            .SelectMany(p => p.Elements)
            .Where(e => e.IncludeInReading
                        && e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem)
            .OrderBy(e => e.PageNumber)
            .ThenBy(e => e.ReadingOrder)
            .ToList();

        var chaptersResolved = ResolveChapters(bookTitle, layout, headings, toc, readingElements);
        var sections = BuildSections(headings);
        var chapters = new List<AnalyzedChapter>();
        var units = new List<AnalyzedUnit>();

        if (chaptersResolved.Count == 0)
        {
            chapters.Add(new AnalyzedChapter(
                bookTitle, null, 0, 0,
                readingElements.FirstOrDefault()?.PageNumber ?? 1,
                readingElements.LastOrDefault()?.PageNumber ?? readingEndPage));

            units.AddRange(BuildUnitsForElements(
                readingElements, headings, chapterOrder: 0, defaultTitle: bookTitle, headingConfidence: 0.5,
                ct: cancellationToken));
        }
        else
        {
            var firstHead = chaptersResolved[0];
            var preamble = readingElements.Where(e => IsBefore(e, firstHead.Element)).ToList();
            var chapterOffset = 0;
            if (preamble.Sum(e => e.NormalizedText.Length) >= 120)
            {
                var preambleTitle = InferPreambleTitle(preamble, bookTitle);
                chapters.Add(new AnalyzedChapter(
                    preambleTitle, null, 0, 0,
                    preamble.Min(e => e.PageNumber),
                    preamble.Max(e => e.PageNumber)));
                units.AddRange(BuildUnitsForElements(
                    preamble, headings, 0, preambleTitle, 0.55, cancellationToken));
                chapterOffset = 1;
            }

            for (var i = 0; i < chaptersResolved.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var head = chaptersResolved[i];
                var next = i + 1 < chaptersResolved.Count ? chaptersResolved[i + 1] : null;
                var startPage = head.Element.PageNumber;
                // Inclusive provenance span — not exclusive page ownership. Two chapters may
                // legitimately share a physical page when the next heading starts mid-page.
                var endPage = next?.Element.PageNumber ?? readingEndPage;
                if (endPage < startPage) endPage = startPage;
                if (endPage > readingEndPage) endPage = readingEndPage;

                var order = i + chapterOffset;
                chapters.Add(new AnalyzedChapter(
                    PreferBodyTitle(head),
                    null,
                    order,
                    sections.Count > 1 ? ResolveSectionIndex(headings, head, sections.Count) : 0,
                    startPage,
                    endPage));

                var slice = Slice(readingElements, head.Element, next?.Element)
                    .Where(e => e.PageNumber <= readingEndPage)
                    .ToList();
                units.AddRange(BuildUnitsForElements(
                    slice, headings, order, PreferBodyTitle(head), head.Confidence, cancellationToken));
            }
        }

        if (units.Count == 0 && readingElements.Count > 0)
        {
            var unit = BuildUnitFromElements(readingElements, 0, 0, bookTitle, 0.4);
            if (unit is not null) units.Add(unit);
            if (chapters.Count == 0)
            {
                chapters.Add(new AnalyzedChapter(
                    bookTitle, null, 0, 0,
                    readingElements.Min(e => e.PageNumber),
                    readingElements.Max(e => e.PageNumber)));
            }
        }

        var structure = new AnalyzedBookStructure(sections, chapters, units, toc, excluded, headings.Count, null);
        var report = IngestionQualityEvaluator.Build(layout, structure, excluded, headings.Count, 0, headings);
        structure = structure with { QualityReport = report };
        return Task.FromResult(structure);
    }

    private static int FindReadingEndPage(LayoutDocument layout)
    {
        // Only trim a trailing suffix of back-matter — do not cut mid-book false appendix zones.
        var pages = layout.Pages.OrderBy(p => p.PageNumber).ToList();
        if (pages.Count == 0) return 1;
        var last = pages.Count - 1;
        while (last >= 0 && (DocumentZoneClassifier.IsBackMatterZone(pages[last].ZoneRole)
                             || pages[last].ZoneRole is DocumentZoneRole.TableOfContents
                                 or DocumentZoneRole.PublisherInfo
                                 or DocumentZoneRole.Copyright))
        {
            // Keep trimming only while still in a trailing non-reading suffix
            last--;
        }

        // If we trimmed into front matter somehow, fall back to last main/preface page
        if (last < 0)
        {
            return pages.LastOrDefault(p => DocumentZoneClassifier.IsReadingZone(p.ZoneRole))?.PageNumber
                   ?? pages.Last().PageNumber;
        }

        return pages[last].PageNumber;
    }

    private static void EnsureReadingContentAvailable(LayoutDocument layout, List<ExcludedElementSummary> excluded)
    {
        var reading = layout.Pages.SelectMany(p => p.Elements).Count(e => e.IncludeInReading
            && e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem);
        if (reading > 0) return;

        foreach (var page in layout.Pages.OrderByDescending(p =>
                     p.Elements.Where(e => e.ElementType == DocumentElementType.Text)
                         .Sum(e => e.NormalizedText.Length)))
        {
            if (page.ZoneRole is DocumentZoneRole.TableOfContents or DocumentZoneRole.Copyright
                or DocumentZoneRole.Index or DocumentZoneRole.Appendix)
                continue;

            var restored = 0;
            foreach (var el in page.Elements.Where(e =>
                         e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem))
            {
                if (el.Evidence.Any(x => x.Contains("toc-", StringComparison.Ordinal)
                                         || x.StartsWith("exclude:contact", StringComparison.Ordinal)))
                    continue;
                if (el.NormalizedText.Length < 40) continue;
                el.IncludeInReading = true;
                el.Evidence.Add("restored-reading-content");
                restored++;
            }

            if (restored > 0)
            {
                page.ZoneRole = DocumentZoneRole.MainContent;
                page.ZoneEvidence.Add("restored-from-overclassified-front-matter");
                excluded.RemoveAll(e => e.PageNumber == page.PageNumber && e.Text.Length >= 40);
                break;
            }
        }
    }

    private static List<AnalyzedSection> BuildSections(IReadOnlyList<HeadingCandidate> headings)
    {
        var partHeads = headings.Where(h => h.LevelCandidate == HeadingLevelCandidate.Part).ToList();
        if (partHeads.Count == 0)
            return [new AnalyzedSection("Main Content", 0, null)];
        return partHeads.Select((p, i) => new AnalyzedSection(CleanTitle(p.Text), i, null)).ToList();
    }

    private static int ResolveSectionIndex(
        IReadOnlyList<HeadingCandidate> headings,
        HeadingCandidate chapterHead,
        int sectionCount)
    {
        var parts = headings.Where(h => h.LevelCandidate == HeadingLevelCandidate.Part).ToList();
        if (parts.Count == 0) return 0;
        return parts
            .Select((p, idx) => (p, idx))
            .Where(x => x.p.Element.PageNumber < chapterHead.Element.PageNumber
                        || (x.p.Element.PageNumber == chapterHead.Element.PageNumber
                            && x.p.Element.ReadingOrder <= chapterHead.Element.ReadingOrder))
            .Select(x => x.idx)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static List<HeadingCandidate> ResolveChapters(
        string bookTitle,
        LayoutDocument layout,
        IReadOnlyList<HeadingCandidate> headings,
        IReadOnlyList<TocEntry> toc,
        IReadOnlyList<DocumentElement> readingElements)
    {
        var tocEntries = toc.Where(t => !IsTitleFragment(t.Title)).ToList();

        // Prefer body headings that match TOC (fuzzy), using BODY text as title
        var fromToc = new List<HeadingCandidate>();
        foreach (var entry in tocEntries)
        {
            var match = headings
                .Select(h => (h, score: TitleMatchScore(h.Text, entry.Title)))
                .Where(x => x.score >= 0.55)
                .OrderByDescending(x => x.score)
                .ThenBy(x => Math.Abs((x.h.Element.PageNumber) - (entry.PageNumber ?? x.h.Element.PageNumber)))
                .FirstOrDefault();

            if (match.h is null) continue;
            // Keep body heading text; boost confidence when TOC agrees
            var enriched = new HeadingCandidate
            {
                Element = match.h.Element,
                Text = match.h.Text,
                LevelCandidate = HeadingLevelCandidate.Chapter,
                Confidence = Math.Clamp(match.h.Confidence + 0.15, 0, 1)
            };
            enriched.Evidence.Add("toc-body-match");
            foreach (var e in match.h.Evidence) enriched.Evidence.Add(e);
            fromToc.Add(enriched);
        }

        var strong = headings
            .Where(h => h.LevelCandidate is HeadingLevelCandidate.Part
                or HeadingLevelCandidate.Section
                or HeadingLevelCandidate.Chapter
                or HeadingLevelCandidate.Subchapter)
            .Where(h => h.Confidence >= 0.68 || h.Evidence.Contains("structural-keyword"))
            .Where(h => !IsTitleFragment(h.Text))
            .Where(h => HasBodyAfter(h, readingElements, headings))
            .ToList();

        // Additional body section heads: visual headings with body that split long spans
        var sectionLike = headings
            .Where(h => h.Confidence >= 0.7)
            .Where(h => !IsTitleFragment(h.Text))
            .Where(h => h.Text.Length is >= 3 and <= 80)
            .Where(h => HasBodyAfter(h, readingElements, headings))
            .ToList();

        var merged = fromToc
            .Concat(strong)
            .Concat(sectionLike)
            .GroupBy(h => NormalizeTitle(h.Text))
            .Select(g => g.OrderByDescending(x => x.Confidence).First())
            .OrderBy(h => h.Element.PageNumber)
            .ThenBy(h => h.Element.ReadingOrder)
            .ToList();

        if (merged.Count > 1 && TitlesLooselyEqual(merged[0].Text, bookTitle))
            merged.RemoveAt(0);

        // Drop fragment-like titles that somehow remained
        merged = merged.Where(h => !IsTitleFragment(h.Text)).ToList();

        // Require body between boundaries
        var withBody = new List<HeadingCandidate>();
        for (var i = 0; i < merged.Count; i++)
        {
            var next = i + 1 < merged.Count ? merged[i + 1] : null;
            var slice = Slice(readingElements, merged[i].Element, next?.Element);
            var bodyChars = slice.Where(e => e.Id != merged[i].Element.Id).Sum(e => e.NormalizedText.Length);
            if (bodyChars >= 40 || slice.Count >= 2)
                withBody.Add(merged[i]);
        }

        // Adaptive density: if too many chapters vs pages, keep TOC-backed + strongest
        var pageCount = Math.Max(1, layout.PageCount);
        if (withBody.Count > pageCount * 1.2)
        {
            withBody = withBody
                .Where(h => h.Evidence.Contains("toc-body-match")
                            || h.Evidence.Contains("toc-match")
                            || h.Confidence >= 0.78
                            || h.LevelCandidate is HeadingLevelCandidate.Chapter or HeadingLevelCandidate.Part)
                .ToList();
        }

        if (withBody.Count > 0) return withBody;

        return headings
            .Where(h => h.Confidence >= 0.72 && !IsTitleFragment(h.Text)
                        && HasBodyAfter(h, readingElements, headings))
            .GroupBy(h => NormalizeTitle(h.Text))
            .Select(g => g.OrderByDescending(x => x.Confidence).First())
            .OrderBy(h => h.Element.PageNumber)
            .ThenBy(h => h.Element.ReadingOrder)
            .Take(Math.Max(1, pageCount / 2))
            .ToList();
    }

    private static string PreferBodyTitle(HeadingCandidate head) => CleanTitle(head.Text);

    private static double TitleMatchScore(string bodyHeading, string tocTitle)
    {
        var a = NormalizeTitle(bodyHeading);
        var b = NormalizeTitle(tocTitle);
        if (a.Length < 2 || b.Length < 2) return 0;
        if (a == b) return 1;
        if (a.Contains(b) || b.Contains(a)) return 0.85;
        var ta = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length > 2).ToHashSet();
        var tb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length > 2).ToHashSet();
        if (ta.Count == 0 || tb.Count == 0) return 0;
        var inter = ta.Intersect(tb).Count();
        var union = ta.Union(tb).Count();
        return inter / (double)union;
    }

    private static bool IsTitleFragment(string title)
    {
        var t = title.Trim();
        if (t.Length < 3) return true;
        return TitleFragmentOnly().IsMatch(t)
               && t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 6;
    }

    private static List<AnalyzedUnit> BuildUnitsForElements(
        IReadOnlyList<DocumentElement> elements,
        IReadOnlyList<HeadingCandidate> allHeadings,
        int chapterOrder,
        string defaultTitle,
        double headingConfidence,
        CancellationToken ct)
    {
        if (elements.Count == 0) return [];

        var elementIds = elements.Select(e => e.Id).ToHashSet();
        var unitHeads = allHeadings
            .Where(h => elementIds.Contains(h.Element.Id))
            .Where(h => h.LevelCandidate is HeadingLevelCandidate.UnitHeading
                or HeadingLevelCandidate.Subchapter)
            .Where(h => h.Confidence >= 0.72)
            .Where(h => !IsTitleFragment(h.Text))
            .Where(h => HasBodyAfter(h, elements, allHeadings))
            .OrderBy(h => h.Element.PageNumber)
            .ThenBy(h => h.Element.ReadingOrder)
            .ToList();

        var firstHeading = allHeadings.FirstOrDefault(h => h.Element.Id == elements[0].Id);
        var boundaries = new List<(HeadingCandidate Head, double Conf)>();
        if (firstHeading is not null)
            boundaries.Add((firstHeading, Math.Max(headingConfidence, firstHeading.Confidence)));
        else
            boundaries.Add((new HeadingCandidate
            {
                Element = elements[0],
                Text = defaultTitle,
                LevelCandidate = HeadingLevelCandidate.UnitHeading,
                Confidence = headingConfidence
            }, headingConfidence));

        foreach (var h in unitHeads)
        {
            if (boundaries.Any(b => b.Head.Element.Id == h.Element.Id)) continue;
            boundaries.Add((h, h.Confidence));
        }

        boundaries = CollapseWeakBoundaries(boundaries, elements);

        var units = new List<AnalyzedUnit>();
        for (var i = 0; i < boundaries.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var start = boundaries[i];
            var end = i + 1 < boundaries.Count ? boundaries[i + 1].Head : null;
            var slice = end is null
                ? elements.Where(e => IsAfterOrAt(e, start.Head.Element)).ToList()
                : Slice(elements, start.Head.Element, end.Element);
            if (slice.Count == 0) continue;

            var title = CleanTitle(string.IsNullOrWhiteSpace(start.Head.Text) ? defaultTitle : start.Head.Text);
            var unit = BuildUnitFromElements(slice, chapterOrder, units.Count, title, start.Conf);
            if (unit is null) continue;

            var hasBody = unit.Blocks.Any(b => b.BlockType != BlockType.Heading && b.Text.Length >= 20);
            if (!hasBody && units.Count > 0) continue;
            if (!hasBody && i < boundaries.Count - 1) continue;
            units.Add(unit);
        }

        if (units.Count == 0)
        {
            var unit = BuildUnitFromElements(elements, chapterOrder, 0, defaultTitle, headingConfidence);
            if (unit is not null) units.Add(unit);
        }

        return units;
    }

    private static List<(HeadingCandidate Head, double Conf)> CollapseWeakBoundaries(
        List<(HeadingCandidate Head, double Conf)> boundaries,
        IReadOnlyList<DocumentElement> elements)
    {
        if (boundaries.Count <= 1) return boundaries;
        var kept = new List<(HeadingCandidate Head, double Conf)> { boundaries[0] };
        for (var i = 1; i < boundaries.Count; i++)
        {
            var cur = boundaries[i];
            var next = i + 1 < boundaries.Count ? boundaries[i + 1].Head : null;
            var slice = next is null
                ? elements.Where(e => IsAfterOrAt(e, cur.Head.Element)).ToList()
                : Slice(elements, cur.Head.Element, next.Element);
            var bodyChars = slice.Where(e => e.Id != cur.Head.Element.Id).Sum(e => e.NormalizedText.Length);
            if (bodyChars >= 80 || slice.Count(e => e.Id != cur.Head.Element.Id) >= 2
                || cur.Head.Evidence.Contains("toc-body-match")
                || cur.Conf >= 0.78)
                kept.Add(cur);
        }

        return kept;
    }

    private static bool HasBodyAfter(
        HeadingCandidate heading,
        IReadOnlyList<DocumentElement> elements,
        IReadOnlyList<HeadingCandidate> allHeadings)
    {
        var after = elements.Where(e => IsAfter(e, heading.Element)).Take(12).ToList();
        if (after.Count == 0) return false;
        var nextHead = allHeadings
            .Where(h => IsAfter(h.Element, heading.Element))
            .OrderBy(h => h.Element.PageNumber).ThenBy(h => h.Element.ReadingOrder)
            .FirstOrDefault();
        var body = nextHead is null ? after : after.Where(e => IsBefore(e, nextHead.Element)).ToList();
        var prose = body.Where(e => e.Id != heading.Element.Id).ToList();
        return prose.Sum(e => e.NormalizedText.Length) >= 40
               || prose.Any(e => e.NormalizedText.Length >= 40);
    }

    private static List<DocumentElement> Slice(
        IReadOnlyList<DocumentElement> elements,
        DocumentElement start,
        DocumentElement? end)
        => elements
            .Where(e => IsAfterOrAt(e, start))
            .Where(e => end is null || IsBefore(e, end))
            .ToList();

    private static AnalyzedUnit? BuildUnitFromElements(
        IReadOnlyList<DocumentElement> elements,
        int chapterOrder,
        int order,
        string title,
        double headingConfidence)
    {
        if (elements.Count == 0) return null;

        var blocks = new List<AnalyzedBlock>();
        var blockOrder = 0;
        for (var i = 0; i < elements.Count; i++)
        {
            var el = elements[i];
            if (string.IsNullOrWhiteSpace(el.NormalizedText)) continue;
            var prev = i > 0 ? elements[i - 1] : null;
            var next = i + 1 < elements.Count ? elements[i + 1] : null;
            var (type, lang, verification) = ContentBlockClassifier.Classify(el, prev, next, el.Confidence);

            if (blockOrder == 0 && TitlesLooselyEqual(el.NormalizedText, title))
                type = BlockType.Heading;

            if (type is BlockType.Paragraph or BlockType.List
                && LooksLikeSuspiciousFragment(el.NormalizedText)
                && el.Evidence.All(e => e is not ("line-wrap-merge" or "list-item-continue" or "paragraph-continue" or "title-line-merge")))
            {
                verification = VerificationStatus.FlaggedLowConfidence;
            }

            blocks.Add(new AnalyzedBlock(
                type,
                lang,
                el.NormalizedText,
                blockOrder++,
                el.PageNumber,
                $"p.{el.PageNumber}",
                verification,
                el.Id == Guid.Empty ? Guid.NewGuid() : el.Id));
        }

        if (blocks.Count == 0) return null;

        var text = string.Join('\n', blocks.Select(b => b.Text));
        var topics = InferGenericTopics(title, text);
        var bodyChars = blocks.Where(b => b.BlockType != BlockType.Heading).Sum(b => b.Text.Length);
        var flagged = blocks.Count(b => b.VerificationStatus == VerificationStatus.FlaggedLowConfidence);
        var structureConfidence = Math.Clamp(
            0.35
            + 0.35 * headingConfidence
            + (bodyChars >= 200 ? 0.2 : bodyChars >= 80 ? 0.1 : 0)
            + (IsTitleFragment(title) ? -0.35 : 0.1)
            - Math.Min(0.25, flagged * 0.03),
            0.05, 1.0);

        var needsReview = structureConfidence < 0.55 || IsTitleFragment(title) || flagged >= 3;

        return new AnalyzedUnit(
            CleanTitle(title),
            chapterOrder,
            order,
            elements.Min(e => e.PageNumber),
            elements.Max(e => e.PageNumber),
            ReadingTimeCalculator.EstimateSeconds(text),
            blocks,
            topics,
            structureConfidence,
            needsReview);
    }

    /// <summary>
    /// Suspicious unmerged wrap fragment — not a complete short sentence.
    /// Sentence-ending punctuation includes . ! ? and Arabic/Urdu marks.
    /// </summary>
    private static bool LooksLikeSuspiciousFragment(string text)
    {
        var t = text.Trim();
        if (t.Length is < 8 or > 60) return false;
        if (LogicalTextReconstructor.EndsWithSentencePunctuation(t)) return false;
        // Only lowercase-leading fragments are suspicious (Title Case sentences are fine)
        if (!char.IsLower(t[0])) return false;
        return true;
    }

    private static string InferPreambleTitle(IReadOnlyList<DocumentElement> preamble, string bookTitle)
    {
        var headingLike = preamble
            .Where(e => e.IsBold || e.FontSize is >= 14)
            .Where(e => e.NormalizedText.Length is >= 3 and <= 80)
            .OrderBy(e => e.PageNumber).ThenBy(e => e.ReadingOrder)
            .Select(e => CleanTitle(e.NormalizedText))
            .FirstOrDefault(t => !IsTitleFragment(t));
        return string.IsNullOrWhiteSpace(headingLike) ? bookTitle : headingLike;
    }

    private static IReadOnlyList<string> InferGenericTopics(string title, string text)
        => Taleem.Application.Common.TopicQuality.InferFromTitle(title);

    private static bool IsAfterOrAt(DocumentElement e, DocumentElement start)
        => e.PageNumber > start.PageNumber
           || (e.PageNumber == start.PageNumber && e.ReadingOrder >= start.ReadingOrder);

    private static bool IsAfter(DocumentElement e, DocumentElement start)
        => e.PageNumber > start.PageNumber
           || (e.PageNumber == start.PageNumber && e.ReadingOrder > start.ReadingOrder);

    private static bool IsBefore(DocumentElement e, DocumentElement end)
        => e.PageNumber < end.PageNumber
           || (e.PageNumber == end.PageNumber && e.ReadingOrder < end.ReadingOrder);

    private static string CleanTitle(string text)
    {
        var t = text.Trim();
        t = Regex.Replace(t, @"^\d{1,4}(?=[A-Zء-ي])", "");
        t = Regex.Replace(t, @"^\d{1,4}\s+", "");
        return t.Trim();
    }

    private static string NormalizeTitle(string text) => CleanTitle(text).ToLowerInvariant();

    private static bool TitlesLooselyEqual(string a, string b)
    {
        var na = NormalizeTitle(a);
        var nb = NormalizeTitle(b);
        if (na == nb) return true;
        if (na.Length >= 8 && nb.Contains(na)) return true;
        if (nb.Length >= 8 && na.Contains(nb)) return true;
        return false;
    }

    [GeneratedRegex(@"^(and|or|of|the|part of|with|for|by|to|in|on|as)\b", RegexOptions.IgnoreCase)]
    private static partial Regex TitleFragmentOnly();
}
