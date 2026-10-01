using Microsoft.Extensions.Options;
using System.Text.Json;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.DocumentIr;
using Taleem.Application.Options;
using Taleem.Domain.Enums;
using Taleem.Infrastructure.Ingestion;

namespace Taleem.Tests;

public class LogicalReconstructionTests
{
    [Fact]
    public void List_Item_Wrapped_Over_Multiple_Lines_Merges()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1,
                    Line(1, 0, "Topic", 16, true, 700, 100),
                    Line(1, 1, "1) Morning prayer can be performed between dawn and", 11, false, 650, 100),
                    Line(1, 2, "sunrise and there are two units in this prayer.", 11, false, 620, 100),
                    Line(1, 3, "2) Afternoon prayer is performed later in the day.", 11, false, 580, 100))
            ]
        };

        LogicalTextReconstructor.Reconstruct(layout);
        var lines = Reading(layout);
        Assert.Contains(lines, t =>
            t.Contains("dawn and", StringComparison.OrdinalIgnoreCase)
            && t.Contains("sunrise", StringComparison.OrdinalIgnoreCase)
            && t.StartsWith("1)", StringComparison.Ordinal));
        Assert.Contains(lines, t => t.StartsWith("2)", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, t => t.Equals("sunrise and there are two units in this prayer.", StringComparison.Ordinal));
    }

    [Fact]
    public void List_Item_Continues_Across_Page_Without_Merging_Next_Number()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1,
                    Line(1, 0, "Steps", 16, true, 700, 100),
                    Line(1, 1, "1) First step continues onto the next", 11, false, 650, 100)),
                Page(2,
                    Line(2, 0, "physical page with more explanation text.", 11, false, 700, 100),
                    Line(2, 1, "2) Second step stays separate from the first.", 11, false, 650, 100))
            ]
        };

        LogicalTextReconstructor.Reconstruct(layout);
        var lines = Reading(layout);
        Assert.Contains(lines, t =>
            t.StartsWith("1)", StringComparison.Ordinal)
            && t.Contains("next", StringComparison.OrdinalIgnoreCase)
            && t.Contains("physical page", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, t => t.StartsWith("2)", StringComparison.Ordinal));
        Assert.Equal(1, lines.Count(t => t.StartsWith("1)", StringComparison.Ordinal)));
    }

    [Fact]
    public void Ordinary_Paragraph_Wrap_And_Cross_Page_Merge()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1,
                    Line(1, 0, "Section One", 16, true, 700, 100),
                    Line(1, 1, "The teacher describes", 11, false, 650, 100),
                    Line(1, 2, "Himself in the source as merciful and kind toward learners.", 11, false, 620, 100)),
                Page(2,
                    Line(2, 0, "Praise is only for", 11, false, 700, 100),
                    Line(2, 1, "Compassionate, the Most Merciful; Master of the Day.", 11, false, 660, 100),
                    Line(2, 2, "New Heading Here", 16, true, 600, 100),
                    Line(2, 3, "Independent paragraph after the heading with enough body text here.", 11, false, 560, 100))
            ]
        };

        LogicalTextReconstructor.Reconstruct(layout);
        var lines = Reading(layout);
        Assert.Contains(lines, t =>
            t.Contains("describes", StringComparison.OrdinalIgnoreCase)
            && t.Contains("Himself in the source", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, t =>
            t.Contains("Praise is only for", StringComparison.OrdinalIgnoreCase)
            && t.Contains("Compassionate, the Most Merciful", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, t => t.Equals("New Heading Here", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, t =>
            t.Contains("Praise is only for", StringComparison.OrdinalIgnoreCase)
            && t.Contains("New Heading Here", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Multi_Line_Title_Uses_Toc_Evidence()
    {
        var toc = new List<TocEntry>
        {
            new() { Title = "Opening Topic, Secondary Phrase", PageNumber = 1, SourcePage = 1, Confidence = 0.9 }
        };
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1,
                    Line(1, 0, "Opening Topic", 18, true, 700, 200),
                    Line(1, 1, "Secondary Phrase", 14, true, 660, 200),
                    Line(1, 2, "Body paragraph explaining the opening topic with enough continuous prose for a unit.", 11, false, 600, 100))
            ]
        };

        LogicalTextReconstructor.Reconstruct(layout, toc);
        var lines = Reading(layout);
        Assert.Contains(lines, t =>
            t.Contains("Opening Topic", StringComparison.OrdinalIgnoreCase)
            && t.Contains("Secondary Phrase", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(lines, t => t.Equals("Secondary Phrase", StringComparison.Ordinal));
    }

    [Fact]
    public void Short_Sentence_Ending_With_Bang_Or_Question_Is_Not_Suspicious()
    {
        Assert.True(LogicalTextReconstructor.EndsWithSentencePunctuation("This completes our lesson!"));
        Assert.True(LogicalTextReconstructor.EndsWithSentencePunctuation("Is this finished?"));
        Assert.True(LogicalTextReconstructor.EndsWithSentencePunctuation("انتهى الدرس؟"));
        Assert.False(LogicalTextReconstructor.EndsWithSentencePunctuation("and it has three units"));
    }

    [Fact]
    public async Task Analyzer_Does_Not_Flag_Short_Bang_Sentence()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1,
                    Line(1, 0, "Closing Note", 16, true, 700, 100),
                    Line(1, 1, "We finish the practice carefully and with gratitude in our hearts today.", 11, false, 650, 100),
                    Line(1, 2, "This completes our lesson!", 11, false, 600, 100))
            ]
        };

        var structure = await new LayoutAwareStructureAnalyzer().AnalyzeAsync("Book", layout);
        var flagged = structure.Units.SelectMany(u => u.Blocks)
            .Where(b => b.VerificationStatus == VerificationStatus.FlaggedLowConfidence)
            .Select(b => b.Text)
            .ToList();
        Assert.DoesNotContain(flagged, t => t.Contains("completes our lesson", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Suspicious_Lowercase_Fragment_Stays_Unmerged_After_Hard_Sentence()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1,
                    Line(1, 0, "Timing Notes", 16, true, 720, 100),
                    Line(1, 1, "Evening prayer ends before night fully arrives.", 11, false, 680, 100),
                    Line(1, 2, "and it has three compulsory units", 11, false, 500, 100))
            ]
        };

        LogicalTextReconstructor.Reconstruct(layout);
        var lines = Reading(layout);
        Assert.Contains(lines, t => t.Contains("Evening prayer ends", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, t => t.Equals("and it has three compulsory units", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Suspicious_Lowercase_Fragment_Remains_Flagged_When_Unmerged()
    {
        // Use a mid-book page so zone classification stays MAINCONTENT (not title-page).
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(5,
                    Line(5, 0, "Timing Notes", 16, true, 720, 40),
                    Line(5, 1, "Evening prayer ends before night fully arrives.", 11, false, 680, 40),
                    Line(5, 2, "and it has three compulsory units", 11, false, 400, 40),
                    Line(5, 3, "Additional complete guidance continues with enough prose for the unit body.", 11, false, 200, 40))
            ]
        };

        var structure = await new LayoutAwareStructureAnalyzer().AnalyzeAsync("Book", layout);
        Assert.Contains(structure.Units.SelectMany(u => u.Blocks), b =>
            b.Text.Trim().Equals("and it has three compulsory units", StringComparison.Ordinal)
            && b.VerificationStatus == VerificationStatus.FlaggedLowConfidence);
    }

    [Fact]
    public void SearchChunk_Never_Starts_Mid_Word_From_Overlap()
    {
        var prev = "Siraatal-lazeena an amta Alyhim ghayril-maghdoobi Alyhim wa lad-daaalleen Praise is only for Allah";
        var overlap = LogicalChunkGenerator.TakeOverlapAtSafeBoundary(prev, 40);
        Assert.False(string.IsNullOrEmpty(overlap));
        Assert.False(char.IsLetter(overlap[0]) && overlap.Length < 40 && prev.Contains(overlap) && !prev.EndsWith(overlap, StringComparison.Ordinal)
                     && prev[prev.LastIndexOf(overlap, StringComparison.Ordinal) - 1] is { } c && char.IsLetter(c));
        // Must start at a token boundary in previous text
        var idx = prev.LastIndexOf(overlap, StringComparison.Ordinal);
        Assert.True(idx == 0 || !char.IsLetter(prev[idx - 1]));
        Assert.True(LogicalChunkGenerator.StartsAtWordBoundary(overlap));
    }

    [Fact]
    public void SearchChunk_Unicode_Arabic_Boundary_Safe()
    {
        var prev = "بسم الله الرحمن الرحيم الحمد لله رب العالمين";
        var overlap = LogicalChunkGenerator.TakeOverlapAtSafeBoundary(prev, 20);
        if (overlap.Length == 0) return;
        var idx = prev.LastIndexOf(overlap, StringComparison.Ordinal);
        Assert.True(idx == 0 || char.IsWhiteSpace(prev[idx - 1]));
    }

    [Fact]
    public void SearchChunk_Metadata_Includes_ContentBlock_Guids()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var chapterId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var generator = new LogicalChunkGenerator(Options.Create(new ChunkingOptions
        {
            MaxChunkCharacters = 500,
            MinChunkCharacters = 20,
            OverlapCharacters = 80
        }));

        var unit = new AnalyzedUnit(
            "Unit", 0, 0, 1, 1, 60,
            [
                new AnalyzedBlock(BlockType.Heading, "en", "Title", 0, 1, "p.1", VerificationStatus.Unverified, id1),
                new AnalyzedBlock(BlockType.Paragraph, "en", new string('a', 100) + " complete sentence.", 1, 1, "p.1", VerificationStatus.Unverified, id2)
            ],
            ["Topic"]);

        var chunks = generator.Generate(unit, bookId, chapterId, unitId, "2.2.0");
        Assert.NotEmpty(chunks);
        foreach (var c in chunks)
        {
            Assert.NotEmpty(c.ContentBlockIds);
            using var doc = JsonDocument.Parse(c.MetadataJson);
            Assert.True(doc.RootElement.TryGetProperty("contentBlockIds", out var ids));
            Assert.True(ids.GetArrayLength() > 0);
            Assert.Equal("2.2.0", doc.RootElement.GetProperty("processorVersion").GetString());
            Assert.Equal(bookId.ToString(), doc.RootElement.GetProperty("bookId").GetString());
            Assert.Equal(unitId.ToString(), doc.RootElement.GetProperty("taleemUnitId").GetString());
            Assert.True(LogicalChunkGenerator.StartsAtWordBoundary(c.ChunkText));
        }
    }

    [Fact]
    public async Task Two_Chapters_May_Share_Physical_Page()
    {
        var pages = new List<DocumentPage>
        {
            Page(1,
                Line(1, 0, "CONTENTS", 16, true, 720, 40),
                Line(1, 1, "First Topic ........ 3", 11, false, 680, 40),
                Line(1, 2, "Second Topic ........ 3", 11, false, 650, 40)),
            Page(2, Line(2, 0, "Intermediate filler page with continuous prose for pagination only here.", 11, false, 700, 40)),
            Page(3,
                Line(3, 0, "First Topic", 16, true, 700, 40),
                Line(3, 1, "Body of the first topic with enough continuous prose for readers.", 11, false, 650, 40),
                Line(3, 2, "Second Topic", 16, true, 580, 40),
                Line(3, 3, "Body of the second topic also with enough continuous prose for readers.", 11, false, 540, 40))
        };

        var structure = await new LayoutAwareStructureAnalyzer().AnalyzeAsync("Sample Book", new LayoutDocument { Pages = pages });
        Assert.True(structure.Chapters.Count >= 2, $"chapters={structure.Chapters.Count}");
        var first = structure.Chapters.First(c => c.Title.Contains("First", StringComparison.OrdinalIgnoreCase));
        var second = structure.Chapters.First(c => c.Title.Contains("Second", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, first.SourcePageStart);
        Assert.Equal(3, second.SourcePageStart);
        // Inclusive provenance: prior chapter end may equal next chapter start on a shared page
        Assert.Equal(3, first.SourcePageEnd);
        Assert.Equal(3, second.SourcePageStart);
    }

    private static List<string> Reading(LayoutDocument layout)
        => layout.Pages.SelectMany(p => p.Elements)
            .Where(e => e.IncludeInReading && e.ElementType is DocumentElementType.Text or DocumentElementType.ListItem)
            .OrderBy(e => e.PageNumber).ThenBy(e => e.ReadingOrder)
            .Select(e => e.NormalizedText)
            .ToList();

    private static DocumentPage Page(int number, params DocumentElement[] elements)
        => new()
        {
            PageNumber = number,
            Width = 600,
            Height = 800,
            ZoneRole = DocumentZoneRole.MainContent,
            Elements = elements.ToList()
        };

    private static DocumentElement Line(int page, int order, string text, double fontSize, bool bold, double y, double x)
        => new()
        {
            PageNumber = page,
            ElementType = DocumentElementType.Text,
            Text = text,
            NormalizedText = text,
            FontSize = fontSize,
            IsBold = bold,
            // Keep left-aligned (not centered) so pages are not mis-zoned as title pages in fixtures
            BoundingBox = new BoundingBox(x, y, Math.Min(280, Math.Max(80, text.Length * fontSize * 0.35)), fontSize + 2),
            ReadingOrder = order,
            Script = ScriptLanguageDetector.DetectScript(text),
            LanguageHint = "en",
            TextDirection = TextDirectionHint.Ltr,
            Confidence = 0.9,
            ExtractionMethod = "test",
            IncludeInReading = true
        };
}
