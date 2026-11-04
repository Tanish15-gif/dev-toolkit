namespace DevToolkit.Auth.DTOs.Auth;

public class VerifyForgotPasswordOtpResponseDto
{
    public string ResetToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
