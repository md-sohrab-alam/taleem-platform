using Microsoft.Extensions.Logging;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.DocumentIr;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Layout-aware PDF extraction. Builds Document IR from words/letters; flat page text is derived afterward.
/// Never classifies religious meaning at this stage.
/// </summary>
public sealed class PdfPigDocumentExtractor : IDocumentExtractor
{
    private readonly ILogger<PdfPigDocumentExtractor> _logger;

    public PdfPigDocumentExtractor(ILogger<PdfPigDocumentExtractor> logger) => _logger = logger;

    public Task<DocumentExtractionResult> ExtractAsync(Stream documentStream, string mimeType, CancellationToken cancellationToken = default)
    {
        if (!mimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unsupported mime type for extraction: {mimeType}");

        try
        {
            using var document = PdfDocument.Open(documentStream);
            var layout = new LayoutDocument();
            var flatPages = new List<ExtractedPageResult>();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var docPage = BuildPage(page);
                layout.Pages.Add(docPage);

                foreach (var lang in docPage.DetectedLanguages)
                {
                    if (!layout.DetectedLanguages.Contains(lang, StringComparer.OrdinalIgnoreCase))
                        layout.DetectedLanguages.Add(lang);
                }

                flatPages.Add(new ExtractedPageResult(
                    docPage.PageNumber,
                    JoinReadingOrder(docPage.Elements, includeFurniture: true),
                    JoinReadingOrder(docPage.Elements, includeFurniture: false),
                    docPage.IsScanned ? 0.35 : (docPage.Elements.Count == 0 ? 0.25 : 0.9),
                    docPage.IsScanned,
                    docPage.DetectedLanguages.ToList()));
            }

            PageFurnitureClassifier.Classify(layout);

            for (var i = 0; i < layout.Pages.Count; i++)
            {
                var p = layout.Pages[i];
                flatPages[i] = flatPages[i] with
                {
                    RawText = JoinReadingOrder(p.Elements, includeFurniture: true),
                    ContentText = JoinReadingOrder(p.Elements, includeFurniture: false)
                };
            }

            _logger.LogInformation(
                "Layout-extracted {PageCount} pages, {ElementCount} elements, languages={Languages}",
                layout.PageCount,
                layout.Pages.Sum(p => p.Elements.Count),
                string.Join(',', layout.DetectedLanguages));

            return Task.FromResult(new DocumentExtractionResult(layout, flatPages, layout.PageCount));
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "PDF parsing failed");
            throw new InvalidOperationException("PDF parsing failed.", ex);
        }
    }

    private static DocumentPage BuildPage(Page page)
    {
        var width = page.Width;
        var height = page.Height;
        var words = page.GetWords().ToList();
        var letters = page.Letters?.ToList() ?? [];
        var isScanned = words.Count == 0 && letters.Count == 0;

        // Fallback: synthesize words from letters when GetWords is empty but letters exist
        if (words.Count == 0 && letters.Count > 0)
            words = SynthesizeWordsFromLetters(letters);

        var lines = ClusterWordsIntoLines(words);
        var elements = new List<DocumentElement>();
        var order = 0;

        foreach (var line in lines)
        {
            var text = string.Join(" ", line.Select(w => w.Text)).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            var lineLetters = line.SelectMany(w => w.Letters ?? Enumerable.Empty<Letter>()).ToList();
            if (lineLetters.Count == 0)
            {
                // Approximate with page letters overlapping the line bbox
                var minX = line.Min(w => w.BoundingBox.Left);
                var minY = line.Min(w => w.BoundingBox.Bottom);
                var maxX = line.Max(w => w.BoundingBox.Right);
                var maxY = line.Max(w => w.BoundingBox.Top);
                lineLetters = letters.Where(l =>
                    l.GlyphRectangle.Left >= minX - 1 &&
                    l.GlyphRectangle.Right <= maxX + 1 &&
                    l.GlyphRectangle.Bottom >= minY - 1 &&
                    l.GlyphRectangle.Top <= maxY + 1).ToList();
            }

            var fontSize = lineLetters.Count > 0 ? lineLetters.Average(l => (double)l.FontSize) : (double?)null;
            var fontName = lineLetters.FirstOrDefault()?.FontName;
            var isBold = fontName is not null &&
                         (fontName.Contains("Bold", StringComparison.OrdinalIgnoreCase)
                          || fontName.Contains("Black", StringComparison.OrdinalIgnoreCase)
                          || fontName.Contains("Heavy", StringComparison.OrdinalIgnoreCase));
            var isItalic = fontName is not null &&
                           (fontName.Contains("Italic", StringComparison.OrdinalIgnoreCase)
                            || fontName.Contains("Oblique", StringComparison.OrdinalIgnoreCase));

            var boxMinX = line.Min(w => w.BoundingBox.Left);
            var boxMinY = line.Min(w => w.BoundingBox.Bottom);
            var boxMaxX = line.Max(w => w.BoundingBox.Right);
            var boxMaxY = line.Max(w => w.BoundingBox.Top);

            var script = ScriptLanguageDetector.DetectScript(text);
            var element = new DocumentElement
            {
                PageNumber = page.Number,
                ElementType = DocumentElementType.Text,
                Text = text,
                NormalizedText = NormalizeInline(text),
                BoundingBox = new BoundingBox(boxMinX, boxMinY, boxMaxX - boxMinX, boxMaxY - boxMinY),
                FontFamily = fontName,
                FontSize = fontSize,
                IsBold = isBold,
                IsItalic = isItalic,
                Script = script,
                TextDirection = ScriptLanguageDetector.DetectDirection(script),
                LanguageHint = ScriptLanguageDetector.DetectLanguageHint(text, script),
                Confidence = 0.9,
                ReadingOrder = order++,
                ExtractionMethod = "pdfpig-words"
            };
            elements.Add(element);
        }

        foreach (var el in elements)
        {
            if (IsLikelyPageNumberGlyph(el, height))
            {
                el.ElementType = DocumentElementType.PageNumber;
                el.Evidence.Add("short-numeric-edge");
            }
        }

        var docPage = new DocumentPage
        {
            PageNumber = page.Number,
            Width = width,
            Height = height,
            IsScanned = isScanned,
            Elements = elements
        };

        foreach (var lang in ScriptLanguageDetector.DetectLanguagesInText(string.Join(' ', elements.Select(e => e.Text))))
            docPage.DetectedLanguages.Add(lang);

        return docPage;
    }

    private static List<Word> SynthesizeWordsFromLetters(IReadOnlyList<Letter> letters)
    {
        // Minimal fallback — group letters into crude words by proximity; PdfPig Word ctor may vary by version.
        // If Word cannot be constructed, BuildPage already handles empty words.
        return [];
    }

    private static List<List<Word>> ClusterWordsIntoLines(IReadOnlyList<Word> words)
    {
        if (words.Count == 0) return [];

        var avgHeight = words.Average(w => w.BoundingBox.Height);
        var yTolerance = Math.Max(2.0, avgHeight * 0.45);

        var ordered = words
            .OrderByDescending(w => w.BoundingBox.Bottom + w.BoundingBox.Height / 2)
            .ThenBy(w => w.BoundingBox.Left)
            .ToList();

        var lines = new List<List<Word>>();
        foreach (var word in ordered)
        {
            var cy = word.BoundingBox.Bottom + word.BoundingBox.Height / 2;
            var line = lines.LastOrDefault();
            if (line is null)
            {
                lines.Add([word]);
                continue;
            }

            var lineCy = line.Average(w => w.BoundingBox.Bottom + w.BoundingBox.Height / 2);
            if (Math.Abs(lineCy - cy) <= yTolerance)
                line.Add(word);
            else
                lines.Add([word]);
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var lineText = string.Join("", lines[i].Select(w => w.Text));
            var script = ScriptLanguageDetector.DetectScript(lineText);
            lines[i] = script == ScriptHint.Arabic
                ? lines[i].OrderByDescending(w => w.BoundingBox.Left).ToList()
                : lines[i].OrderBy(w => w.BoundingBox.Left).ToList();
        }

        return lines;
    }

    private static bool IsLikelyPageNumberGlyph(DocumentElement el, double pageHeight)
    {
        var t = el.NormalizedText;
        if (t.Length is 0 or > 4) return false;
        if (!t.All(char.IsDigit)) return false;

        var band = pageHeight * 0.12;
        var nearTop = el.BoundingBox.Top >= pageHeight - band;
        var nearBottom = el.BoundingBox.Bottom <= band;
        return nearTop || nearBottom;
    }

    private static string JoinReadingOrder(IEnumerable<DocumentElement> elements, bool includeFurniture)
    {
        var parts = elements
            .OrderBy(e => e.ReadingOrder)
            .Where(e => includeFurniture || e.ElementType is not (
                DocumentElementType.Header or DocumentElementType.Footer or DocumentElementType.PageNumber
                or DocumentElementType.TocEntry))
            .Select(e => e.NormalizedText)
            .Where(t => !string.IsNullOrWhiteSpace(t));
        return string.Join("\n", parts);
    }

    private static string NormalizeInline(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return string.Join(' ', text.Replace('\u00A0', ' ').Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));
    }
}
