using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Marks repeated header/footer/page-number furniture using cross-page position + text repetition.
/// </summary>
public static class PageFurnitureClassifier
{
    public static void Classify(LayoutDocument document)
    {
        if (document.Pages.Count == 0) return;

        var headerVotes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var footerVotes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in document.Pages)
        {
            var headerBand = page.Height * 0.12;
            var footerBand = page.Height * 0.12;

            foreach (var el in page.Elements.Where(e => e.ElementType == DocumentElementType.Text))
            {
                var key = NormalizeKey(el.NormalizedText);
                if (key.Length is < 2 or > 80) continue;

                if (el.BoundingBox.Top >= page.Height - headerBand)
                    headerVotes[key] = headerVotes.GetValueOrDefault(key) + 1;
                if (el.BoundingBox.Bottom <= footerBand)
                    footerVotes[key] = footerVotes.GetValueOrDefault(key) + 1;
            }
        }

        var threshold = Math.Max(2, (int)Math.Ceiling(document.Pages.Count * 0.35));
        var headerKeys = headerVotes.Where(kv => kv.Value >= threshold).Select(kv => kv.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var footerKeys = footerVotes.Where(kv => kv.Value >= threshold).Select(kv => kv.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var page in document.Pages)
        {
            var headerBand = page.Height * 0.12;
            var footerBand = page.Height * 0.12;

            foreach (var el in page.Elements)
            {
                var key = NormalizeKey(el.NormalizedText);

                if (el.ElementType == DocumentElementType.PageNumber)
                {
                    el.Evidence.Add("page-number");
                    continue;
                }

                if (el.BoundingBox.Top >= page.Height - headerBand && headerKeys.Contains(key))
                {
                    el.ElementType = DocumentElementType.Header;
                    el.Evidence.Add("repeated-header");
                }
                else if (el.BoundingBox.Bottom <= footerBand && footerKeys.Contains(key))
                {
                    el.ElementType = DocumentElementType.Footer;
                    el.Evidence.Add("repeated-footer");
                }
            }

            // Detach leading page-number tokens glued visually as own elements already;
            // also strip pure page numbers in body edge if not yet marked.
            foreach (var el in page.Elements.Where(e => e.ElementType == DocumentElementType.Text))
            {
                if (el.NormalizedText.Length <= 4 && el.NormalizedText.All(char.IsDigit)
                    && (el.BoundingBox.Bottom <= footerBand || el.BoundingBox.Top >= page.Height - headerBand))
                {
                    el.ElementType = DocumentElementType.PageNumber;
                    el.Evidence.Add("edge-digits");
                }
            }
        }
    }

    private static string NormalizeKey(string text)
        => string.Join(' ', text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
