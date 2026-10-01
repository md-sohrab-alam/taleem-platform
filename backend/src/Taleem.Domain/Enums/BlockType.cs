namespace Taleem.Domain.Enums;

/// <summary>
/// Extensible content block types. Prefer adding new values over overloading existing ones.
/// </summary>
public enum BlockType
{
    Heading = 0,
    Paragraph = 1,
    QuranArabic = 2,
    QuranTranslation = 3,
    HadithArabic = 4,
    HadithTranslation = 5,
    Transliteration = 6,
    Narration = 7,
    Explanation = 8,
    Reference = 9,
    Dua = 10,
    Instruction = 11,
    List = 12,
    Note = 13,
    Other = 100
}
