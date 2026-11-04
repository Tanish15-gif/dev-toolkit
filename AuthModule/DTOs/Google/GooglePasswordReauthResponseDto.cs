namespace DevToolkit.Auth.DTOs;

public class GooglePasswordReauthResponseDto
{
    public string SetupToken { get; set; } = string.Empty;

    public int ExpiresInSeconds { get; set; }
}
