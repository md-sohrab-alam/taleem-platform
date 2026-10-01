using Taleem.Application.Common;
using Taleem.Application.DTOs;
using Taleem.Domain.Entities;
using Taleem.Domain.Enums;

namespace Taleem.Application.Mapping;

public static class DtoMapper
{
    public static BookSummaryDto ToSummary(Book book) => new(
        book.Id,
        book.Slug,
        book.Title,
        book.Subtitle,
        book.Author,
        book.Description,
        book.CoverUrl,
        book.DefaultLanguage,
        StatusNames.Content(book.Status),
        book.PublishedAt);

    public static ContentBlockDto ToBlock(ContentBlock b) => new(
        b.Id,
        ToBlockTypeString(b.BlockType),
        b.Language,
        b.Text,
        b.OrderIndex,
        b.SourcePage,
        b.SourceReference,
        b.VerificationStatus.ToString().ToUpperInvariant());

    public static TaleemUnitSummaryDto ToUnitSummary(TaleemUnit u) => new(
        u.Id,
        u.ChapterId,
        u.Title,
        u.EstimatedReadingSeconds,
        u.SourcePageStart,
        u.SourcePageEnd,
        u.OrderIndex);

    public static TopicDto ToTopic(Topic t) => new(t.Id, t.Slug, t.DisplayName);

    public static ChapterSummaryDto ToChapterSummary(Chapter c, int unitCount) => new(
        c.Id,
        c.Title,
        c.Description,
        c.Slug,
        c.OrderIndex,
        c.SourcePageStart,
        c.SourcePageEnd,
        c.SectionId,
        unitCount);

    public static string ToBlockTypeString(BlockType type) => type switch
    {
        BlockType.Heading => "HEADING",
        BlockType.Paragraph => "PARAGRAPH",
        BlockType.QuranArabic => "QURAN_ARABIC",
        BlockType.QuranTranslation => "QURAN_TRANSLATION",
        BlockType.HadithArabic => "HADITH_ARABIC",
        BlockType.HadithTranslation => "HADITH_TRANSLATION",
        BlockType.Transliteration => "TRANSLITERATION",
        BlockType.Narration => "NARRATION",
        BlockType.Explanation => "EXPLANATION",
        BlockType.Reference => "REFERENCE",
        BlockType.Dua => "DUA",
        BlockType.Instruction => "INSTRUCTION",
        BlockType.List => "LIST",
        BlockType.Note => "NOTE",
        _ => "OTHER"
    };

    public static BlockType ParseBlockType(string value) => value.Trim().ToUpperInvariant() switch
    {
        "HEADING" => BlockType.Heading,
        "PARAGRAPH" => BlockType.Paragraph,
        "QURAN_ARABIC" => BlockType.QuranArabic,
        "QURAN_TRANSLATION" => BlockType.QuranTranslation,
        "HADITH_ARABIC" => BlockType.HadithArabic,
        "HADITH_TRANSLATION" => BlockType.HadithTranslation,
        "TRANSLITERATION" => BlockType.Transliteration,
        "NARRATION" => BlockType.Narration,
        "EXPLANATION" => BlockType.Explanation,
        "REFERENCE" => BlockType.Reference,
        "DUA" => BlockType.Dua,
        "INSTRUCTION" => BlockType.Instruction,
        "LIST" => BlockType.List,
        "NOTE" => BlockType.Note,
        _ => BlockType.Other
    };

    public static ReviewStatus ParseReviewStatus(string value) => value.Trim().ToUpperInvariant() switch
    {
        "APPROVED" => ReviewStatus.Approved,
        "REJECTED" => ReviewStatus.Rejected,
        "NEEDS_EDITS" => ReviewStatus.NeedsEdits,
        _ => ReviewStatus.Pending
    };
}
