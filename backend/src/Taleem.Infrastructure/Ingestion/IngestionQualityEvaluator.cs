using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

public static class IngestionQualityEvaluator
{
    public const string ProcessorVersion = "2.2.0";

    public static IngestionQualityReport Build(
        LayoutDocument layout,
        AnalyzedBookStructure structure,
        IReadOnlyList<ExcludedElementSummary> excluded,
        int headingCandidateCount,
        int searchChunkCount,
        IReadOnlyList<HeadingCandidate>? headings = null)
    {
        var units = structure.Units;
        var pageCount = Math.Max(1, layout.PageCount);
        var blockCount = units.Sum(u => u.Blocks.Count);

        var headingOnly = units.Count(u =>
            u.Blocks.Count == 1 && u.Blocks[0].BlockType == Domain.Enums.BlockType.Heading);
        var emptyBody = units.Count(u =>
            u.Blocks.All(b => b.BlockType == Domain.Enums.BlockType.Heading)
            || u.Blocks.Sum(b => b.Text.Length) < 40);

        var unitsPerPage = new Dictionary<int, int>();
        foreach (var u in units)
            unitsPerPage[u.SourcePageStart] = unitsPerPage.GetValueOrDefault(u.SourcePageStart) + 1;

        var lowConfidenceBlocks = units.SelectMany(u => u.Blocks)
            .Count(b => b.VerificationStatus == Domain.Enums.VerificationStatus.FlaggedLowConfidence);
        var lowConfidenceUnits = units.Count(u => u.NeedsReview || u.StructureConfidence < 0.55);
        var lowConfidence = lowConfidenceBlocks + lowConfidenceUnits;

        var headingScores = (headings ?? Array.Empty<HeadingCandidate>()).Select(h => h.Confidence).ToList();
        var structureScores = units.Select(u => u.StructureConfidence).ToList();

        var frontMatterPages = layout.Pages
            .Where(p => !DocumentZoneClassifier.IsReadingZone(p.ZoneRole))
            .Select(p => p.PageNumber)
            .ToHashSet();

        var frontOrigin = units.Count(u =>
            frontMatterPages.Contains(u.SourcePageStart)
            && frontMatterPages.Contains(u.SourcePageEnd));

        var anomalyFlags = new List<string>();
        var unitsPerPageAvg = units.Count / (double)pageCount;
        var headingOnlyRatio = units.Count == 0 ? 0 : headingOnly / (double)units.Count;
        var frontRatio = units.Count == 0 ? 0 : frontOrigin / (double)units.Count;

        if (unitsPerPageAvg > 4.0 && headingOnlyRatio > 0.35)
            anomalyFlags.Add("high-unit-density-with-heading-only-units");
        if (headingOnlyRatio > 0.5)
            anomalyFlags.Add("majority-heading-only-units");
        if (frontRatio > 0.15)
            anomalyFlags.Add("units-from-front-matter-zones");
        if (structure.Chapters.Count > pageCount * 2.5 && unitsPerPageAvg > 3)
            anomalyFlags.Add("chapter-density-anomaly");
        if (structureScores.Count > 0 && structureScores.Average() < 0.55)
            anomalyFlags.Add("low-average-structure-confidence");
        if (units.Count(u => u.NeedsReview) > Math.Max(1, units.Count / 3))
            anomalyFlags.Add("many-units-need-review");
        if (unitsPerPage.Values.DefaultIfEmpty(0).Max() >= 8)
            anomalyFlags.Add("single-page-unit-spike");

        var suspicious = units
            .Where(u => u.NeedsReview || u.Blocks.Count <= 1 || u.Title.Length > 90 || u.StructureConfidence < 0.55)
            .Select(u => u.Title)
            .Distinct()
            .Take(25)
            .ToList();

        return new IngestionQualityReport
        {
            ProcessorVersion = ProcessorVersion,
            PageCount = layout.PageCount,
            SectionCount = structure.Sections.Count,
            ChapterCount = structure.Chapters.Count,
            UnitCount = units.Count,
            ContentBlockCount = blockCount,
            SearchChunkCount = searchChunkCount,
            TocEntryCount = structure.TocEntries.Count,
            ExcludedFrontMatterCount = excluded.Count,
            HeadingCandidateCount = headingCandidateCount,
            LowConfidenceItemCount = lowConfidence,
            HeadingOnlyUnitCount = headingOnly,
            EmptyBodyUnitCount = emptyBody,
            UnitsPerPageAverage = Math.Round(unitsPerPageAvg, 2),
            HeadingOnlyUnitRatio = Math.Round(headingOnlyRatio, 3),
            FrontMatterOriginRatio = Math.Round(frontRatio, 3),
            HeadingConfidenceMin = headingScores.Count == 0 ? 0 : Math.Round(headingScores.Min(), 3),
            HeadingConfidenceMax = headingScores.Count == 0 ? 0 : Math.Round(headingScores.Max(), 3),
            HeadingConfidenceAverage = headingScores.Count == 0 ? 0 : Math.Round(headingScores.Average(), 3),
            StructureConfidenceMin = structureScores.Count == 0 ? 0 : Math.Round(structureScores.Min(), 3),
            StructureConfidenceMax = structureScores.Count == 0 ? 0 : Math.Round(structureScores.Max(), 3),
            StructureConfidenceAverage = structureScores.Count == 0 ? 0 : Math.Round(structureScores.Average(), 3),
            HasStructuralAnomalies = anomalyFlags.Count > 0,
            AnomalyFlags = anomalyFlags,
            DocumentZones = layout.Pages.Select(p => new DocumentZoneSummary
            {
                PageNumber = p.PageNumber,
                ZoneRole = p.ZoneRole.ToString().ToUpperInvariant(),
                Confidence = p.ZoneConfidence,
                Evidence = p.ZoneEvidence.ToList()
            }).ToList(),
            TocEntries = structure.TocEntries.ToList(),
            ExcludedFrontMatter = excluded.Take(200).ToList(),
            SuspiciousUnitTitles = suspicious,
            UnitsPerPageDistribution = unitsPerPage
        };
    }
}
