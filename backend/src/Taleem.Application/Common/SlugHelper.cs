using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Taleem.Application.Common;

public static partial class SlugHelper
{
    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "book";

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (char.IsWhiteSpace(c) || c is '-' or '_')
                sb.Append('-');
        }

        var slug = MultiDash().Replace(sb.ToString().Normalize(NormalizationForm.FormC), "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "book" : slug;
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultiDash();
}
