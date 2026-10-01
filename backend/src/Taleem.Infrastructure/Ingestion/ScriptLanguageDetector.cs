using System.Globalization;
using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Script/language hints from Unicode ranges only — no book-specific vocabulary.
/// </summary>
public static class ScriptLanguageDetector
{
    public static ScriptHint DetectScript(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return ScriptHint.Unknown;

        var arabic = 0;
        var latin = 0;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsDigit(c)) continue;
            if (IsArabicScript(c)) arabic++;
            else if (IsLatinScript(c)) latin++;
        }

        if (arabic == 0 && latin == 0) return ScriptHint.Unknown;
        if (arabic > 0 && latin > 0)
        {
            var ratio = (double)Math.Min(arabic, latin) / Math.Max(arabic, latin);
            return ratio > 0.15 ? ScriptHint.Mixed : (arabic > latin ? ScriptHint.Arabic : ScriptHint.Latin);
        }

        return arabic > 0 ? ScriptHint.Arabic : ScriptHint.Latin;
    }

    public static TextDirectionHint DetectDirection(ScriptHint script) => script switch
    {
        ScriptHint.Arabic => TextDirectionHint.Rtl,
        ScriptHint.Latin => TextDirectionHint.Ltr,
        ScriptHint.Mixed => TextDirectionHint.Mixed,
        _ => TextDirectionHint.Unknown
    };

    /// <summary>BCP-47-ish hint: ar / en / ur / und. Urdu shares Arabic script — mark as ur only with Urdu-specific code points.</summary>
    public static string DetectLanguageHint(string text, ScriptHint script)
    {
        if (script == ScriptHint.Latin) return "en";
        if (script is ScriptHint.Arabic or ScriptHint.Mixed)
        {
            if (ContainsUrduSpecific(text)) return "ur";
            return script == ScriptHint.Mixed ? "und" : "ar";
        }
        return "und";
    }

    public static IReadOnlyList<string> DetectLanguagesInText(string text)
    {
        var langs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return ["und"];

        var script = DetectScript(text);
        langs.Add(DetectLanguageHint(text, script));
        if (script == ScriptHint.Mixed)
        {
            langs.Add("en");
            langs.Add(ContainsUrduSpecific(text) ? "ur" : "ar");
        }

        return langs.ToList();
    }

    public static bool IsArabicScript(char c)
        => c is >= '\u0600' and <= '\u06FF'
            or >= '\u0750' and <= '\u077F'
            or >= '\u08A0' and <= '\u08FF'
            or >= '\uFB50' and <= '\uFDFF'
            or >= '\uFE70' and <= '\uFEFF';

    private static bool IsLatinScript(char c)
    {
        var cat = CharUnicodeInfo.GetUnicodeCategory(c);
        return cat is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
               && c < '\u0250';
    }

    private static bool ContainsUrduSpecific(string text)
        => text.Any(c => c is '\u0679' or '\u0688' or '\u0691' or '\u06BA' or '\u06BE' or '\u06C1' or '\u06D2' or '\u06D3' or '\u06AF' or '\u067E');
}
