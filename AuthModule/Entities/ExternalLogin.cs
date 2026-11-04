namespace DevToolkit.Auth.Entities;

public partial class ExternalLogin
{
    public int ExternalLoginId { get; set; }

    public int AppUserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderUserId { get; set; } = null!;

    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;
}
