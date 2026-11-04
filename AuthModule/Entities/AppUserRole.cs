namespace DevToolkit.Auth.Entities;

public partial class AppUserRole
{
    public int AppUserId { get; set; }

    public int RoleId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
