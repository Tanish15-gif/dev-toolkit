namespace DevToolkit.Auth.Settings;

/// <summary>Limits and health-check timeouts for authentication endpoints.</summary>
public class AuthModuleSettings
{
    public const string SectionName = "AuthModule";

    public bool EnableAbuseProtection { get; set; } = true;
    public int AuthIpPermitLimit { get; set; } = 60;
    public int AuthAccountPermitLimit { get; set; } = 15;
    public int AuthWindowSeconds { get; set; } = 300;
    public int AuthInspectionMaxBytes { get; set; } = 16384;
    public int SessionStoreSlowMilliseconds { get; set; } = 1000;
    public int HealthCheckTimeoutSeconds { get; set; } = 5;

    public bool IsValid()
    {
        return AuthIpPermitLimit > 0 &&
            AuthAccountPermitLimit > 0 &&
            AuthWindowSeconds > 0 &&
            AuthInspectionMaxBytes is > 0 and <= 1048576 &&
            SessionStoreSlowMilliseconds > 0 &&
            HealthCheckTimeoutSeconds > 0;
    }
}
