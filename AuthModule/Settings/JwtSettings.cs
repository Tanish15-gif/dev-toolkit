namespace DevToolkit.Auth.Settings;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
    public int AbsoluteSessionDays { get; set; } = 30;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(SecretKey)
        && System.Text.Encoding.UTF8.GetByteCount(SecretKey) >= 32
        && !string.IsNullOrWhiteSpace(Issuer)
        && !string.IsNullOrWhiteSpace(Audience)
        && AccessTokenMinutes is > 0 and <= 60
        && RefreshTokenDays is > 0 and <= 90
        && AbsoluteSessionDays is > 0 and <= 90;
    }
}
