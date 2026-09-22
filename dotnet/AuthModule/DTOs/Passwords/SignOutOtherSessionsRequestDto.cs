namespace TanishElectronics.Api.DTOs.Auth;

public class SignOutOtherSessionsRequestDto
{
    public string CurrentRefreshToken { get; set; } = string.Empty;
}