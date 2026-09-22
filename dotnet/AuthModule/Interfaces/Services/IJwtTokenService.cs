using AuthModule.DTOs.Auth;
using AuthModule.Entities;

namespace AuthModule.Interfaces.Services;

public interface IJwtTokenService
{
    AuthResponseDto GenerateAuthResponse(AppUser user, List<string> roles, string refreshToken, Guid userSessionId);

    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}