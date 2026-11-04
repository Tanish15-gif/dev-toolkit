using DevToolkit.Auth.DTOs;
using DevToolkit.Auth.Interfaces.Services;

namespace DevToolkit.Auth.Services;

public class CustomAuthService : ICustomAuthService
{
    private readonly ICredentialAuthService _credentialAuth;
    private readonly ITokenService _tokenService;
    private readonly IGoogleAuthService _googleAuth;
    private readonly IPasswordRecoveryService _passwordRecovery;
    private readonly ISessionService _sessionService;

    public CustomAuthService(
        ICredentialAuthService credentialAuth,
        ITokenService tokenService,
        IGoogleAuthService googleAuth,
        IPasswordRecoveryService passwordRecovery,
        ISessionService sessionService)
    {
        _credentialAuth = credentialAuth;
        _tokenService = tokenService;
        _googleAuth = googleAuth;
        _passwordRecovery = passwordRecovery;
        _sessionService = sessionService;
    }

    // ---- Credential (Register/Login) ----

    public Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
        => _credentialAuth.RegisterAsync(dto);

    public Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        => _credentialAuth.LoginAsync(dto);

    // ---- Token ----

    public Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto dto)
        => _tokenService.RefreshAsync(dto);

    public Task LogoutAsync(LogoutRequestDto dto)
        => _tokenService.LogoutAsync(dto);

    // ---- Google ----

    public Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto)
        => _googleAuth.GoogleLoginAsync(dto);

    public Task<GooglePasswordReauthResponseDto> ReauthenticateGoogleForPasswordAsync(GooglePasswordReauthRequestDto dto)
        => _googleAuth.ReauthenticateGoogleForPasswordAsync(dto);

    // ---- Password Recovery ----

    public Task RequestForgotPasswordOtpAsync(ForgotPasswordRequestDto dto)
        => _passwordRecovery.RequestForgotPasswordOtpAsync(dto);

    public Task<VerifyForgotPasswordOtpResponseDto> VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequestDto dto)
        => _passwordRecovery.VerifyForgotPasswordOtpAsync(dto);

    public Task ResetPasswordAsync(ResetPasswordRequestDto dto)
        => _passwordRecovery.ResetPasswordAsync(dto);

    public Task ChangePasswordAsync(ChangePasswordRequestDto dto)
        => _passwordRecovery.ChangePasswordAsync(dto);

    public Task CompleteSetPasswordAsync(CompleteSetPasswordRequestDto dto)
        => _passwordRecovery.CompleteSetPasswordAsync(dto);

    public Task<SecuritySummaryResponseDto> GetSecuritySummaryAsync()
        => _passwordRecovery.GetSecuritySummaryAsync();

    // ---- Session ----

    public Task SignOutOtherSessionsAsync(SignOutOtherSessionsRequestDto dto)
        => _sessionService.SignOutOtherSessionsAsync(dto);

    public Task SignOutAllSessionsAsync()
        => _sessionService.SignOutAllSessionsAsync();

    public Task<List<ActiveSessionDto>> GetActiveSessionsAsync()
        => _sessionService.GetActiveSessionsAsync();

    public Task RevokeSessionAsync(Guid userSessionId)
        => _sessionService.RevokeSessionAsync(userSessionId);
}
