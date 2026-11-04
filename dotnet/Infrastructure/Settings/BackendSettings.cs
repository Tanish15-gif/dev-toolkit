namespace DevToolkit.Infrastructure.Settings;

public class BackendSettings
{
    public const string SectionName = "Backend";

    public bool EnableExceptionHandling { get; set; } = true;
    public bool EnableAuthAbuseProtection { get; set; } = true;
    public string[] AuthPathPrefixes { get; set; } = ["/auth/custom"];
    public string[] OptionalAuthenticationPaths { get; set; } = [];
    public string[] IdempotencyPaths { get; set; } = [];
    public int AuthIpPermitLimit { get; set; } = 60;
    public int AuthAccountPermitLimit { get; set; } = 15;
    public int AuthWindowSeconds { get; set; } = 300;
    public int AuthInspectionMaxBytes { get; set; } = 16384;
    public int IdempotencyMaxEntries { get; set; } = 1000;
    public int IdempotencyRetentionSeconds { get; set; } = 600;
    public int IdempotencyMaxRequestBytes { get; set; } = 32768;
    public int IdempotencyMaxResponseBytes { get; set; } = 65536;
    public int SessionStoreSlowMilliseconds { get; set; } = 1000;
    public int HealthCheckTimeoutSeconds { get; set; } = 5;

    public bool IsValid()
    {
        return AuthIpPermitLimit > 0 && AuthAccountPermitLimit > 0 && AuthWindowSeconds > 0 &&
            AuthInspectionMaxBytes is > 0 and <= 1048576 &&
            IdempotencyMaxEntries is > 0 and <= 10000 && IdempotencyRetentionSeconds > 0 &&
            IdempotencyMaxRequestBytes is > 0 and <= 1048576 &&
            IdempotencyMaxResponseBytes is > 0 and <= 1048576 &&
            SessionStoreSlowMilliseconds > 0 && HealthCheckTimeoutSeconds > 0 &&
            AreValidPaths(AuthPathPrefixes) && AreValidPaths(OptionalAuthenticationPaths) &&
            AreValidPaths(IdempotencyPaths);
    }

    private static bool AreValidPaths(string[]? paths)
    {
        return paths != null && paths.All(value =>
            !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') &&
            value != "/" && !value.Contains('?') && !value.Contains('#')
        );
    }
}
