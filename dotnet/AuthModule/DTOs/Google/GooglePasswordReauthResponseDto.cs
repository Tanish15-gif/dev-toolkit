namespace TanishElectronics.Api.DTOs.Auth;

public class GooglePasswordReauthResponseDto
{
    public string SetupToken { get; set; } = string.Empty;

    public int ExpiresInSeconds { get; set; }
}