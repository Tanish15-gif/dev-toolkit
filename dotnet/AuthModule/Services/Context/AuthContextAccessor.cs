using System.Security.Claims;
using DevToolkit.Auth.Interfaces.Services;

namespace DevToolkit.Auth.Services.Context;

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

        // ForwardedHeaders middleware handles only explicitly trusted proxies.
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    public string? GetUserAgent()
    {
        var value = _httpContextAccessor.HttpContext?
            .Request
            .Headers
            .UserAgent
            .ToString();
        return value?.Length > 500 ? value[..500] : value;
    }
}
