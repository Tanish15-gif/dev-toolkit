using DevToolkit.Infrastructure.RateLimiting;
using DevToolkit.Auth.Constants;
using DevToolkit.Infrastructure.Helpers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevToolkit.Infrastructure.Extensions;

public static class BackendHealthCheckEndpointExtensions
{
    /// <summary>Maps public liveness/readiness and admin-only detailed diagnostics.</summary>
    public static IEndpointRouteBuilder MapDevToolkitHealthChecks
    (
        this IEndpointRouteBuilder endpoints,
        string basePath = "/health"
    )
    {
        if (string.IsNullOrWhiteSpace(basePath) || !basePath.StartsWith('/') || basePath == "/" || basePath.Contains('?') || basePath.Contains('#'))
        {
            throw new ArgumentException("Use an absolute health endpoint path without a query or fragment.", nameof(basePath));
        }

        var prefix = basePath.TrimEnd('/');

        endpoints.MapHealthChecks(prefix, new HealthCheckOptions
        {
            Predicate = _ => false,
            AllowCachingResponses = false
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.PublicRead);

        endpoints.MapHealthChecks(prefix + "/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready") && !check.Tags.Contains("external"),
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.PublicRead);

        endpoints.MapHealthChecks(prefix + "/details", new HealthCheckOptions
        {
            AllowCachingResponses = false,
            ResponseWriter = HealthCheckResponseWriter.WriteJsonResponse
        })
        .RequireAuthorization(AppPolicies.AdminOnly)
        .RequireRateLimiting(RateLimitPolicies.PublicRead);

        return endpoints;
    }
}
