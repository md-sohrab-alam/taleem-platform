using System.Text.RegularExpressions;
using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Detects real CONTENTS pages and TOC entries as hierarchy evidence only.
/// Joins wrapped titles, validates page numbers, ignores catalog/imprint pages.
/// </summary>
public static partial class TocDetector
{
    public static IReadOnlyList<TocEntry> Detect(LayoutDocument document)
    {
        var pageCount = document.PageCount;
        var tocPageNumbers = IdentifyTocPages(document);
        var entries = new List<TocEntry>();

        foreach (var page in document.Pages.Where(p => tocPageNumbers.Contains(p.PageNumber)))
        {
            page.ZoneRole = DocumentZoneRole.TableOfContents;
            page.ZoneConfidence = Math.Max(page.ZoneConfidence, 0.9);
            if (!page.ZoneEvidence.Contains("toc-detector"))
                page.ZoneEvidence.Add("toc-detector");

            var lines = page.Elements
                .Where(e => e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem
                    or DocumentElementType.TocEntry)
                .OrderBy(e => e.ReadingOrder)
                .ToList();

            foreach (var el in lines)
            {
                el.ElementType = DocumentElementType.TocEntry;
                el.IncludeInReading = false;
                el.Evidence.Add("toc-page-content");
            }

            entries.AddRange(ParseEntries(lines, page.PageNumber, pageCount));
        }

        // Demote false TOC zones that were set earlier without becoming toc pages
        foreach (var page in document.Pages.Where(p =>
                     p.ZoneRole == DocumentZoneRole.TableOfContents
                     && !tocPageNumbers.Contains(p.PageNumber)))
        {
            if (CatalogCue().IsMatch(string.Join('\n', page.Elements.Select(e => e.NormalizedText))))
            {
                page.ZoneRole = DocumentZoneRole.Appendix;
                page.ZoneEvidence.Add("catalog-not-toc");
            }
            else if (PublisherCue().IsMatch(string.Join('\n', page.Elements.Select(e => e.NormalizedText))))
            {
                page.ZoneRole = DocumentZoneRole.PublisherInfo;
                page.ZoneEvidence.Add("publisher-not-toc");
            }
        }

        return Deduplicate(entries, pageCount);
    }

    private static HashSet<int> IdentifyTocPages(LayoutDocument document)
    {
        var pageCount = Math.Max(1, document.PageCount);
        var earlyLimit = Math.Max(3, (int)Math.Ceiling(pageCount * 0.35));
        var result = new HashSet<int>();

        foreach (var page in document.Pages)
        {
            var content = page.Elements
                .Where(e => e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem)
                .Select(e => e.NormalizedText)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
            var joined = string.Join('\n', content);

            if (CatalogCue().IsMatch(joined) && page.PageNumber > earlyLimit)
                continue;
            if (PublisherCue().IsMatch(joined) && !ContentsHeading().IsMatch(joined))
                continue;

            if (ContentsHeading().IsMatch(joined) && page.PageNumber <= earlyLimit)
            {
                result.Add(page.PageNumber);
                continue;
            }
        }

        // Adjacent early page with dense title+page patterns after a CONTENTS page
        if (result.Count > 0)
        {
            var seed = result.Min();
            foreach (var page in document.Pages.Where(p =>
                         p.PageNumber > seed
                         && p.PageNumber <= seed + 1
                         && p.PageNumber <= earlyLimit))
            {
                var content = page.Elements
                    .Where(e => e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem)
                    .Select(e => e.NormalizedText)
                    .ToList();
                var patterned = content.Count(t => TocLineWithLeaders().IsMatch(t) || TocLineLoose().IsMatch(t));
                if (patterned >= 3 && !CatalogCue().IsMatch(string.Join('\n', content)))
                    result.Add(page.PageNumber);
            }
        }

        return result;
    }

    private static List<TocEntry> ParseEntries(List<DocumentElement> lines, int sourcePage, int pageCount)
    {
        var entries = new List<TocEntry>();
        var pendingTitle = new List<string>();
        Guid? pendingId = null;

        void FlushPending()
        {
            pendingTitle.Clear();
            pendingId = null;
        }

        foreach (var el in lines)
        {
            var text = el.NormalizedText.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (ContentsHeading().IsMatch(text) && text.Length < 48)
            {
                FlushPending();
                continue;
            }

            if (ImprintOrContact().IsMatch(text))
            {
                FlushPending();
                continue;
            }

            var m = TocLineWithLeaders().Match(text);
            if (!m.Success) m = TocLineLoose().Match(text);

            if (m.Success)
            {
                var titlePart = CleanTocTitle(m.Groups["title"].Value);
                var pageNum = int.Parse(m.Groups["page"].Value);
                // OCR often splits multi-digit pages: "… Standing 1" + page "4" → 14
                (titlePart, pageNum) = RepairSplitPageNumber(titlePart, pageNum, pageCount);

                if (!IsValidPage(pageNum, pageCount))
                {
                    FlushPending();
                    continue;
                }

                var fullTitle = pendingTitle.Count == 0
                    ? titlePart
                    : CleanTocTitle(string.Join(' ', pendingTitle.Append(titlePart)));

                if (IsUsableTocTitle(fullTitle))
                {
                    entries.Add(new TocEntry
                    {
                        Title = fullTitle,
                        PageNumber = pageNum,
                        SourcePage = sourcePage,
                        Confidence = pendingTitle.Count > 0 ? 0.85 : (TocLineWithLeaders().IsMatch(text) ? 0.9 : 0.75),
                        SourceElementId = pendingId ?? el.Id
                    });
                }

                FlushPending();
                continue;
            }

            // Wrapped title fragment (no page number yet)
            if (text.Length is >= 2 and <= 90
                && !DigitsAtEnd().IsMatch(text)
                && !ImprintOrContact().IsMatch(text))
            {
                pendingTitle.Add(CleanTocTitle(text));
                pendingId ??= el.Id;
                continue;
            }

            FlushPending();
        }

        return entries;
    }

