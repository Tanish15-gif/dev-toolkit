namespace DevToolkit.Auth.Entities;

public partial class UserSession
{
    public Guid UserSessionId { get; set; }

    public int AppUserId { get; set; }

    public string DeviceType { get; set; } = null!;

    public string DeviceName { get; set; } = null!;

    public string BrowserName { get; set; } = null!;

    public string? BrowserVersion { get; set; }

    public string OperatingSystem { get; set; } = null!;

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
