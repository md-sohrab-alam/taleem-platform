using Taleem.Application.DocumentIr;
using Taleem.Infrastructure.Ingestion;

namespace Taleem.Tests;

public class StructureAnalyzerTests
{
    [Fact]
    public async Task Layout_Analyzer_Groups_Heading_With_Body_Not_Per_Line()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1, 600, 800, DocumentZoneRole.MainContent,
                    Line(1, 0, "Chapter One", 18, true, 700, 100),
                    Line(1, 1, "An explanation of the opening teaching for readers.", 11, false, 650, 100),
                    Line(1, 2, "More prose continues on the same page without a new unit.", 11, false, 620, 100)),
                Page(2, 600, 800, DocumentZoneRole.MainContent,
                    Line(2, 0, "Still the same chapter body on the next page.", 11, false, 700, 100),
                    Line(2, 1, "Second Topic", 16, true, 640, 120),
                    Line(2, 2, "Body under the second topic heading continues with enough text for a unit.", 11, false, 600, 100))
            ]
        };

        var analyzer = new LayoutAwareStructureAnalyzer();
        var structure = await analyzer.AnalyzeAsync("Sample Book", layout);

        Assert.NotEmpty(structure.Chapters);
        Assert.True(structure.Units.Count >= 1);
        Assert.True(structure.Units.Count <= 4, $"Over-segmented: {structure.Units.Count} units");
        Assert.True(structure.Units.SelectMany(u => u.Blocks).Any(b =>
            b.Text.Contains("opening teaching", StringComparison.OrdinalIgnoreCase)
            || b.Text.Contains("second topic", StringComparison.OrdinalIgnoreCase)
            || b.Text.Contains("same chapter body", StringComparison.OrdinalIgnoreCase)));
        Assert.True(structure.Units.All(u => u.Blocks.Count >= 1));
    }

    [Fact]
    public async Task Toc_Entries_Do_Not_Become_TaleemUnits()
    {
        var pages = new List<DocumentPage>
        {
            Page(1, 600, 800, DocumentZoneRole.Unknown,
                Line(1, 0, "CONTENTS", 16, true, 720, 200),
                Line(1, 1, "A Loving Relationship ........ 4", 11, false, 680, 100),
                Line(1, 2, "Sujood or Prostration by Jesus pbuh ........ 10", 11, false, 650, 100))
        };
        for (var i = 2; i <= 10; i++)
        {
            if (i == 4)
            {
                pages.Add(Page(4, 600, 800, DocumentZoneRole.Unknown,
                    Line(4, 0, "A Loving Relationship", 16, true, 700, 200),
                    Line(4, 1, "Islam teaches a relationship of love and mercy with the creator for believers.", 11, false, 650, 200),
                    Line(4, 2, "Further explanation continues across this teaching unit with enough body text.", 11, false, 600, 200)));
            }
            else if (i == 10)
            {
                pages.Add(Page(10, 600, 800, DocumentZoneRole.Unknown,
                    Line(10, 0, "Sujood or Prostration by Jesus pbuh", 16, true, 700, 200),
                    Line(10, 1, "This section explains prostration with historical context and practical guidance for readers.", 11, false, 650, 200)));
            }
            else
            {
                pages.Add(Page(i, 600, 800, DocumentZoneRole.MainContent,
                    Line(i, 0, $"Intermediate page {i} filler text for pagination.", 11, false, 700, 100)));
            }
        }

        var layout = new LayoutDocument { Pages = pages };
        var analyzer = new LayoutAwareStructureAnalyzer();
        var structure = await analyzer.AnalyzeAsync("Sample Book", layout);

        Assert.Contains(structure.TocEntries, t => t.Title.Contains("Loving Relationship", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(structure.Units, u =>
            u.SourcePageStart == 1 && u.Title.Contains("Sujood", StringComparison.OrdinalIgnoreCase));
        Assert.True(structure.Units.Count < 10, $"Expected compact units, got {structure.Units.Count}");
        Assert.Contains(structure.Units, u => u.Title.Contains("Loving Relationship", StringComparison.OrdinalIgnoreCase)
                                              || structure.Chapters.Any(c => c.Title.Contains("Loving Relationship")));
    }

    [Fact]
    public void Page_Numbers_Are_Not_Merged_Into_Heading_Text_When_Separate_Elements()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(4, 600, 800, DocumentZoneRole.MainContent,
                    new DocumentElement
                    {
                        PageNumber = 4,
                        ElementType = DocumentElementType.PageNumber,
                        Text = "4",
                        NormalizedText = "4",
                        BoundingBox = new BoundingBox(300, 20, 10, 12),
                        FontSize = 10,
                        ReadingOrder = 0,
                        IncludeInReading = false
                    },
                    Line(4, 1, "A Loving Relationship", 16, true, 700, 200),
                    Line(4, 2, "Islam teaches a relationship of love and mercy with substantial explanation text.", 11, false, 650, 200))
            ]
        };

        PageFurnitureClassifier.Classify(layout);
        var analyzer = new LayoutAwareStructureAnalyzer();
        var structure = analyzer.AnalyzeAsync("Book", layout).GetAwaiter().GetResult();

        Assert.DoesNotContain(structure.Units, u => u.Title.Contains("4A", StringComparison.Ordinal));
        Assert.Contains(structure.Units, u => u.Title.Contains("Loving Relationship", StringComparison.OrdinalIgnoreCase)
                                              || structure.Chapters.Any(c => c.Title.Contains("Loving Relationship")));
    }

    [Fact]
    public async Task Line_Wraps_Are_Merged_Into_Paragraph_Blocks()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1, 600, 800, DocumentZoneRole.MainContent,
                    Line(1, 0, "Teaching Title", 16, true, 700, 100),
                    Line(1, 1, "This sentence is split across lines so the reader sees", 11, false, 650, 100),
                    Line(1, 2, "a continuation that should become one paragraph block.", 11, false, 620, 100))
            ]
        };

        var analyzer = new LayoutAwareStructureAnalyzer();
        var structure = await analyzer.AnalyzeAsync("Book", layout);
        var body = structure.Units.SelectMany(u => u.Blocks)
            .Where(b => b.BlockType != Taleem.Domain.Enums.BlockType.Heading)
            .ToList();
        Assert.Contains(body, b =>
            b.Text.Contains("split across lines", StringComparison.OrdinalIgnoreCase)
            && b.Text.Contains("one paragraph block", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Toc_Joins_Wrapped_Titles_And_Rejects_Catalog_Pages()
    {
        var pages = new List<DocumentPage>
        {
            Page(1, 600, 800, DocumentZoneRole.Unknown,
                Line(1, 0, "CONTENTS", 16, true, 720, 200),
                Line(1, 1, "Ruku or Bowing down David", 11, false, 680, 100),
                Line(1, 2, "and other prophets pbut 3", 11, false, 650, 100))
        };
        for (var i = 2; i <= 10; i++)
            pages.Add(Page(i, 600, 800, DocumentZoneRole.MainContent, Line(i, 0, $"Body page {i} with enough text.", 11, false, 700, 100)));
        pages.Add(Page(11, 600, 800, DocumentZoneRole.Unknown,
            Line(11, 0, "OTHER FREE LITERATURE & PUBLICATIONS", 14, true, 720, 100),
            Line(11, 1, "Booklets", 12, true, 680, 100),
            Line(11, 2, "1. The Quran 24. Salah-Islamic Prayer", 10, false, 640, 100),
            Line(11, 3, "2. Another Booklet Title Here", 10, false, 600, 100)));

        var layout = new LayoutDocument { Pages = pages };
        DocumentZoneClassifier.Classify(layout);
        var toc = TocDetector.Detect(layout);

        Assert.Contains(toc, t => t.Title.Contains("Ruku or Bowing", StringComparison.OrdinalIgnoreCase)
                                  && t.Title.Contains("other prophets", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(toc, t => t.Title.Equals("and other prophets pbut", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(DocumentZoneRole.Appendix, layout.Pages.First(p => p.PageNumber == 11).ZoneRole);
    }

    [Fact]
    public void Prose_Mentioning_Booklet_Is_Not_Catalog_Appendix()
    {
        var pages = new List<DocumentPage>();
        for (var i = 1; i <= 12; i++)
        {
            if (i == 10)
            {
                pages.Add(Page(10, 600, 800, DocumentZoneRole.Unknown,
                    Line(10, 0, "Closing Posture Topic", 16, true, 700, 100),
                    Line(10, 1, "The practice of praying while sitting down is rare but not totally absent in scripture.", 11, false, 650, 100),
                    Line(10, 2, "This booklet has been written with an intention to inspire people into looking at how we communicate.", 11, false, 600, 100)));
            }
            else if (i >= 11)
            {
                pages.Add(Page(i, 600, 800, DocumentZoneRole.Unknown,
                    Line(i, 0, "OTHER ESSENTIAL READINGS", 14, true, 720, 100),
                    Line(i, 1, "1. Sample Promo Title One", 10, false, 680, 100),
                    Line(i, 2, "2. Sample Promo Title Two", 10, false, 640, 100),
                    Line(i, 3, "3. Sample Promo Title Three", 10, false, 600, 100),
                    Line(i, 4, "4. Sample Promo Title Four", 10, false, 560, 100)));
            }
            else
            {
                pages.Add(Page(i, 600, 800, DocumentZoneRole.Unknown,
                    Line(i, 0, $"Teaching body continues on page {i} with enough continuous prose for readers.", 11, false, 700, 100)));
            }
        }

        var layout = new LayoutDocument { Pages = pages };
        DocumentZoneClassifier.Classify(layout);

        Assert.Equal(DocumentZoneRole.MainContent, layout.Pages.First(p => p.PageNumber == 10).ZoneRole);
        Assert.Equal(DocumentZoneRole.Appendix, layout.Pages.First(p => p.PageNumber == 11).ZoneRole);
        Assert.Equal(DocumentZoneRole.Appendix, layout.Pages.First(p => p.PageNumber == 12).ZoneRole);
    }

    [Fact]
    public async Task Late_Body_Heading_Surrounded_By_Reading_Is_Kept_As_Chapter()
    {
        var pages = new List<DocumentPage>
        {
            Page(1, 600, 800, DocumentZoneRole.Unknown,
                Line(1, 0, "CONTENTS", 16, true, 720, 200),
                Line(1, 1, "Early Topic ........ 2", 11, false, 680, 100),
                Line(1, 2, "Closing Posture Topic ........ 3", 11, false, 650, 100)),
            Page(2, 600, 800, DocumentZoneRole.Unknown,
                Line(2, 0, "Early Topic", 16, true, 700, 200),
                Line(2, 1, "Early teaching body with enough continuous prose for a complete reading unit here.", 11, false, 650, 200)),
            Page(3, 600, 800, DocumentZoneRole.Unknown,
                Line(3, 0, "Closing Posture Topic", 16, true, 700, 200),
                Line(3, 1, "The practice of praying while sitting down is rare but not totally absent in scripture texts.", 11, false, 650, 200),
                Line(3, 2, "This booklet has been written with an intention to inspire people into looking at communication.", 11, false, 600, 200)),
            Page(4, 600, 800, DocumentZoneRole.Unknown,
                Line(4, 0, "OTHER FREE LITERATURE & PUBLICATIONS", 14, true, 720, 100),
                Line(4, 1, "Booklets", 12, true, 680, 100),
                Line(4, 2, "1. The Quran Sample", 10, false, 640, 100),
                Line(4, 3, "2. Another Booklet Title", 10, false, 600, 100),
                Line(4, 4, "3. Third Catalog Entry", 10, false, 560, 100),
                Line(4, 5, "4. Fourth Catalog Entry", 10, false, 520, 100))
        };

        var layout = new LayoutDocument { Pages = pages };
        var analyzer = new LayoutAwareStructureAnalyzer();
        var structure = await analyzer.AnalyzeAsync("Sample Book", layout);

        Assert.Contains(structure.Chapters, c => c.Title.Contains("Closing Posture", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(structure.Units.SelectMany(u => u.Blocks), b =>
            b.Text.Contains("OTHER FREE LITERATURE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Toc_Repairs_Split_Page_Digits_From_Ocr()
    {
        var pages = new List<DocumentPage>
        {
            Page(1, 600, 800, DocumentZoneRole.Unknown,
                Line(1, 0, "CONTENTS", 16, true, 720, 200),
                Line(1, 1, "A Loving Relationship ........ 4", 11, false, 680, 100),
                Line(1, 2, "Qiyam or Standing 1 4", 11, false, 650, 100),
                Line(1, 3, "Preparation for Salah ........ 6", 11, false, 620, 100))
        };
        for (var i = 2; i <= 14; i++)
            pages.Add(Page(i, 600, 800, DocumentZoneRole.MainContent,
                Line(i, 0, $"Body page {i} with enough continuous teaching text for pagination.", 11, false, 700, 100)));

        var layout = new LayoutDocument { Pages = pages };
        var toc = TocDetector.Detect(layout);

        Assert.Contains(toc, t =>
            t.Title.Contains("Qiyam or Standing", StringComparison.OrdinalIgnoreCase)
            && !t.Title.EndsWith("1", StringComparison.Ordinal)
            && t.PageNumber == 14);
    }

    [Fact]
    public async Task Front_Matter_Is_Excluded_From_Reading_Units()
    {
        var layout = new LayoutDocument
        {
            Pages =
            [
                Page(1, 600, 800, DocumentZoneRole.Unknown,
                    Line(1, 0, "Sample Book Title", 22, true, 700, 200),
                    Line(1, 1, "Published & Distributed by:", 11, false, 640, 200),
                    Line(1, 2, "askme@example.org | www.example.org", 10, false, 600, 200)),
                Page(2, 600, 800, DocumentZoneRole.Unknown,
                    Line(2, 0, "Opening Teaching", 16, true, 700, 200),
                    Line(2, 1, "This is the first real teaching paragraph with enough body content for a reader unit.", 11, false, 650, 200))
            ]
        };

        var analyzer = new LayoutAwareStructureAnalyzer();
        var structure = await analyzer.AnalyzeAsync("Sample Book Title", layout);

        Assert.DoesNotContain(structure.Units, u => u.Title.Contains("example.org", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(structure.Units, u => u.Title.Contains("Published", StringComparison.OrdinalIgnoreCase));
        Assert.True(structure.ExcludedFrontMatter.Count > 0);
        Assert.Contains(structure.Units, u => u.Blocks.Any(b => b.Text.Contains("first real teaching")));
    }

    private static DocumentPage Page(int number, double w, double h, DocumentZoneRole zone, params DocumentElement[] elements)
        => new()
        {
            PageNumber = number,
            Width = w,
            Height = h,
            ZoneRole = zone,
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
            BoundingBox = new BoundingBox(x, y, Math.Min(400, text.Length * fontSize * 0.45), fontSize + 2),
            ReadingOrder = order,
            Script = ScriptLanguageDetector.DetectScript(text),
            LanguageHint = ScriptLanguageDetector.DetectLanguageHint(text, ScriptLanguageDetector.DetectScript(text)),
            TextDirection = ScriptLanguageDetector.DetectDirection(ScriptLanguageDetector.DetectScript(text)),
            Confidence = 0.9,
            ExtractionMethod = "test",
            IncludeInReading = true
        };
}
