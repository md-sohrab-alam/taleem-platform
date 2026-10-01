using System.Text.RegularExpressions;
using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Marks non-reading elements (front matter, contact, TOC, furniture) with IncludeInReading=false.
/// Preserves text for provenance. No book-specific author/title hardcoding.
/// </summary>
public static partial class FrontMatterClassifier
{
    public static IReadOnlyList<ExcludedElementSummary> Classify(LayoutDocument document)
    {
        var excluded = new List<ExcludedElementSummary>();

        foreach (var page in document.Pages)
        {
            var nonReadingZone = !DocumentZoneClassifier.IsReadingZone(page.ZoneRole)
                                 || DocumentZoneClassifier.IsBackMatterZone(page.ZoneRole);

            foreach (var el in page.Elements)
            {
                var reasons = new List<string>();
                var text = el.NormalizedText ?? string.Empty;

                if (el.ElementType is DocumentElementType.Header or DocumentElementType.Footer
                    or DocumentElementType.PageNumber or DocumentElementType.TocEntry)
                {
                    reasons.Add($"element-type:{el.ElementType}");
                }

                if (nonReadingZone
                    && el.ElementType is DocumentElementType.Text or DocumentElementType.ListItem
                    && page.ZoneRole != DocumentZoneRole.Preface)
                {
                    // Keep substantial prose even on mis-zoned pages; exclude short meta/title lines.
                    if (page.ZoneRole == DocumentZoneRole.TableOfContents
                        || text.Length < 80
                        || ContactCue().IsMatch(text)
                        || CopyrightCue().IsMatch(text)
                        || PublisherMetaCue().IsMatch(text))
                    {
                        reasons.Add($"zone:{page.ZoneRole}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (ContactCue().IsMatch(text))
                        reasons.Add("contact-pattern");
                    if (CopyrightCue().IsMatch(text))
                        reasons.Add("copyright-pattern");
                    if (PublisherMetaCue().IsMatch(text))
                        reasons.Add("publisher-meta-pattern");
                    if (IsbnCue().IsMatch(text))
                        reasons.Add("isbn-pattern");
                }

                if (reasons.Count == 0)
                {
                    el.IncludeInReading = el.ElementType is DocumentElementType.Text or DocumentElementType.ListItem;
                    continue;
                }

                el.IncludeInReading = false;
                foreach (var r in reasons)
                    el.Evidence.Add("exclude:" + r);

                var snippet = text.Length <= 120 ? text : text[..120];
                excluded.Add(new ExcludedElementSummary
                {
                    PageNumber = el.PageNumber,
                    Text = snippet,
                    ElementType = el.ElementType.ToString().ToUpperInvariant(),
                    Reason = string.Join(",", reasons)
                });
            }
        }

        return excluded;
    }

    [GeneratedRegex(@"(@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}|www\.|https?://|\.org\b|\.com\b|\.net\b)", RegexOptions.IgnoreCase)]
    private static partial Regex ContactCue();

    [GeneratedRegex(@"\b(©|copyright|all rights reserved|حقوق محفوظ)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CopyrightCue();

    [GeneratedRegex(@"\b(published\s*(&|and)?\s*distributed|distributed by|publisher|first print|reprint|edition\b|طباعت|ناشر)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PublisherMetaCue();

    [GeneratedRegex(@"\b(isbn|issn)\b[:\s]*[0-9X\-]+", RegexOptions.IgnoreCase)]
    private static partial Regex IsbnCue();
}
