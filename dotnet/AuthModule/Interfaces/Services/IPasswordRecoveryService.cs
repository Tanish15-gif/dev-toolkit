using DevToolkit.Auth.DTOs.Auth;

namespace DevToolkit.Auth.Interfaces.Services;

public interface IPasswordRecoveryService
{
    Task RequestForgotPasswordOtpAsync(ForgotPasswordRequestDto dto);

    Task<VerifyForgotPasswordOtpResponseDto> VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequestDto dto);
    Task ResetPasswordAsync(ResetPasswordRequestDto dto);
    Task ChangePasswordAsync(ChangePasswordRequestDto dto);
    Task CompleteSetPasswordAsync(CompleteSetPasswordRequestDto dto);
    Task<SecuritySummaryResponseDto> GetSecuritySummaryAsync();
}
