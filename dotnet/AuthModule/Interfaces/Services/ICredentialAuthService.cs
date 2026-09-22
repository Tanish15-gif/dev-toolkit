using AuthModule.DTOs.Auth;

namespace AuthModule.Interfaces.Services;

public interface ICredentialAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}