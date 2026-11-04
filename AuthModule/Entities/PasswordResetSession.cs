namespace DevToolkit.Auth.Entities;

public partial class PasswordResetSession
{
    public int PasswordResetSessionId { get; set; }

    public int AppUserId { get; set; }

    public string Purpose { get; set; } = "password-reset";

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByIp { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;
}
