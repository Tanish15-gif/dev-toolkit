namespace DevToolkit.Auth.DTOs;

public class SecuritySummaryResponseDto
{
    public bool HasPassword { get; set; }
    public bool EmailConfirmed { get; set; }

    public DateTime? PasswordUpdatedAt { get; set; }

    public List<string> ConnectedProviders { get; set; } = [];
    public int ActiveSessionCount { get; set; }
}
