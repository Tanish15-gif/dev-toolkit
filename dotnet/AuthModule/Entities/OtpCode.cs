namespace AuthModule.Entities;

public partial class OtpCode
{
    public int OtpCodeId { get; set; }

    public int? AppUserId { get; set; }

    public string Email { get; set; } = null!;

    public string NormalizedEmail { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public string CodeHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByIp { get; set; }

    public virtual AppUser? AppUser { get; set; }
}
