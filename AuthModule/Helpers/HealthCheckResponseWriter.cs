using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevToolkit.Auth.Helpers;

/// <summary>Detailed output is intended for the authenticated admin health endpoint.</summary>
public static class HealthCheckResponseWriter
{
    public static async Task WriteJsonResponse
    (
        HttpContext httpContext,
        HealthReport report
    )
    {
        httpContext.Response.Headers.CacheControl = "no-store";

        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                data = entry.Value.Data
            })
        }, httpContext.RequestAborted);
    }
}
