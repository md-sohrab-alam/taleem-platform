namespace Taleem.Application.Abstractions.Ingestion;

public interface IBookPublisher
{
    Task PublishAsync(Guid bookId, CancellationToken cancellationToken = default);
}
