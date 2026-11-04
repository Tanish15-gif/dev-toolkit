namespace DevToolkit.Auth.Settings;

/// <summary>
/// Application name used in password-recovery emails.
/// </summary>
public class AppInfoSettings
{
    public const string SectionName = "AppInfo";
    public string Name { get; set; } = "Your Project";
}
