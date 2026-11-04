using System.Security.Claims;

namespace DevToolkit.Auth.Helpers;

public static class RateLimitKeyHelper
{
    public static string GetUserOrIpKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");

        return string.IsNullOrWhiteSpace(userId)
            ? GetIpKey(context)
            : $"user:{userId}";
    }

    public static string GetIpKey(HttpContext context)
    {
        // Configure trusted proxies in the host before running the rate limiter.
        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    public static string GetEndpointIpKey(HttpContext context)
    {
        return $"{context.Request.Method}:{context.Request.Path}:{GetIpKey(context)}";
    }
}
