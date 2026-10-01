using System.Text.RegularExpressions;
using Taleem.Application.Abstractions.Ingestion;

namespace Taleem.Infrastructure.Ingestion;

public sealed partial class BookNormalizer : IBookNormalizer
{
    public NormalizedPage Normalize(ExtractedPageResult page)
        => new(page.PageNumber, NormalizePageText(page.ContentText), page.Confidence);

    public string NormalizePageText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var text = rawText.Replace("\r\n", "\n").Replace('\r', '\n');
        text = text.Replace('\u00A0', ' ');
        text = SoftHyphen().Replace(text, string.Empty);
        text = MultiSpace().Replace(text, " ");
        text = MultiNewline().Replace(text, "\n\n");
        return text.Trim();
    }

    [GeneratedRegex(@"\u00AD")]
    private static partial Regex SoftHyphen();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultiSpace();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultiNewline();
}
