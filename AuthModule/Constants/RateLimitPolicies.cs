namespace DevToolkit.Auth.Constants;

/// <summary>Named policies are scoped to the module to avoid host policy collisions.</summary>
public static class RateLimitPolicies
{
    public const string AuthAttempt = "devtoolkit-auth-attempt";
    public const string OtpRequest = "devtoolkit-otp-request";
    public const string OtpVerify = "devtoolkit-otp-verify";
    public const string PublicRead = "devtoolkit-auth-read";
    public const string FormSubmit = "devtoolkit-auth-submit";
}
