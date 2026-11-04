using DevToolkit.Auth.Entities;
using DevToolkit.Auth.Extensions;
using DevToolkit.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevToolkit.Auth.Data;

/// <summary>
/// Ready-to-use auth context. Inherit from this class to share your application's context,
/// or use AddDevToolkitAuth(configuration, configureDatabase) to register it directly.
/// </summary>
public class AuthDbContext : DbContext, IAuthDbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected AuthDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<PasswordResetSession> PasswordResetSessions => Set<PasswordResetSession>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<UserAuthEvent> UserAuthEvents => Set<UserAuthEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddDevToolkitAuthModel();
    }
}
