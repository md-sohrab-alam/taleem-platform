using System.Globalization;
using System.Text.RegularExpressions;
using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Document-wide logical text reconstruction: physical PDF lines → paragraphs / list items / titles.
/// A line break is never by itself a ContentBlock boundary. No book-specific vocabulary.
/// </summary>
public static partial class LogicalTextReconstructor
{
    public static void Reconstruct(LayoutDocument document, IReadOnlyList<TocEntry>? toc = null)
    {
        var tocTitles = (toc ?? Array.Empty<TocEntry>())
            .Select(t => Normalize(t.Title))
            .Where(t => t.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pages = document.Pages.OrderBy(p => p.PageNumber).ToList();
        var furniture = new Dictionary<int, List<DocumentElement>>();
        var stream = new List<DocumentElement>();

        foreach (var page in pages)
        {
            furniture[page.PageNumber] = [];
            foreach (var el in page.Elements.OrderBy(e => e.ReadingOrder))
            {
                if (el.ElementType is DocumentElementType.Text or DocumentElementType.ListItem
                    && el.IncludeInReading)
                    stream.Add(CloneMutable(el));
                else
                    furniture[page.PageNumber].Add(el);
            }
        }

        var logical = GroupLogical(stream, tocTitles);
        WriteBack(pages, furniture, logical);
    }

    private static List<DocumentElement> GroupLogical(
        List<DocumentElement> stream,
        HashSet<string> tocTitles)
    {
        var result = new List<DocumentElement>();
        DocumentElement? open = null;
        var openIsList = false;
        var openIsTitle = false;

        void Flush()
        {
            if (open is null) return;
            result.Add(open);
            open = null;
            openIsList = false;
            openIsTitle = false;
        }

        for (var i = 0; i < stream.Count; i++)
        {
            var el = stream[i];
            var text = el.NormalizedText.Trim();
            if (text.Length == 0) continue;

            var isListStart = LooksLikeListStart(text);
            var isHeadingLike = LooksLikeHeadingLine(el);

            if (open is null)
            {
                open = CloneMutable(el);
                openIsList = isListStart || el.ElementType == DocumentElementType.ListItem;
                openIsTitle = isHeadingLike && !openIsList;
                if (openIsList)
                    open.ElementType = DocumentElementType.ListItem;
                continue;
            }

            // Multi-line title / opener heading reconstruction
            if (openIsTitle && !openIsList && ShouldMergeTitleLine(open, el, tocTitles))
            {
                // Subtitle style ("Title, subtitle") only when the next line is Title Case;
                // lowercase wrap tails join with a space so TOC matching still works.
                var subtitle = !StartsContinuation(el.NormalizedText) && char.IsUpper(el.NormalizedText.TrimStart()[0]);
                MergeInto(open, el, titleStyle: subtitle);
                open.Evidence.Add("title-line-merge");
                continue;
            }

            // New list item starts → close previous
            if (isListStart)
            {
                Flush();
                open = CloneMutable(el);
                open.ElementType = DocumentElementType.ListItem;
                openIsList = true;
                openIsTitle = false;
                continue;
            }

            // List item continuation (same item, possibly across pages)
            if (openIsList && ShouldContinueListItem(open, el))
            {
                MergeInto(open, el);
                open.Evidence.Add("list-item-continue");
                continue;
            }

            // Heading boundary stops prose merge
            if (isHeadingLike && !StartsContinuation(text) && !EndsWithFunctionWord(open.NormalizedText)
                && !ShouldMergeTitleLine(open, el, tocTitles))
            {
                Flush();
                open = CloneMutable(el);
                openIsList = false;
                openIsTitle = true;
                continue;
            }

            // Ordinary / cross-page paragraph continuation
            if (!openIsList && ShouldContinueParagraph(open, el))
            {
                MergeInto(open, el);
                open.Evidence.Add("paragraph-continue");
                // Title cluster ends once body prose is attached
                if (open.NormalizedText.Length > 90)
                    openIsTitle = false;
                continue;
            }

            Flush();
            open = CloneMutable(el);
            openIsList = isListStart || el.ElementType == DocumentElementType.ListItem;
            openIsTitle = LooksLikeHeadingLine(el) && !openIsList;
            if (openIsList)
                open.ElementType = DocumentElementType.ListItem;
        }

        Flush();
        return result;
    }

    private static void WriteBack(
        List<DocumentPage> pages,
        Dictionary<int, List<DocumentElement>> furniture,
        List<DocumentElement> logical)
    {
        var byPage = pages.ToDictionary(p => p.PageNumber, _ => new List<DocumentElement>());
        foreach (var el in logical)
            byPage[el.PageNumber].Add(el);

        foreach (var page in pages)
        {
            var reading = byPage[page.PageNumber];
            var nonReading = furniture[page.PageNumber];
            page.Elements.Clear();
            var order = 0;
            foreach (var el in nonReading.OrderBy(e => e.ReadingOrder).ThenByDescending(e => e.BoundingBox.CenterY))
            {
                el.ReadingOrder = order++;
                page.Elements.Add(el);
            }

            foreach (var el in reading)
            {
                el.ReadingOrder = order++;
                page.Elements.Add(el);
            }
        }
    }

    private static bool ShouldMergeTitleLine(
        DocumentElement title,
        DocumentElement next,
        HashSet<string> tocTitles)
    {
        if (LooksLikeListStart(next.NormalizedText)) return false;
        if (IsHardSentenceEnd(title.NormalizedText)) return false;
        // Wrap tails like "… part of" are paragraph continuations, not multi-line titles
        if (EndsWithFunctionWord(title.NormalizedText)) return false;

        var a = title.NormalizedText.Trim();
        var b = next.NormalizedText.Trim();
        if (b.Length is < 2 or > 70) return false;
        if (a.Length > 60 || CountWords(a) > 8) return false;
        if (b.Contains(". ") || EndsWithSentencePunctuation(b)) return false;
        if (CountWords(b) > 8) return false;
        if (!LooksLikeHeadingLine(title) && !(title.IsBold || title.FontSize is >= 13))
            return false;

        // Strong: TOC contains joined forms
        var joinedComma = Normalize(a + ", " + b);
        var joinedSpace = Normalize(a + " " + b);
        if (tocTitles.Contains(joinedComma) || tocTitles.Contains(joinedSpace))
            return true;
        if (tocTitles.Any(t => t.StartsWith(Normalize(a), StringComparison.OrdinalIgnoreCase)
                               && t.Contains(Normalize(b), StringComparison.OrdinalIgnoreCase)))
            return true;

        // Typographic subtitle: short, close vertically, similar alignment, not body prose
        var gap = title.BoundingBox.CenterY - next.BoundingBox.CenterY
                  - (title.BoundingBox.Height + next.BoundingBox.Height) / 2;
        if (gap > Math.Max(28, (title.FontSize ?? 12) * 2.2))
            return false;

        var xDelta = Math.Abs(title.BoundingBox.CenterX - next.BoundingBox.CenterX);
        var aligned = xDelta < Math.Max(40, title.BoundingBox.Width * 0.35)
                      || Math.Abs(title.BoundingBox.X - next.BoundingBox.X) < 24;
        if (!aligned) return false;

        // Next should look like a title fragment, not a sentence start of body
        if (CountWords(b) <= 6 && !StartsContinuation(b) && char.IsUpper(b[0]))
            return true;

        return false;
    }

    private static bool ShouldContinueListItem(DocumentElement open, DocumentElement next)
    {
        if (LooksLikeListStart(next.NormalizedText)) return false;
        if (LooksLikeHeadingLine(next) && !StartsContinuation(next.NormalizedText)
            && IsHardSentenceEnd(open.NormalizedText))
            return false;

        if (IsHyphenWrap(open.NormalizedText) || EndsWithFunctionWord(open.NormalizedText))
            return true;

        // Lowercase / Arabic continuation only when the list line is not a finished sentence
        if (StartsContinuation(next.NormalizedText) && !IsHardSentenceEnd(open.NormalizedText))
            return true;

        // Soft-ended list line + similar style / indent → same item
        if (!IsHardSentenceEnd(open.NormalizedText)
            && SimilarFont(open, next)
            && SimilarIndent(open, next)
            && !LooksLikeHeadingLine(next)
            && VerticalGapOk(open, next))
            return true;

        // Cross-page: soft end + non-heading next
        if (open.PageNumber != next.PageNumber
            && !IsHardSentenceEnd(open.NormalizedText)
            && !LooksLikeHeadingLine(next)
            && !LooksLikeListStart(next.NormalizedText))
            return true;

        return false;
    }

    private static bool ShouldContinueParagraph(DocumentElement open, DocumentElement next)
    {
        if (LooksLikeListStart(next.NormalizedText)) return false;
        if (LooksLikeHeadingLine(next) && IsHardSentenceEnd(open.NormalizedText)
            && !EndsWithFunctionWord(open.NormalizedText))
            return false;
        if (LooksLikeHeadingLine(next) && open.NormalizedText.Length > 40
            && !EndsWithFunctionWord(open.NormalizedText)
            && !StartsContinuation(next.NormalizedText)
            && IsHardSentenceEnd(open.NormalizedText))
            return false;

        if (EndsWithFunctionWord(open.NormalizedText) && next.NormalizedText.Length <= 100)
            return true;

        if (IsHyphenWrap(open.NormalizedText))
            return true;

        // Lowercase continuation only after soft ends (not after .!? finished sentences)
        if (StartsContinuation(next.NormalizedText) && !IsHardSentenceEnd(open.NormalizedText)
            && (VerticalGapOk(open, next) || open.PageNumber != next.PageNumber))
            return true;

        // Soft end + similar font: classic wrap, including Capitalized continuations
        // ("… describes" / "Himself in the …", "… only for" / "Compassionate …")
        if (!IsHardSentenceEnd(open.NormalizedText)
            && SimilarFont(open, next)
            && !LooksLikeHeadingLine(next)
            && (VerticalGapOk(open, next) || open.PageNumber != next.PageNumber)
            && open.NormalizedText.Length >= 20)
            return true;

        return false;
    }

    private static bool IsHyphenWrap(string text)
    {
        var t = text.TrimEnd();
        return t.EndsWith('-') || t.EndsWith('‐');
    }

    private static bool VerticalGapOk(DocumentElement a, DocumentElement b)
    {
        if (a.PageNumber != b.PageNumber) return true; // cross-page handled by soft-end rules
        var gap = a.BoundingBox.CenterY - b.BoundingBox.CenterY
                  - (a.BoundingBox.Height + b.BoundingBox.Height) / 2;
        var threshold = Math.Max(16, (a.FontSize ?? 11) * 1.65);
        return gap <= threshold;
    }

    private static bool SimilarIndent(DocumentElement a, DocumentElement b)
        => Math.Abs(a.BoundingBox.X - b.BoundingBox.X) <= 36;

    private static bool SimilarFont(DocumentElement a, DocumentElement b)
    {
        if (a.FontSize is double fa && b.FontSize is double fb)
            return Math.Abs(fa - fb) <= 0.9;
        return true;
    }

    private static bool LooksLikeHeadingLine(DocumentElement el)
    {
        var t = el.NormalizedText.Trim();
        if (t.Length is < 2 or > 90) return false;
        if (LooksLikeListStart(t)) return false;
        if (t.Contains(". ") && CountWords(t) > 6) return false;
        var sizeBoost = el.FontSize is >= 13;
        return (el.IsBold || sizeBoost)
               && CountWords(t) <= 16
               && !EndsWithSentencePunctuation(t);
    }

    private static bool LooksLikeListStart(string text)
    {
        var t = text.TrimStart();
        if (t.Length < 2) return false;
        if (t[0] is '-' or '*' or '•' or '●' or '○' or '▪' or '–' or '—')
            return t.Length > 2 && (t[1] == ' ' || char.IsLetterOrDigit(t[1]));
        return ListStart().IsMatch(t);
    }

    private static bool IsHardSentenceEnd(string text) => EndsWithSentencePunctuation(text);

    /// <summary>Sentence-ending punctuation including Latin and Arabic/Urdu marks.</summary>
    public static bool EndsWithSentencePunctuation(string text)
    {
        var t = text.TrimEnd();
        if (t.Length == 0) return true;
        // Strip trailing quotes/brackets then test
        while (t.Length > 0 && t[^1] is '"' or '\'' or '”' or '’' or '»' or ')' or ']' or '»')
            t = t[..^1].TrimEnd();
        if (t.Length == 0) return true;
        var c = t[^1];
        return c is '.' or '!' or '?' or '۔' or '؟' or '…' or '！' or '？';
    }

    private static bool EndsWithFunctionWord(string text)
    {
        var t = text.TrimEnd().TrimEnd(',', ';', ':');
        var parts = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        var last = parts[^1].TrimEnd(',', ';', ':').ToLowerInvariant();
        return last is "of" or "the" or "a" or "an" or "and" or "or" or "to" or "for" or "with" or "by"
            or "in" or "on" or "as" or "from" or "that" or "which" or "who" or "whom" or "whose"
            or "between" or "until" or "till" or "into" or "onto" or "about" or "over" or "under";
    }

    private static bool StartsContinuation(string text)
    {
        var t = text.TrimStart();
        if (t.Length == 0) return false;
        var c = t[0];
        if (char.IsLower(c)) return true;
        // Arabic script often continues without case
        if (c is >= '\u0600' and <= '\u06FF') return true;
        return false;
    }

    private static int CountWords(string text)
        => text.Split([' ', '\t', '\u00A0'], StringSplitOptions.RemoveEmptyEntries).Length;

    private static string Normalize(string text)
        => string.Join(' ', text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static DocumentElement CloneMutable(DocumentElement el)
        => new()
        {
            Id = el.Id,
            PageNumber = el.PageNumber,
            ElementType = el.ElementType,
            Text = el.Text,
            NormalizedText = el.NormalizedText,
            BoundingBox = el.BoundingBox,
            FontFamily = el.FontFamily,
            FontSize = el.FontSize,
            IsBold = el.IsBold,
            IsItalic = el.IsItalic,
            TextDirection = el.TextDirection,
            Script = el.Script,
            LanguageHint = el.LanguageHint,
            Confidence = el.Confidence,
            ReadingOrder = el.ReadingOrder,
            ExtractionMethod = el.ExtractionMethod,
            IncludeInReading = el.IncludeInReading
        };

    private static void MergeInto(DocumentElement open, DocumentElement next, bool titleStyle = false)
    {
        var leftRaw = open.NormalizedText.TrimEnd();
        var right = next.NormalizedText.TrimStart();
        string joined;
        if (leftRaw.EndsWith('-') || leftRaw.EndsWith('‐'))
            joined = leftRaw.TrimEnd('-', '‐') + right;
        else if (titleStyle)
        {
            var sep = leftRaw.EndsWith(',') ? " " : ", ";
            joined = leftRaw + sep + right;
        }
        else
            joined = leftRaw + " " + right;

        open.NormalizedText = joined;
        open.Text = joined;
        open.BoundingBox = Union(open.BoundingBox, next.BoundingBox);
        // Preserve heading emphasis across wraps (either line bold ⇒ keep bold signal)
        open.IsBold = open.IsBold || next.IsBold;
        if (next.FontSize is double nfs && (open.FontSize is null || nfs > open.FontSize))
            open.FontSize = nfs;
        open.Confidence = Math.Min(open.Confidence, next.Confidence);
        open.Evidence.Add("line-wrap-merge");
        // Keep start page for provenance; unit page-end comes from later elements
        if (next.ElementType == DocumentElementType.ListItem)
            open.ElementType = DocumentElementType.ListItem;
    }

    private static BoundingBox Union(BoundingBox a, BoundingBox b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        var r = Math.Max(a.Right, b.Right);
        var t = Math.Max(a.Top, b.Top);
        return new BoundingBox(x, y, Math.Max(0, r - x), Math.Max(0, t - y));
    }

    /// <summary>
    /// 1) 2) 3) (a) a) ١) etc. — start of a new list item, not a continuation.
    /// </summary>
    [GeneratedRegex(@"^((\d{1,3}|[IVXLCivxlc]{1,6}|[A-Za-z]|[\u0660-\u0669]{1,3}|[\u06F0-\u06F9]{1,3})([\.\):\-]|[\u060C])|\(\s*(\d{1,3}|[A-Za-z])\s*\))\s+\S")]
    private static partial Regex ListStart();
}
