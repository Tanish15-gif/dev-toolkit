namespace AuthModule.Entities;

public partial class UserAuthEvent
{
    public int UserAuthEventId { get; set; }

    public int? AppUserId { get; set; }

    public string? Email { get; set; }

    public string EventType { get; set; } = null!;

    public bool Success { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? AppUser { get; set; }
}
