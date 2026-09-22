namespace TanishElectronics.Api.DTOs.Auth;

public class CompleteSetPasswordRequestDto
{
    public string SetupToken { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}