    private static IReadOnlyList<TocEntry> Deduplicate(List<TocEntry> entries, int pageCount)
    {
        var ordered = entries
            .Where(e => e.PageNumber is int p && IsValidPage(p, pageCount))
            .Where(e => IsUsableTocTitle(e.Title))
            .GroupBy(e => Normalize(e.Title))
            .Select(g => g.OrderByDescending(x => x.Confidence).First())
            .OrderBy(e => e.PageNumber ?? int.MaxValue)
            .ThenBy(e => e.Title)
            .ToList();

        return DropImplausiblePageJumps(ordered);
    }

    /// <summary>
    /// Drop TOC rows whose page number regresses sharply vs neighbors (OCR glued digits).
    /// Keeps leader-style high-confidence rows.
    /// </summary>
    private static List<TocEntry> DropImplausiblePageJumps(List<TocEntry> entries)
    {
        if (entries.Count < 3) return entries;
        var kept = new List<TocEntry>();
        for (var i = 0; i < entries.Count; i++)
        {
            var cur = entries[i];
            var curPage = cur.PageNumber ?? 0;
            var prevPage = kept.LastOrDefault()?.PageNumber;
            var nextPage = i + 1 < entries.Count ? entries[i + 1].PageNumber : null;

            var regresses = prevPage is int p && curPage + 2 < p;
            var nextContinuesPrior = prevPage is int pp && nextPage is int np && np >= pp - 1;
            if (regresses && nextContinuesPrior && cur.Confidence < 0.88)
                continue;

            kept.Add(cur);
        }

        return kept;
    }

    /// <summary>
    /// When OCR splits "14" into title-trailing "1" and page token "4", rejoin into page 14.
    /// </summary>
    private static (string Title, int Page) RepairSplitPageNumber(string title, int page, int pageCount)
    {
        var m = TrailingPageDigit().Match(title);
        if (!m.Success) return (title, page);
        if (page is < 0 or > 9) return (title, page);
        if (!int.TryParse(m.Groups["hi"].Value, out var hi) || hi is < 1 or > 99)
            return (title, page);

        var combined = hi * (int)Math.Pow(10, page.ToString().Length) + page;
        if (!IsValidPage(combined, pageCount)) return (title, page);

        var cleaned = CleanTocTitle(m.Groups["t"].Value);
        if (!IsUsableTocTitle(cleaned)) return (title, page);
        return (cleaned, combined);
    }

    private static bool IsValidPage(int page, int pageCount)
        => page >= 1 && page <= pageCount;

    private static bool IsUsableTocTitle(string title)
    {
        var t = title.Trim();
        if (t.Length is < 3 or > 120) return false;
        if (ImprintOrContact().IsMatch(t)) return false;
        // Reject bare wrap tails that never joined (generic function words)
        if (TitleFragmentOnly().IsMatch(t) && t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 5)
            return false;
        return true;
    }

    private static string CleanTocTitle(string title)
    {
        var t = title.Trim().TrimEnd('.', '·', '•', '…', ' ', '\t');
        t = Regex.Replace(t, @"[\.·•…]{2,}", " ").Trim();
        t = Regex.Replace(t, @"\s+", " ");
        return t;
    }

    private static string Normalize(string text)
        => string.Join(' ', text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex(@"\b(contents|table of contents|فہرست|الفهرس|فهرست)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ContentsHeading();

    [GeneratedRegex(@"\b(other free literature|other essential readings?|essential readings?|further readings?|free literature|publications?|catalogu?e)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CatalogCue();

    [GeneratedRegex(@"^(?<t>.+?)\s+(?<hi>\d{1,2})$")]
    private static partial Regex TrailingPageDigit();

    [GeneratedRegex(@"\b(published|distributed|publisher|isbn|issn|first print|edition|reprint|طباعت|ناشر)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PublisherCue();

    [GeneratedRegex(@"(@|www\.|https?://|all rights reserved|©)", RegexOptions.IgnoreCase)]
    private static partial Regex ImprintOrContact();

    /// <summary>Titles that look like unfinished wrap tails, not standalone TOC rows.</summary>
    [GeneratedRegex(@"^(and|or|of|the|part of|with|for|by|to|in|on|as)\b", RegexOptions.IgnoreCase)]
    private static partial Regex TitleFragmentOnly();

    [GeneratedRegex(@"^(?<title>.+?)[\s\.·•…]{2,}(?<page>\d{1,4})$")]
    private static partial Regex TocLineWithLeaders();

    [GeneratedRegex(@"^(?<title>.{2,80}?)\s+(?<page>\d{1,4})$")]
    private static partial Regex TocLineLoose();

    [GeneratedRegex(@"\d{1,4}$")]
    private static partial Regex DigitsAtEnd();
}
