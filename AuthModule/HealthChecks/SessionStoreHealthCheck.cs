using System.Diagnostics;
using DevToolkit.Auth.Interfaces;
using DevToolkit.Auth.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DevToolkit.Auth.HealthChecks;

public class SessionStoreHealthCheck : IHealthCheck
{
    private readonly IAuthDbContext _database;
    private readonly AuthModuleSettings _settings;

    public SessionStoreHealthCheck
    (
        IAuthDbContext database,
        IOptions<AuthModuleSettings> options
    )
    {
        _database = database;
        _settings = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync
    (
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _database.UserSessions.AsNoTracking().AnyAsync(cancellationToken);
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["latencyMs"] = stopwatch.ElapsedMilliseconds
            };

            return stopwatch.ElapsedMilliseconds > _settings.SessionStoreSlowMilliseconds
                ? HealthCheckResult.Degraded("Session store is readable but slow.", data: data)
                : HealthCheckResult.Healthy("Session store is reachable and readable.", data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Session store is unavailable.", exception);
        }
    }
}
