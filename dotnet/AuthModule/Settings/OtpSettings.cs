namespace AuthModule.Settings;

public class OtpSettings
{
    public const string SectionName = "Otp";
    public int ExpiryMinutes { get; set; } = 10;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
    public int ResetTokenExpiryMinutes { get; set; } = 10;
    public string HashKey { get; set; } = string.Empty;
}