using DevToolkit.Auth.Exceptions;
using Microsoft.EntityFrameworkCore;
using DevToolkit.Auth.Interfaces;
using DevToolkit.Auth.Entities;
using DevToolkit.Auth.Interfaces.Repositories;

namespace DevToolkit.Auth.Repositories;

public class GoogleAuthRepository : IGoogleAuthRepository
{
    private readonly IAuthDbContext _dbContext;
    public GoogleAuthRepository(IAuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<ExternalLogin?> GetExternalLoginAsync(string provider, string providerUserId)
    {
        return await _dbContext.ExternalLogins
            .Include(externalLogin => externalLogin.AppUser)
                .ThenInclude(user => user.AppUserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(externalLogin =>
                externalLogin.Provider == provider &&
                externalLogin.ProviderUserId == providerUserId);
    }

    public async Task AddExternalLoginAsync(ExternalLogin externalLogin)
    {
        _dbContext.ExternalLogins.Add(externalLogin);

        await _dbContext.SaveChangesAsync();
    }

    public async Task CreatePasswordSetupSessionAsync(PasswordResetSession setupSession)
    {
        var now = DateTime.UtcNow;

        var existingSessions = await _dbContext.PasswordResetSessions
            .Where(session =>
                session.AppUserId == setupSession.AppUserId &&
                session.ConsumedAt == null
            )
            .ToListAsync();

        foreach (var session in existingSessions)
        {
            session.ConsumedAt = now;
        }

        _dbContext.PasswordResetSessions.Add(setupSession);

        await _dbContext.SaveChangesAsync();
    }

    public async Task CompletePasswordSetupAsync(AppUser user, PasswordResetSession setupSession)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;

            var affectedRows = await _dbContext.PasswordResetSessions
                .Where(session =>
                    session.PasswordResetSessionId == setupSession.PasswordResetSessionId &&
                    session.Purpose == "password-setup" &&
                    session.ConsumedAt == null &&
                    session.ExpiresAt > now &&
                    session.AppUser.IsActive && session.AppUser.DeletedAt == null &&
                    session.AppUser.PasswordHash == null
                )
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        session => session.ConsumedAt,
                        now
                    )
                );

            if (affectedRows != 1) throw new BadRequestException("Invalid or expired setup session.");

            _dbContext.Entry(setupSession).State = EntityState.Detached;
            // The user is tracked; persist only fields changed by password setup.

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
