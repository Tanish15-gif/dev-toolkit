using AuthModule.DTOs.Auth;
using AuthModule.Entities;
using AuthModule.Enums.Auth;
using AuthModule.Exceptions;
using AuthModule.Interfaces.Repositories;
using AuthModule.Interfaces.Services;
using AuthModule.Services.Helpers;
using AuthModule.Settings;
using Microsoft.Extensions.Options;

namespace AuthModule.Services;

public class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly ICustomAuthRepository _authRepository;
    private readonly IAuthContextAccessor _authContextAccessor;
    private readonly IPasswordHasherService _hasherService;
    private readonly IOtpSecurityService _otpSecurityService;
    private readonly OtpSettings _otpSettings;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetTokenService _passwordResetTokenService;
    private readonly AppInfoSettings _appInfo;

    public PasswordRecoveryService
    (
        ICustomAuthRepository authRepository,
        IAuthContextAccessor authContextAccessor,
        IPasswordHasherService hasherService,
        IOtpSecurityService otpSecurityService,
        IOptions<OtpSettings> options,
        IEmailService emailService,
        IPasswordResetTokenService passwordResetTokenService,
        IOptions<AppInfoSettings> appInfoOptions
    )
    {
        _authRepository = authRepository;
        _authContextAccessor = authContextAccessor;
        _hasherService = hasherService;
        _otpSecurityService = otpSecurityService;
        _otpSettings = options.Value;
        _emailService = emailService;
        _passwordResetTokenService = passwordResetTokenService;
        _appInfo = appInfoOptions.Value;
    }

    public async Task RequestForgotPasswordOtpAsync(ForgotPasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new InvalidOperationException("Email is required");

        var normalizedEmail = AuthHelpers.NormalizeEmail(dto.Email);

        var user = await _authRepository.GetUserByNormalizedEmailAsync(normalizedEmail);

        if (user == null || user.DeletedAt != null) throw new KeyNotFoundException("No account is registered with this email.");
        if (!user.IsActive) throw new InvalidOperationException("This account is currently inactive");
        if (string.IsNullOrWhiteSpace(user.PasswordHash)) throw new InvalidOperationException("This account uses Google sign-in. Please sign in with Google.");

        var purpose = OtpPurpose.ForgotPassword.ToString();

        var latestOtp = await _authRepository.GetLatestOtpAsync(normalizedEmail, purpose);

        var resendAllowedAt = latestOtp?.CreatedAt.AddSeconds(_otpSettings.ResendCooldownSeconds);

        if (resendAllowedAt.HasValue && resendAllowedAt.Value > DateTime.UtcNow) return;

        await _authRepository.InvalidateActiveOtpsAsync(normalizedEmail, purpose);

        var plainCode = _otpSecurityService.GenerateCode();

        var codeHash = _otpSecurityService.HashCode(normalizedEmail, purpose, plainCode);

        var otpEntity = new OtpCode
        {
            AppUserId = user.AppUserId,
            Email = user.Email,
            NormalizedEmail = normalizedEmail,
            Purpose = purpose,
            CodeHash = codeHash,

            ExpiresAt = DateTime.UtcNow.AddMinutes(_otpSettings.ExpiryMinutes),

            ConsumedAt = null,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = _authContextAccessor.GetClientIpAddress()
        };

        await _authRepository.AddOtpAsync(otpEntity);

        try
        {
            var safeName = System.Net.WebUtility.HtmlEncode(user.FullName);
            var appName = _appInfo.Name;

            var htmlBody = $"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0;background:#f7f5ef;font-family:Arial,sans-serif;color:#1f1a12;">
                <div style="max-width:560px;margin:40px auto;padding:24px;">
                    <div style="background:#fffdf7;border:1px solid #d8cdb6;border-radius:24px;padding:32px;">
                        <p style="margin:0;color:#8a6624;font-size:12px;font-weight:700;letter-spacing:2px;text-transform:uppercase;">
                            {appName}
                        </p>

                        <h1 style="margin:20px 0 10px;font-size:28px;">
                            Reset your password
                        </h1>

                        <p style="line-height:1.7;color:#6f6758;">
                            Hello {safeName}, use the verification code below to continue resetting your password.
                        </p>

                        <div style="margin:28px 0;padding:20px;text-align:center;background:#f2eadb;border-radius:16px;font-size:34px;font-weight:800;letter-spacing:10px;color:#1f1a12;">
                            {plainCode}
                        </div>

                        <p style="line-height:1.7;color:#6f6758;">
                            This code expires in {_otpSettings.ExpiryMinutes} minutes.
                        </p>

                        <p style="margin-top:24px;font-size:13px;line-height:1.6;color:#817867;">
                            If you did not request a password reset, you can safely ignore this email.
                        </p>
                    </div>
                </div>
            </body>
            </html>
            """;

            var textBody = $"""
                {appName} password reset

                Hello {user.FullName},

                Your password reset code is: {plainCode}

                This code expires in {_otpSettings.ExpiryMinutes} minutes.

                If you did not request this, ignore this email.
            """;

            await _emailService.SendAsync(
                user.Email,
                $"Your {appName} password reset code",
                htmlBody,
                textBody)
            ;
        }
        catch
        {
            await _authRepository.InvalidateActiveOtpsAsync(normalizedEmail, purpose);
            throw;
        }
    }

    public async Task<VerifyForgotPasswordOtpResponseDto> VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new InvalidOperationException("Email is required.");

        var code = dto.Code?.Trim() ?? string.Empty;

        if (code.Length != 6 || !code.All(char.IsDigit))
        {
            throw new InvalidOperationException("Invalid or expired verification code.");
        }

        var normalizedEmail = AuthHelpers.NormalizeEmail(dto.Email);

        var user =
            await _authRepository
                .GetUserByNormalizedEmailAsync(
                    normalizedEmail
                );

        if
        (
            user == null ||
            !user.IsActive ||
            user.DeletedAt != null
        )
        {
            throw new InvalidOperationException(
                "Invalid or expired verification code."
            );
        }

        var purpose = OtpPurpose.ForgotPassword.ToString();

        var otp =
            await _authRepository.GetLatestActiveOtpAsync(
                normalizedEmail,
                purpose
            );

        if (otp == null)
        {
            throw new InvalidOperationException(
                "Invalid or expired verification code."
            );
        }

        var now = DateTime.UtcNow;

        if (
            otp.ExpiresAt <= now ||
            otp.AttemptCount >= _otpSettings.MaxAttempts
        )
        {
            otp.ConsumedAt = now;

            await _authRepository.UpdateOtpAsync(otp);

            throw new InvalidOperationException(
                "Invalid or expired verification code."
            );
        }

        var valid =
            _otpSecurityService.VerifyCode(
                normalizedEmail,
                purpose,
                code,
                otp.CodeHash
            );

        if (!valid)
        {
            otp.AttemptCount += 1;

            if
            (
                otp.AttemptCount >=
                _otpSettings.MaxAttempts
            )
            {
                otp.ConsumedAt = now;
            }

            await _authRepository.UpdateOtpAsync(otp);

            throw new InvalidOperationException("Invalid or expired verification code.");
        }

        var plainResetToken = _passwordResetTokenService.GenerateToken();

        var resetTokenHash = _passwordResetTokenService.HashToken(plainResetToken);

        var resetSession = new PasswordResetSession
        {
            AppUserId = user.AppUserId,
            TokenHash = resetTokenHash,

            ExpiresAt = now.AddMinutes(_otpSettings.ResetTokenExpiryMinutes),

            ConsumedAt = null,
            CreatedAt = now,
            CreatedByIp = _authContextAccessor.GetClientIpAddress()
        };

        await _authRepository.ConsumeOtpAndCreateResetSessionAsync(otp, resetSession);

        return new VerifyForgotPasswordOtpResponseDto
        {
            ResetToken = plainResetToken,

            ExpiresInSeconds = _otpSettings.ResetTokenExpiryMinutes * 60
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ResetToken)) throw new InvalidOperationException("Invalid or expired reset session.");

        if (string.IsNullOrWhiteSpace(dto.NewPassword)) throw new InvalidOperationException("New password is required.");

        if (dto.NewPassword.Length < 8) throw new InvalidOperationException("Password must be at least 8 characters.");

        var tokenHash = _passwordResetTokenService.HashToken(dto.ResetToken);

        var resetSession =
            await _authRepository
                .GetPasswordResetSessionByHashAsync(
                    tokenHash
                );

        if (
            resetSession == null ||
            resetSession.ConsumedAt != null ||
            resetSession.ExpiresAt <= DateTime.UtcNow
        )
        {
            throw new InvalidOperationException(
                "Invalid or expired reset session."
            );
        }

        var user = resetSession.AppUser;

        if (
            user == null ||
            !user.IsActive ||
            user.DeletedAt != null
        )
        {
            throw new InvalidOperationException(
                "Invalid or expired reset session."
            );
        }

        user.PasswordHash =
            _hasherService.HashPassword(
                dto.NewPassword
            );

        user.PasswordUpdatedAt = DateTime.UtcNow;
        user.EmailConfirmed = true;
        user.AccessFailedCount = 0;
        user.LockoutEndAt = null;

        await _authRepository.CompletePasswordResetAsync(
            user,
            resetSession,
            _authContextAccessor.GetClientIpAddress()
        );
    }

    public async Task ChangePasswordAsync(ChangePasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword)) throw new BadRequestException("Current password is required.");

        if (string.IsNullOrWhiteSpace(dto.NewPassword)) throw new InvalidOperationException("New password is required.");

        if (dto.NewPassword.Length < 8) throw new InvalidOperationException("New password must be at least 8 characters.");

        var appUserId = _authContextAccessor.GetAuthenticatedUserId();

        var user = await _authRepository.GetUserByIdWithExternalLoginsAsync(appUserId);

        if (user == null || !user.IsActive || user.DeletedAt != null) throw new UnauthorizedAccessException("Account not found");

        if (string.IsNullOrWhiteSpace(user.PasswordHash)) throw new InvalidOperationException("This account does not have a password yet.");

        var now = DateTime.UtcNow;

        var CurrentPasswordValid = _hasherService.VerifyPassword(dto.CurrentPassword, user.PasswordHash);

        if (!CurrentPasswordValid) throw new BadRequestException("Current password is incorrect.");

        var samePassword = _hasherService.VerifyPassword(dto.NewPassword, user.PasswordHash);

        if (samePassword) throw new BadRequestException("New password must be different from the current password.");

        user.PasswordHash = _hasherService.HashPassword(dto.NewPassword);

        user.PasswordUpdatedAt = now;
        user.UpdatedAt = now;
        user.AccessFailedCount = 0;
        user.LockoutEndAt = null;

        await _authRepository.CompletePasswordChangeAsync(user, _authContextAccessor.GetClientIpAddress());
    }

    public async Task CompleteSetPasswordAsync(CompleteSetPasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SetupToken)) throw new InvalidOperationException("Invalid or expired setup session.");

        if (string.IsNullOrWhiteSpace(dto.NewPassword)) throw new InvalidOperationException("New password is required.");

        if (dto.NewPassword.Length < 8) throw new InvalidOperationException("Password must be at least 8 characters.");

        var appUserId = _authContextAccessor.GetAuthenticatedUserId();

        var tokenHash = _passwordResetTokenService.HashToken(dto.SetupToken);

        var setUpSession = await _authRepository.GetPasswordResetSessionByHashAsync(tokenHash);

        if (
            setUpSession == null ||
            setUpSession.AppUserId != appUserId ||
            setUpSession.ConsumedAt != null ||
            setUpSession.ExpiresAt <= DateTime.UtcNow
        )
        {
            throw new InvalidOperationException("Invalid or expired setup session.");
        }

        var user = setUpSession.AppUser;

        if (user == null || !user.IsActive || user.DeletedAt != null) throw new UnauthorizedAccessException("Account was not found.");

        if (!string.IsNullOrWhiteSpace(user.PasswordHash)) throw new InvalidOperationException("This account already has a password.");

        var now = DateTime.UtcNow;

        user.PasswordHash = _hasherService.HashPassword(dto.NewPassword);

        user.PasswordUpdatedAt = now;
        user.UpdatedAt = now;
        user.EmailConfirmed = true;
        user.AccessFailedCount = 0;
        user.LockoutEndAt = null;

        await _authRepository.CompletePasswordSetupAsync(user, setUpSession);
    }

    public async Task<SecuritySummaryResponseDto> GetSecuritySummaryAsync()
    {
        var appUserId = _authContextAccessor.GetAuthenticatedUserId();

        var user = await _authRepository.GetUserByIdWithExternalLoginsAsync(appUserId);

        var activeSessionCount = await _authRepository.GetActiveUserSessionCountAsync(appUserId);

        if (user == null || !user.IsActive || user.DeletedAt != null)
            throw new UnauthorizedAccessException("Account was not found");

        var connectedProviders = user.ExternalLogins
            .Select(login => login.Provider)
            .Where(provider => !string.IsNullOrWhiteSpace(provider))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(provider => provider)
            .ToList();

        return new SecuritySummaryResponseDto
        {
            HasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash),

            EmailConfirmed = user.EmailConfirmed,
            PasswordUpdatedAt = user.PasswordUpdatedAt,
            ConnectedProviders = connectedProviders,
            ActiveSessionCount = activeSessionCount
        };
    }
}