using DevToolkit.Auth.Enums;

namespace DevToolkit.Auth.Interfaces.Services;

public interface IAuthEventLogger
{
    Task LogAsync(int? userId, string? email, AuthEventType type, bool success, string? description);
}
