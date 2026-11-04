namespace DevToolkit.Auth.Interfaces.Services;

public interface IAuthContextAccessor
{
    string? GetClientIpAddress();
    string? GetUserAgent();
    int GetAuthenticatedUserId();
    Guid GetAuthenticatedSessionId();
}
