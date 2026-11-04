using DevToolkit.Auth.DTOs;

namespace DevToolkit.Auth.Interfaces.Services;

public interface ICustomAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);

    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);

    Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto dto);

    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto);

    Task LogoutAsync(LogoutRequestDto dto);

    Task RequestForgotPasswordOtpAsync(ForgotPasswordRequestDto dto);

    Task<VerifyForgotPasswordOtpResponseDto> VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequestDto dto);

    Task ResetPasswordAsync(ResetPasswordRequestDto dto);

    Task<SecuritySummaryResponseDto> GetSecuritySummaryAsync();

    Task ChangePasswordAsync(ChangePasswordRequestDto dto);

    Task<GooglePasswordReauthResponseDto> ReauthenticateGoogleForPasswordAsync(GooglePasswordReauthRequestDto dto);

    Task CompleteSetPasswordAsync(CompleteSetPasswordRequestDto dto);

    Task SignOutOtherSessionsAsync(SignOutOtherSessionsRequestDto dto);

    Task SignOutAllSessionsAsync();

    Task<List<ActiveSessionDto>> GetActiveSessionsAsync();

    Task RevokeSessionAsync(Guid userSessionId);
}
