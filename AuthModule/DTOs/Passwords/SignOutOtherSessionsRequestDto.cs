namespace DevToolkit.Auth.DTOs;

public class SignOutOtherSessionsRequestDto
{
    public string CurrentRefreshToken { get; set; } = string.Empty;
}
