using AuthModule.DTOs.Auth;
using AuthModule.Entities;

namespace AuthModule.Interfaces.Services;

public interface ISessionService
{
    Task SignOutOtherSessionsAsync(SignOutOtherSessionsRequestDto dto);
    Task SignOutAllSessionsAsync();
    Task<List<ActiveSessionDto>> GetActiveSessionsAsync();
    Task RevokeSessionAsync(Guid userSessionId);
    Task<UserSession> CreateUserSessionAsync(int appUserId);
}