using DevToolkit.Auth.DTOs;
using DevToolkit.Auth.Entities;

namespace DevToolkit.Auth.Interfaces.Services;

public interface IJwtTokenService
{
    AuthResponseDto GenerateAuthResponse(AppUser user, List<string> roles, string refreshToken, Guid userSessionId);

    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
