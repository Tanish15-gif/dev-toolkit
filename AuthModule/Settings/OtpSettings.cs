namespace DevToolkit.Auth.Settings;

public class OtpSettings
{
    public const string SectionName = "Otp";
    public int ExpiryMinutes { get; set; } = 10;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
    public int ResetTokenExpiryMinutes { get; set; } = 10;
    public string HashKey { get; set; } = string.Empty;

    public bool IsValid()
    {
        if (ExpiryMinutes <= 0 || ResendCooldownSeconds <= 0 || MaxAttempts <= 0 || ResetTokenExpiryMinutes <= 0)
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(HashKey).Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
