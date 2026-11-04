namespace DevToolkit.Auth.Entities;

public partial class RefreshToken
{
    public int RefreshTokenId { get; set; }

    public int AppUserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByIp { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public Guid? UserSessionId { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;

    public virtual UserSession? UserSession { get; set; }
}
