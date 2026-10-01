namespace Taleem.Application.Abstractions.Ingestion;

public interface IIngestionPipeline
{
    Task ProcessAsync(Guid ingestionJobId, CancellationToken cancellationToken = default);
}
