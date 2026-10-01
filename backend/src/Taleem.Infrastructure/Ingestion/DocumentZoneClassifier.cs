using System.Text.RegularExpressions;
using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Classifies pages into document zones using layout + generic bibliographic patterns.
/// No book-specific titles/authors.
/// </summary>
public static partial class DocumentZoneClassifier
{
    public static void Classify(LayoutDocument document)
    {
        var pageCount = document.PageCount;
        if (pageCount == 0) return;
        var earlyLimit = Math.Max(3, (int)Math.Ceiling(pageCount * 0.35));
        var lateStart = Math.Max(2, (int)Math.Floor(pageCount * 0.75));

        foreach (var page in document.Pages)
        {
            var content = page.Elements
                .Where(e => e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem
                    or DocumentElementType.TocEntry)
                .Select(e => e.NormalizedText)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            var joined = string.Join('\n', content);
            var evidence = new List<string>();
            var role = DocumentZoneRole.Unknown;
            var confidence = 0.4;

            // Catalog / promo back-matter: require strong cues and little continuous prose
            if (page.PageNumber >= lateStart
                && LooksLikeCatalogPage(content, joined)
                && content.Count(t => t.Length > 120) <= 1)
            {
                role = DocumentZoneRole.Appendix;
                confidence = 0.86;
                evidence.Add("catalog-back-matter");
            }
            else if (ContentsHeading().IsMatch(joined) && page.PageNumber <= earlyLimit)
            {
                role = DocumentZoneRole.TableOfContents;
                confidence = 0.92;
                evidence.Add("contents-heading");
            }
            else if (CopyrightCue().IsMatch(joined))
            {
                role = DocumentZoneRole.Copyright;
                confidence = 0.88;
                evidence.Add("copyright-cue");
            }
            else if (PublisherCue().IsMatch(joined) && page.PageNumber <= Math.Max(4, pageCount / 5))
            {
                role = DocumentZoneRole.PublisherInfo;
                confidence = 0.82;
                evidence.Add("publisher-cue");
            }
            else if (IndexCue().IsMatch(joined) && page.PageNumber >= lateStart)
            {
                role = DocumentZoneRole.Index;
                confidence = 0.75;
                evidence.Add("index-cue");
            }
            else if (AppendixCue().IsMatch(joined) && page.PageNumber > pageCount / 2)
            {
                role = DocumentZoneRole.Appendix;
                confidence = 0.7;
                evidence.Add("appendix-cue");
            }
            else if (PrefaceCue().IsMatch(joined) && page.PageNumber <= Math.Max(5, pageCount / 4))
            {
                role = DocumentZoneRole.Preface;
                confidence = 0.72;
                evidence.Add("preface-cue");
            }
            else if (page.PageNumber == 1 && LooksLikeCover(content, page))
            {
                role = DocumentZoneRole.Cover;
                confidence = 0.78;
                evidence.Add("cover-layout");
            }
            else if (page.PageNumber <= 3 && LooksLikeTitlePage(content, page, joined))
            {
                role = DocumentZoneRole.TitlePage;
                confidence = 0.74;
                evidence.Add("title-page-layout");
            }
            else
            {
                // Dense title+page lines only count as TOC early and without catalog/publisher dominance
                var tocHits = content.Count(t => TocLineLike().IsMatch(t));
                if (tocHits >= 4 && page.PageNumber <= earlyLimit && !PublisherCue().IsMatch(joined)
                    && !CatalogCue().IsMatch(joined))
                {
                    role = DocumentZoneRole.TableOfContents;
                    confidence = 0.8;
                    evidence.Add("toc-line-density");
                }
                else
                {
                    role = DocumentZoneRole.MainContent;
                    confidence = 0.65;
                    evidence.Add("default-main");
                }
            }

            if (role is DocumentZoneRole.MainContent or DocumentZoneRole.Unknown
                && page.PageNumber <= 3
                && content.Count > 0
                && content.Count(t => t.Length > 120) == 0
                && content.Count(ContactOrMetaCue().IsMatch) >= 2)
            {
                role = DocumentZoneRole.PublisherInfo;
                confidence = 0.8;
                evidence.Add("early-meta-density");
            }

            // Promo / essential-readings strips on otherwise main pages near the end
            if (role == DocumentZoneRole.MainContent
                && page.PageNumber >= lateStart
                && LooksLikeCatalogPage(content, joined)
                && content.Count(t => t.Length > 140) == 0)
            {
                role = DocumentZoneRole.Appendix;
                confidence = 0.8;
                evidence.Add("late-promo-strip");
            }

            page.ZoneRole = role;
            page.ZoneConfidence = confidence;
            page.ZoneEvidence.Clear();
            foreach (var e in evidence)
                page.ZoneEvidence.Add(e);
        }

        RepairFalseAppendixIslands(document);
    }

    /// <summary>Zones that may contribute TaleemUnit reading content.</summary>
    public static bool IsReadingZone(DocumentZoneRole role)
        => role is DocumentZoneRole.MainContent or DocumentZoneRole.Preface;

    public static bool IsBackMatterZone(DocumentZoneRole role)
        => role is DocumentZoneRole.Appendix or DocumentZoneRole.Index;

