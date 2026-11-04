using DevToolkit.Auth.DTOs;

namespace DevToolkit.Auth.Interfaces.Services;

public interface IGoogleAuthService
{
    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto);
    Task<GooglePasswordReauthResponseDto> ReauthenticateGoogleForPasswordAsync(GooglePasswordReauthRequestDto dto);
}
