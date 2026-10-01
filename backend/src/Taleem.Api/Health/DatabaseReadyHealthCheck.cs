using Microsoft.Extensions.Diagnostics.HealthChecks;
using Taleem.Infrastructure.Persistence;

namespace Taleem.Api.Health;

public sealed class DatabaseReadyHealthCheck : IHealthCheck
{
    private readonly TaleemDbContext _db;

    public DatabaseReadyHealthCheck(TaleemDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database reachable")
                : HealthCheckResult.Unhealthy("Database unreachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}
