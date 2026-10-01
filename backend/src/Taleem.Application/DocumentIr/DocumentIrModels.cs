namespace Taleem.Application.DocumentIr;

public enum DocumentElementType
{
    Unknown = 0,
    Text = 1,
    Image = 2,
    Table = 3,
    Line = 4,
    ListItem = 5,
    PageNumber = 6,
    Header = 7,
    Footer = 8,
    Footnote = 9,
    TocEntry = 10
}

/// <summary>Page / region role — physical/document structure, not religious semantics.</summary>
public enum DocumentZoneRole
{
    Unknown = 0,
    Cover = 1,
    TitlePage = 2,
    Copyright = 3,
    PublisherInfo = 4,
    TableOfContents = 5,
    Preface = 6,
    MainContent = 7,
    Appendix = 8,
    Index = 9
}

public enum TextDirectionHint
{
    Unknown = 0,
    Ltr = 1,
    Rtl = 2,
    Mixed = 3
}

public enum ScriptHint
{
    Unknown = 0,
    Latin = 1,
    Arabic = 2,
    Mixed = 3
}

public enum HeadingLevelCandidate
{
    Unknown = 0,
    BookTitle = 1,
    Part = 2,
    Section = 3,
    Chapter = 4,
    Subchapter = 5,
    UnitHeading = 6
}

public sealed record BoundingBox(double X, double Y, double Width, double Height)
{
    public double Top => Y + Height;
    public double Bottom => Y;
    public double CenterY => Y + Height / 2;
    public double CenterX => X + Width / 2;
    public double Right => X + Width;
}

public sealed class DocumentElement
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int PageNumber { get; init; }
    public DocumentElementType ElementType { get; set; } = DocumentElementType.Text;
    public string Text { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public BoundingBox BoundingBox { get; set; } = new(0, 0, 0, 0);
    public string? FontFamily { get; init; }
    public double? FontSize { get; set; }
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public TextDirectionHint TextDirection { get; set; } = TextDirectionHint.Unknown;
    public ScriptHint Script { get; set; } = ScriptHint.Unknown;
    public string? LanguageHint { get; set; }
    public double Confidence { get; set; } = 1.0;
    public int ReadingOrder { get; set; }
    public string ExtractionMethod { get; init; } = "layout";
    public IList<string> Evidence { get; } = new List<string>();

    /// <summary>
    /// When false, text is preserved for provenance/debug but must not become reader TaleemUnits/chunks.
    /// </summary>
    public bool IncludeInReading { get; set; } = true;
}

public sealed class DocumentPage
{
    public int PageNumber { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public bool IsScanned { get; set; }
    public DocumentZoneRole ZoneRole { get; set; } = DocumentZoneRole.Unknown;
    public double ZoneConfidence { get; set; }
    public IList<string> ZoneEvidence { get; } = new List<string>();
    public IList<string> DetectedLanguages { get; } = new List<string>();
    public IList<DocumentElement> Elements { get; init; } = new List<DocumentElement>();
}

public sealed class LayoutDocument
{
    public IList<DocumentPage> Pages { get; init; } = new List<DocumentPage>();
    public int PageCount => Pages.Count;
    public IList<string> DetectedLanguages { get; } = new List<string>();
}

/// <summary>Possible structural heading — NOT a TaleemUnit by itself.</summary>
public sealed class HeadingCandidate
{
    public required DocumentElement Element { get; init; }
    public string Text { get; init; } = string.Empty;
    public HeadingLevelCandidate LevelCandidate { get; set; } = HeadingLevelCandidate.Unknown;
    public double Confidence { get; set; }
    public IList<string> Evidence { get; } = new List<string>();
}

public sealed class TocEntry
{
    public string Title { get; init; } = string.Empty;
    public int? PageNumber { get; init; }
    public int SourcePage { get; init; }
    public double Confidence { get; set; }
    public Guid? SourceElementId { get; init; }
}

public sealed class DocumentZoneSummary
{
    public int PageNumber { get; init; }
    public string ZoneRole { get; init; } = "UNKNOWN";
    public double Confidence { get; init; }
    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
}

public sealed class ExcludedElementSummary
{
    public int PageNumber { get; init; }
    public string Text { get; init; } = string.Empty;
    public string ElementType { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public sealed class IngestionQualityReport
{
    public string ProcessorVersion { get; init; } = string.Empty;
    public int PageCount { get; init; }
    public int SectionCount { get; init; }
    public int ChapterCount { get; init; }
    public int UnitCount { get; init; }
    public int ContentBlockCount { get; init; }
    public int SearchChunkCount { get; init; }
    public int TocEntryCount { get; init; }
    public int ExcludedFrontMatterCount { get; init; }
    public int HeadingCandidateCount { get; init; }
    public int LowConfidenceItemCount { get; init; }
    public int HeadingOnlyUnitCount { get; init; }
    public int EmptyBodyUnitCount { get; init; }
    public double UnitsPerPageAverage { get; init; }
    public double HeadingOnlyUnitRatio { get; init; }
    public double FrontMatterOriginRatio { get; init; }
    public double HeadingConfidenceMin { get; init; }
    public double HeadingConfidenceMax { get; init; }
    public double HeadingConfidenceAverage { get; init; }
    public double StructureConfidenceMin { get; init; }
    public double StructureConfidenceMax { get; init; }
    public double StructureConfidenceAverage { get; init; }
    public bool HasStructuralAnomalies { get; init; }
    public IReadOnlyList<string> AnomalyFlags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<DocumentZoneSummary> DocumentZones { get; init; } = Array.Empty<DocumentZoneSummary>();
    public IReadOnlyList<TocEntry> TocEntries { get; init; } = Array.Empty<TocEntry>();
    public IReadOnlyList<ExcludedElementSummary> ExcludedFrontMatter { get; init; } = Array.Empty<ExcludedElementSummary>();
    public IReadOnlyList<string> SuspiciousUnitTitles { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<int, int> UnitsPerPageDistribution { get; init; } =
        new Dictionary<int, int>();
}
