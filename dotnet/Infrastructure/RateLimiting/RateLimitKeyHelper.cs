using System.Security.Claims;

namespace DevToolkit.Infrastructure.RateLimiting;

/// <summary>
/// Helper methods for building rate-limiter partition keys.
/// </summary>
public static class RateLimitKeyHelper
{
    /// <summary>
    /// Returns "user:{userId}" if the request is authenticated,
    /// otherwise "ip:{clientIp}" using the first X-Forwarded-For address or RemoteIpAddress.
    /// </summary>
    public static string GetUserOrIpKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
            return $"user:{userId}";

        return $"ip:{GetClientIp(context)}";
    }

    /// <summary>Returns "ip:{clientIp}" regardless of authentication status.</summary>
    public static string GetIpKey(HttpContext context)
        => $"ip:{GetClientIp(context)}";

    /// <summary>Returns "{method}:{path}:ip:{clientIp}" for per-endpoint limiting.</summary>
    public static string GetEndpointIpKey(HttpContext context)
        => $"{context.Request.Method}:{context.Request.Path}:ip:{GetClientIp(context)}";

    // Takes only the leftmost IP to prevent X-Forwarded-For spoofing.
    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp.Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
