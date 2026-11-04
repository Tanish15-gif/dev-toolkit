namespace DevToolkit.Auth.Enums;

public enum AuthEventType
{
    RegisterSucceeded = 1,
    RegisterFailed = 2,

    LoginSucceeded = 10,
    LoginFailed = 11,

    LogoutSucceeded = 20,

    RefreshTokenSucceeded = 30,
    RefreshTokenFailed = 31,

    EmailVerificationOtpSent = 40,
    EmailVerified = 41,

    ForgotPasswordOtpSent = 50,
    PasswordResetSucceeded = 51,

    AccountLocked = 60
}
