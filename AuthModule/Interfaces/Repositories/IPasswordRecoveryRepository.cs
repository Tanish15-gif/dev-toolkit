using DevToolkit.Auth.Entities;

namespace DevToolkit.Auth.Interfaces.Repositories;

public interface IPasswordRecoveryRepository
{
    Task<OtpCode?> GetLatestOtpAsync(string normalizedEmail, string purpose);
    Task AddOtpAsync(OtpCode otpCode);
    Task InvalidateActiveOtpsAsync(string normalizedEmail, string purpose);
    Task<OtpCode?> GetLatestActiveOtpAsync(string normalizedEmail, string purpose);
    Task UpdateOtpAsync(OtpCode otpCode);
    Task RecordFailedOtpAttemptAsync(int otpCodeId, int maxAttempts);
    Task ConsumeOtpAndCreateResetSessionAsync(OtpCode otpCode, PasswordResetSession resetSession, int maxAttempts);
    Task<PasswordResetSession?> GetPasswordResetSessionByHashAsync(string tokenHash);
    Task CompletePasswordResetAsync(AppUser user, PasswordResetSession resetSession, string? revokedByIp);
    Task CompletePasswordChangeAsync(AppUser user, string? revokedByIp);
}
