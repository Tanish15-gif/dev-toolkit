namespace AuthModule.Constants;

/// <summary>Rate limit policy name constants used with [EnableRateLimiting].</summary>
public static class RateLimitPolicies
{
    // ---- Auth-specific (registered by AddJwtAuthModule) ----
    /// <summary>5 requests per 5 minutes — login, register, Google sign-in.</summary>
    public const string AuthAttempt = "auth-attempt";

    /// <summary>3 requests per 10 minutes — OTP send/resend.</summary>
    public const string OtpRequest = "otp-request";

    /// <summary>10 attempts per 5 minutes — OTP verification.</summary>
    public const string OtpVerify = "otp-verify";

    // ---- General (register these in your host project) ----
    /// <summary>120 requests per minute — general read endpoints.</summary>
    public const string PublicRead = "public-read";

    /// <summary>10 requests per minute — form submissions (contact, enquiry, etc.).</summary>
    public const string FormSubmit = "form-submit";

    /// <summary>8 requests per 5 minutes — image/file upload.</summary>
    public const string ImageUpload = "image-upload";

    /// <summary>80 requests per minute — admin actions.</summary>
    public const string AdminAction = "admin-action";

    /// <summary>15 messages per minute — real-time chat.</summary>
    public const string ChatMessage = "chat-message";
}
