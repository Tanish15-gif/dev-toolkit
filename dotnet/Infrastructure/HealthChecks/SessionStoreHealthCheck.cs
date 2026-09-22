using AuthModule.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevToolkit.Infrastructure.HealthChecks;

/// <summary>
/// Checks that the UserSessions table is reachable and queryable.
/// Register: .AddCheck&lt;SessionStoreHealthCheck&gt;("session-store", tags: new[] { "ready", "database" })
/// </summary>
public class SessionStoreHealthCheck : IHealthCheck
{
    private readonly IAuthDbContext _db;

    public SessionStoreHealthCheck(IAuthDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Lightweight existence probe — no actual data read needed
            var canConnect = await _db.UserSessions
                .AsNoTracking()
                .AnyAsync(cancellationToken);

            return HealthCheckResult.Healthy("Session store is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Session store health check failed.",
                exception: ex
            );
        }
    }
}
