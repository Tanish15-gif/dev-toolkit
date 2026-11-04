using DevToolkit.Auth.DTOs.Auth;

namespace DevToolkit.Auth.Interfaces.Services;

public interface ICredentialAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}
