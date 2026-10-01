using System.Text.RegularExpressions;

namespace Taleem.Application.Common;

/// <summary>
/// Filters auto-inferred title fragments into topics suitable for public browsing.
/// Book-agnostic: uses length, punctuation, and common English stopwords only.
/// </summary>
public static partial class TopicQuality
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "or", "the", "of", "in", "on", "at", "to", "for", "from", "by", "with",
        "into", "onto", "over", "under", "each", "every", "other", "others", "was", "were", "are",
        "is", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would",
        "shall", "should", "may", "might", "can", "could", "not", "no", "nor", "but", "if", "then",
        "than", "that", "this", "these", "those", "it", "its", "as", "so", "too", "very", "just",
        "also", "only", "all", "any", "some", "such", "same", "both", "few", "more", "most",
        "own", "down", "up", "out", "off", "about", "above", "after", "before", "between",
        "during", "through", "against", "among", "part", "parts", "always", "never", "how",
        "what", "when", "where", "who", "which", "why", "whom", "whose", "their", "them",
        "they", "his", "her", "him", "she", "he", "we", "you", "your", "our", "us", "my",
        "general", "chapter", "section", "page", "pages", "unit", "book", "reading",
        // Weak title fragments that read as incomplete topics
        "facing", "loving", "perform", "cycles", "postures", "relationship", "preparation",
        "islamic", "standing", "sitting", "bowing", "timings", "prayer", "prophets", "prophet",
        "jesus", "david", "pbuh", "pbut", "how", "how-to",
    };

    public static bool IsPublicWorthy(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return false;
        var cleaned = CleanDisplayName(displayName);
        if (cleaned.Length < 4) return false;
        if (int.TryParse(cleaned, out _)) return false;
        if (Stopwords.Contains(cleaned)) return false;
        if (cleaned.Contains(' ') && cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .All(Stopwords.Contains))
            return false;
        // Reject single short tokens that are mostly punctuation residue
        if (cleaned.Length <= 5 && !cleaned.Any(char.IsLetter)) return false;
        // Prefer topics that look like words/phrases, not bare connectors
        var letters = cleaned.Count(char.IsLetter);
        if (letters < 3) return false;
        return true;
    }

    public static string CleanDisplayName(string name)
    {
        var t = name.Trim().Trim(',', '.', ';', ':', '—', '-', '|', '/', '\\');
        t = MultiSpace().Replace(t, " ").Trim();
        return t;
    }

    /// <summary>Infer a small set of public-worthy topics from a unit title.</summary>
    public static IReadOnlyList<string> InferFromTitle(string title)
    {
        var cleanedTitle = CleanDisplayName(title);
        var tokens = cleanedTitle
            .Split([' ', '\t', '-', '—', ':', '/', '|', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanDisplayName)
            .Where(IsPublicWorthy)
            .Select(CapitalizeToken)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        // Prefer the full short title when tokenization yields nothing useful
        if (tokens.Count == 0 && cleanedTitle.Length is >= 4 and <= 48)
        {
            var asTopic = CapitalizeToken(cleanedTitle);
            if (IsPublicWorthy(asTopic)) tokens.Add(asTopic);
        }

        // Do not invent a placeholder "General" topic for public surfaces
        return tokens;
    }

    private static string CapitalizeToken(string t)
    {
        if (t.Length == 0) return t;
        if (t.All(char.IsUpper) && t.Length <= 5) return t; // keep short acronyms
        return char.ToUpperInvariant(t[0]) + t[1..].ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpace();
}
