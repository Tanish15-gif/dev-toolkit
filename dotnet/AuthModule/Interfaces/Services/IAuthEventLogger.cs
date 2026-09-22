using AuthModule.Enums.Auth;

namespace AuthModule.Interfaces.Services;

public interface IAuthEventLogger
{
    Task LogAsync(int? userId, string? email, AuthEventType type, bool success, string? description);
}