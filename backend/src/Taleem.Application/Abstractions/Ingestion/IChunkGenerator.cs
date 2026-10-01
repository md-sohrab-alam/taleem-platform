namespace Taleem.Application.Abstractions.Ingestion;

public interface IChunkGenerator
{
    IReadOnlyList<GeneratedChunk> Generate(
        AnalyzedUnit unit,
        Guid bookId,
        Guid chapterId,
        Guid taleemUnitId,
        string processorVersion);
}

public sealed record GeneratedChunk(
    string ChunkText,
    string Language,
    int ChunkIndex,
    int PageStart,
    int PageEnd,
    IReadOnlyList<int> BlockOrderIndexes,
    IReadOnlyList<Guid> ContentBlockIds,
    string MetadataJson);
