namespace Taleem.Application.Abstractions.Persistence;

/// <summary>
/// Marker for unit-of-work style save. Concrete DbContext lives in Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
