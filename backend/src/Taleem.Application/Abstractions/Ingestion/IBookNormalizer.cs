namespace Taleem.Application.Abstractions.Ingestion;

public interface IBookNormalizer
{
    string NormalizePageText(string rawText);
    NormalizedPage Normalize(ExtractedPageResult page);
}

public sealed record NormalizedPage(int PageNumber, string NormalizedText, double Confidence);
