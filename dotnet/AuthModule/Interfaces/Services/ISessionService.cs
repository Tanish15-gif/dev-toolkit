using DevToolkit.Auth.DTOs.Auth;
using DevToolkit.Auth.Entities;

namespace DevToolkit.Auth.Interfaces.Services;

public interface ISessionService
{
    Task SignOutOtherSessionsAsync(SignOutOtherSessionsRequestDto dto);
    Task SignOutAllSessionsAsync();
    Task<List<ActiveSessionDto>> GetActiveSessionsAsync();
    Task RevokeSessionAsync(Guid userSessionId);
    Task<UserSession> CreateUserSessionAsync(int appUserId);
}
