using DevToolkit.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DevToolkit.Auth.Interfaces;

/// <summary>Database operations needed by authentication, independent of business tables.</summary>
public interface IAuthDbContext
{
    DbSet<AppUser> AppUsers { get; }
    DbSet<Role> Roles { get; }
    DbSet<AppUserRole> AppUserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<PasswordResetSession> PasswordResetSessions { get; }
    DbSet<ExternalLogin> ExternalLogins { get; }
    DbSet<UserAuthEvent> UserAuthEvents { get; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    EntityEntry Entry(object entity);
}
