using Microsoft.Extensions.Diagnostics.HealthChecks;
using PestiTrackBackend.Data;

namespace PestiTrackBackend.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public DatabaseHealthCheck(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database connection succeeded.")
                : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.");
        }
    }
}
