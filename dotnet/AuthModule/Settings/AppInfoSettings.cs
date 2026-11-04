namespace DevToolkit.Auth.Settings;

/// <summary>
/// Basic app branding used in outgoing emails.
/// Bind via: builder.Services.Configure&lt;AppInfoSettings&gt;(config.GetSection("AppInfo"));
/// appsettings.json: { "AppInfo": { "Name": "My App", "SupportEmail": "support@myapp.com" } }
/// </summary>
public class AppInfoSettings
{
    public const string SectionName = "AppInfo";
    public string Name { get; set; } = "My App";
    public string? SupportEmail { get; set; }
    public string? BaseUrl { get; set; }
}
