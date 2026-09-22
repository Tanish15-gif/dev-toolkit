using System.Security.Claims;
using AuthModule.Interfaces.Services;

namespace AuthModule.Services.Context;

public class AuthContextAccessor : IAuthContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthContextAccessor
    (
        IHttpContextAccessor httpContextAccessor
    )
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetAuthenticatedSessionId()
    {
        var sessionIdValue =
            _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue("sid");

        if (!Guid.TryParse(sessionIdValue, out var sessionId))
        {
            throw new UnauthorizedAccessException(
                "Current session was not found."
            );
        }

        return sessionId;
    }

    public int GetAuthenticatedUserId()
    {
        var userIdValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var appUserId)) throw new UnauthorizedAccessException("Authenticated user was not found.");

        return appUserId;
    }

    public string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return null;
        }

        var forwardedFor =
            httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(",")[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    public string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?
            .Request
            .Headers
            .UserAgent
            .ToString();
    }
}