    /// <summary>
    /// Catalog/promo pages: strong back-matter headings and/or numbered title lists,
    /// with little continuous prose. Mid-sentence words like "booklet" alone are not enough.
    /// </summary>
    private static bool LooksLikeCatalogPage(List<string> lines, string joined)
    {
        var longProse = lines.Count(t => t.Length > 120);
        var mediumProse = lines.Count(t => t.Length > 80);
        var numbered = lines.Count(t => NumberedCatalogLine().IsMatch(t));
        var hasHeadingCue = CatalogHeadingCue().IsMatch(joined)
                            || lines.Any(t => t.Length <= 48 && CatalogHeadingCue().IsMatch(t));

        if (!hasHeadingCue && numbered < 3)
            return false;

        // Dense numbered list (publisher catalog) even without a perfect heading match
        if (numbered >= 4 && longProse == 0 && mediumProse <= 1)
            return true;

        // Explicit catalog/promo heading + little continuous prose
        if (hasHeadingCue && longProse == 0 && mediumProse <= 1)
            return true;

        // Heading + several numbered titles, allow one medium line (subheads)
        if (hasHeadingCue && numbered >= 3 && longProse <= 1)
            return true;

        return false;
    }

    /// <summary>
    /// Isolated APPENDIX pages surrounded by reading zones with real prose are usually
    /// false positives (e.g. body text that mentioned "booklet" / "publication").
    /// Trailing back-matter runs are left intact.
    /// </summary>
    private static void RepairFalseAppendixIslands(LayoutDocument document)
    {
        var pages = document.Pages.OrderBy(p => p.PageNumber).ToList();
        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            if (page.ZoneRole != DocumentZoneRole.Appendix) continue;

            var prevReading = i > 0 && IsReadingZone(pages[i - 1].ZoneRole);
            var nextReading = i + 1 < pages.Count && IsReadingZone(pages[i + 1].ZoneRole);
            if (!prevReading || !nextReading) continue;

            var lines = page.Elements
                .Where(e => e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem)
                .Select(e => e.NormalizedText)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
            var longProse = lines.Count(t => t.Length > 100);
            var proseChars = lines.Where(t => t.Length > 60).Sum(t => t.Length);
            if (longProse >= 1 || proseChars >= 180)
            {
                page.ZoneRole = DocumentZoneRole.MainContent;
                page.ZoneConfidence = 0.7;
                page.ZoneEvidence.Add("demoted-false-appendix-island");
            }
        }
    }

    private static bool LooksLikeCover(List<string> lines, DocumentPage page)
    {
        if (lines.Count == 0 || lines.Count > 14) return false;
        if (lines.Any(t => t.Length > 48)) return false;
        var totalChars = lines.Sum(t => t.Length);
        if (totalChars > 160) return false;
        var large = page.Elements.Count(e => e.FontSize is >= 16);
        return large >= 1 && lines.Count <= 8;
    }

    private static bool LooksLikeTitlePage(List<string> lines, DocumentPage page, string joined)
    {
        if (lines.Count is 0 or > 22) return false;
        if (lines.Any(t => t.Length > 90)) return false;
        if (PublisherCue().IsMatch(joined) || CopyrightCue().IsMatch(joined)) return true;
        var centered = page.Elements.Count(e =>
            e.ElementType == DocumentElementType.Text
            && Math.Abs(e.BoundingBox.CenterX - page.Width / 2) < page.Width * 0.2);
        return centered >= 2;
    }

    [GeneratedRegex(@"\b(contents|table of contents|فہرست|الفهرس|فهرست)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ContentsHeading();

    [GeneratedRegex(@"^(?<title>.+?)[\s\.·•…]{2,}(?<page>\d{1,4})$|^(?<title>.+?)\s+(?<page>\d{1,4})$")]
    private static partial Regex TocLineLike();

    [GeneratedRegex(@"\b(©|copyright|all rights reserved|حقوق)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CopyrightCue();

    [GeneratedRegex(@"\b(published|distributed|publisher|isbn|issn|first print|edition|reprint|طباعت|ناشر)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PublisherCue();

    [GeneratedRegex(@"\b(preface|foreword|introduction|دیباچہ|مقدمہ)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PrefaceCue();

    [GeneratedRegex(@"\b(appendix|ملحق|ضمیمہ)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AppendixCue();

    [GeneratedRegex(@"\b(index|فهرست اعلام)\b", RegexOptions.IgnoreCase)]
    private static partial Regex IndexCue();

    /// <summary>Weak cue used only to avoid mistaking catalog pages for TOC.</summary>
    [GeneratedRegex(@"\b(other free literature|other essential readings?|essential readings?|further readings?|free literature|publications?|catalogu?e of)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CatalogCue();

    /// <summary>Heading-like catalog/promo phrases — not mid-sentence "this booklet…".</summary>
    [GeneratedRegex(@"\b(other free literature|other essential readings?|essential readings?|further readings?|free literature(?:\s*&\s*publications?)?|catalogu?e)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CatalogHeadingCue();

    [GeneratedRegex(@"^\d{1,2}[\.\)]\s+\S")]
    private static partial Regex NumberedCatalogLine();

    [GeneratedRegex(@"(@|www\.|https?://|\.org\b|\.com\b|isbn|©)", RegexOptions.IgnoreCase)]
    private static partial Regex ContactOrMetaCue();
}
