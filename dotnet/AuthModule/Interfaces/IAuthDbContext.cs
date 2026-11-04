using Microsoft.EntityFrameworkCore.ChangeTracking;
using DevToolkit.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DevToolkit.Auth.Interfaces;

/// <summary>
/// Abstraction over the host project's DbContext for auth module portability.
/// Your project's DbContext just needs to implement this interface.
/// </summary>
public interface IAuthDbContext
{
    DbSet<AppUser> AppUsers
    {
        get;
    }
    DbSet<Role> Roles
    {
        get;
    }
    DbSet<AppUserRole> AppUserRoles
    {
        get;
    }
    DbSet<RefreshToken> RefreshTokens
    {
        get;
    }
    DbSet<UserSession> UserSessions
    {
        get;
    }
    DbSet<OtpCode> OtpCodes
    {
        get;
    }
    DbSet<PasswordResetSession> PasswordResetSessions
    {
        get;
    }
    DbSet<ExternalLogin> ExternalLogins
    {
        get;
    }
    DbSet<UserAuthEvent> UserAuthEvents
    {
        get;
    }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DatabaseFacade Database
    {
        get;
    }
    EntityEntry Entry(object entity);
}
