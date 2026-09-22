using AuthModule.DTOs.Auth;

namespace AuthModule.Interfaces.Services;

public interface IGoogleAuthService
{
    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto);
    Task<GooglePasswordReauthResponseDto> ReauthenticateGoogleForPasswordAsync(GooglePasswordReauthRequestDto dto);
}