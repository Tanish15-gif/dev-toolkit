using AuthModule.DTOs.Auth;

namespace AuthModule.Interfaces.Services;

public interface ITokenService
{
    Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto dto);
    Task LogoutAsync(LogoutRequestDto dto);
    Task SaveRefreshTokenAsync(int appUserId, Guid userSessionId, string refreshToken);
}