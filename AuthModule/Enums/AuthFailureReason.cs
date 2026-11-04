namespace DevToolkit.Auth.Enums;

public enum AuthFailureReason
{
    None = 0,

    EmailAlreadyExists = 1,
    UserNotFound = 2,
    InvalidPassword = 3,
    AccountInactive = 4,
    AccountLocked = 5,
    EmailNotConfirmed = 6,

    InvalidRefreshToken = 20,
    ExpiredRefreshToken = 21,
    RevokedRefreshToken = 22,

    InvalidOtp = 30,
    ExpiredOtp = 31,
    TooManyOtpAttempts = 32
}
