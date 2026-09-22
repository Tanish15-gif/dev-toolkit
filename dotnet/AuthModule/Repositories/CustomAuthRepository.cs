using TanishElectronics.Api.Entities;
using TanishElectronics.Api.Interfaces.Repositories;
using TanishElectronics.Api.Interfaces.Repositories.Auth;

namespace TanishElectronics.Api.Repositories;

public class CustomAuthRepository : ICustomAuthRepository
{
    private readonly ICredentialAuthRepository _credentialRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IGoogleAuthRepository _googleRepository;
    private readonly IPasswordRecoveryRepository _passwordRepository;
    private readonly ISessionRepository _sessionRepository;

    public CustomAuthRepository
    (
        ICredentialAuthRepository credentialAuthRepository,
        ITokenRepository tokenRepository,
        IGoogleAuthRepository googleRepository,
        IPasswordRecoveryRepository passwordRepository,
        ISessionRepository sessionRepository
    )
    {
        _credentialRepository = credentialAuthRepository;
        _tokenRepository = tokenRepository;
        _googleRepository = googleRepository;
        _passwordRepository = passwordRepository;
        _sessionRepository = sessionRepository;
    }

    // ---- Credential / User ----

    public Task<AppUser?> GetUserByNormalizedEmailAsync(string normalizedEmail)
        => _credentialRepository.GetUserByNormalizedEmailAsync(normalizedEmail);

    public Task<bool> EmailExistsAsync(string normalizedEmail)
        => _credentialRepository.EmailExistsAsync(normalizedEmail);

    public Task<AppUser> CreateUserAsync(AppUser user)
        => _credentialRepository.CreateUserAsync(user);

    public Task UpdateUserAsync(AppUser user)
        => _credentialRepository.UpdateUserAsync(user);

    public Task AssignRoleAsync(int appUserId, string roleName)
        => _credentialRepository.AssignRoleAsync(appUserId, roleName);

    public Task<List<string>> GetUserRolesAsync(int appUserId)
        => _credentialRepository.GetUserRolesAsync(appUserId);

    public Task CreateCustomerProfileIfMissingAsync(AppUser appUser)
        => _credentialRepository.CreateCustomerProfileIfMissingAsync(appUser);

    public Task AddAuthEventAsync(UserAuthEvent authEvent)
        => _credentialRepository.AddAuthEventAsync(authEvent);

    public Task<AppUser?> GetUserByIdWithExternalLoginsAsync(int appUserId)
        => _credentialRepository.GetUserByIdWithExternalLoginsAsync(appUserId);

    // ---- Token ----

    public Task AddRefreshTokenAsync(RefreshToken refreshToken)
        => _tokenRepository.AddRefreshTokenAsync(refreshToken);

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
        => _tokenRepository.GetRefreshTokenByHashAsync(tokenHash);

    public Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string? revokedByIp, string replacedByTokenHash = null!)
        => _tokenRepository.RevokeRefreshTokenAsync(refreshToken, revokedByIp, replacedByTokenHash);

    // ---- Google ----

    public Task<ExternalLogin?> GetExternalLoginAsync(string provider, string providerUserId)
        => _googleRepository.GetExternalLoginAsync(provider, providerUserId);

    public Task AddExternalLoginAsync(ExternalLogin externalLogin)
        => _googleRepository.AddExternalLoginAsync(externalLogin);

    public Task CreatePasswordSetupSessionAsync(PasswordResetSession setupSession)
        => _googleRepository.CreatePasswordSetupSessionAsync(setupSession);

    public Task CompletePasswordSetupAsync(AppUser user, PasswordResetSession setupSession)
        => _googleRepository.CompletePasswordSetupAsync(user, setupSession);

    // ---- Password Recovery ----

    public Task<OtpCode?> GetLatestOtpAsync(string normalizedEmail, string purpose)
        => _passwordRepository.GetLatestOtpAsync(normalizedEmail, purpose);

    public Task AddOtpAsync(OtpCode otpCode)
        => _passwordRepository.AddOtpAsync(otpCode);

    public Task InvalidateActiveOtpsAsync(string normalizedEmail, string purpose)
        => _passwordRepository.InvalidateActiveOtpsAsync(normalizedEmail, purpose);

    public Task<OtpCode?> GetLatestActiveOtpAsync(string normalizedEmail, string purpose)
        => _passwordRepository.GetLatestActiveOtpAsync(normalizedEmail, purpose);

    public Task UpdateOtpAsync(OtpCode otpCode)
        => _passwordRepository.UpdateOtpAsync(otpCode);

    public Task ConsumeOtpAndCreateResetSessionAsync(OtpCode otpCode, PasswordResetSession resetSession)
        => _passwordRepository.ConsumeOtpAndCreateResetSessionAsync(otpCode, resetSession);

    public Task<PasswordResetSession?> GetPasswordResetSessionByHashAsync(string tokenHash)
        => _passwordRepository.GetPasswordResetSessionByHashAsync(tokenHash);

    public Task CompletePasswordResetAsync(AppUser user, PasswordResetSession resetSession, string? revokedByIp)
        => _passwordRepository.CompletePasswordResetAsync(user, resetSession, revokedByIp);

    public Task CompletePasswordChangeAsync(AppUser user, string? revokedByIp)
        => _passwordRepository.CompletePasswordChangeAsync(user, revokedByIp);

    // ---- Session ----

    public Task<int> GetActiveSessionCountAsync(int appUserId)
        => _sessionRepository.GetActiveSessionCountAsync(appUserId);

    public Task RevokeOtherSessionsAsync(int appUserId, Guid currentSessionId, string? revokedByIp)
        => _sessionRepository.RevokeOtherSessionsAsync(appUserId, currentSessionId, revokedByIp);

    public Task RevokeAllRefreshTokensAsync(int appUserId, string? revokedByIp)
        => _sessionRepository.RevokeAllRefreshTokensAsync(appUserId, revokedByIp);

    public Task<UserSession> AddUserSessionAsync(UserSession userSession)
        => _sessionRepository.AddUserSessionAsync(userSession);

    public Task TouchUserSessionAsync(Guid userSessionId, string? ipAddress)
        => _sessionRepository.TouchUserSessionAsync(userSessionId, ipAddress);

    public Task<int> GetActiveUserSessionCountAsync(int appUserId)
        => _sessionRepository.GetActiveUserSessionCountAsync(appUserId);

    public Task<List<UserSession>> GetActiveUserSessionsAsync(int appUserId)
        => _sessionRepository.GetActiveUserSessionsAsync(appUserId);

    public Task<bool> RevokeUserSessionAsync(int appUserId, Guid userSessionId, string? revokedByIp)
        => _sessionRepository.RevokeUserSessionAsync(appUserId, userSessionId, revokedByIp);
}