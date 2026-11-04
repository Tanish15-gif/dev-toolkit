using Microsoft.EntityFrameworkCore;
using DevToolkit.Auth.Interfaces;
using DevToolkit.Auth.Entities;
using DevToolkit.Auth.Interfaces.Repositories;
using Microsoft.Extensions.Options;
using DevToolkit.Auth.Settings;

namespace DevToolkit.Auth.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly IAuthDbContext _dbContext;
    private readonly JwtSettings _settings;
    public SessionRepository(IAuthDbContext dbContext, IOptions<JwtSettings> options)
    {
        _dbContext = dbContext;
        _settings = options.Value;
    }
    public async Task<int> GetActiveSessionCountAsync(int appUserId)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.RefreshTokens
            .CountAsync(token =>
                token.AppUserId == appUserId &&
                token.RevokedAt == null &&
                token.ExpiresAt > now && token.UserSession != null && token.UserSession.RevokedAt == null &&
                token.UserSession.CreatedAt > now.AddDays(-_settings.AbsoluteSessionDays)
            );
    }

    public async Task RevokeOtherSessionsAsync(int appUserId, Guid currentSessionId, string? revokedByIp)
    {
        var now = DateTime.UtcNow;

        var otherSessions = await _dbContext.UserSessions
            .Where(session =>
                session.AppUserId == appUserId &&
                session.UserSessionId != currentSessionId &&
                session.RevokedAt == null
            )
            .ToListAsync();

        foreach (var session in otherSessions)
        {
            session.RevokedAt = now;
            session.RevokedByIp = revokedByIp;
        }

        var otherTokens = await _dbContext.RefreshTokens
            .Where(token =>
                token.AppUserId == appUserId &&
                token.UserSessionId != currentSessionId &&
                token.RevokedAt == null
            )
            .ToListAsync();

        foreach (var token in otherTokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = revokedByIp;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokensAsync(int appUserId, string? revokedByIp)
    {
        var now = DateTime.UtcNow;

        var activeSessions = await _dbContext.UserSessions
            .Where(session =>
                session.AppUserId == appUserId &&
                session.RevokedAt == null
            )
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.RevokedAt = now;
            session.RevokedByIp = revokedByIp;
        }

        var activeTokens = await _dbContext.RefreshTokens
            .Where(token =>
                token.AppUserId == appUserId &&
                token.RevokedAt == null
            )
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = revokedByIp;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<UserSession> AddUserSessionAsync(UserSession userSession)
    {
        _dbContext.UserSessions.Add(userSession);

        await _dbContext.SaveChangesAsync();

        return userSession;
    }

    public async Task TouchUserSessionAsync(Guid userSessionId, string? ipAddress)
    {
        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(item =>
                item.UserSessionId == userSessionId &&
                item.RevokedAt == null
            );

        if (session == null)
        {
            return;
        }
        session.LastSeenAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            session.IpAddress = ipAddress;
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task<int> GetActiveUserSessionCountAsync(int appUserId)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.UserSessions
            .CountAsync(session =>
                session.AppUserId == appUserId &&
                session.RevokedAt == null && session.CreatedAt > now.AddDays(-_settings.AbsoluteSessionDays) &&
                session.RefreshTokens.Any(token => token.RevokedAt == null && token.ExpiresAt > now)
            );
    }

    public async Task<List<UserSession>> GetActiveUserSessionsAsync(int appUserId)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.UserSessions
            .AsNoTracking()
            .Where(session =>
                session.AppUserId == appUserId &&
                session.RevokedAt == null &&
                session.CreatedAt > now.AddDays(-_settings.AbsoluteSessionDays) &&
                session.RefreshTokens.Any(token =>
                    token.RevokedAt == null &&
                    token.ExpiresAt > now
                )
            )
            .OrderByDescending(session => session.LastSeenAt)
            .ToListAsync();
    }

    public async Task<bool> RevokeUserSessionAsync(int appUserId, Guid userSessionId, string? revokedByIp)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var session = await _dbContext.UserSessions
                .FirstOrDefaultAsync(item =>
                    item.UserSessionId == userSessionId &&
                    item.AppUserId == appUserId &&
                    item.RevokedAt == null
                );

            if (session == null)
            {
                return false;
            }
            var now = DateTime.UtcNow;

            session.RevokedAt = now;
            session.RevokedByIp = revokedByIp;

            var activeTokens = await _dbContext.RefreshTokens
                .Where(token =>
                    token.UserSessionId == userSessionId &&
                    token.AppUserId == appUserId &&
                    token.RevokedAt == null
                )
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
                token.RevokedByIp = revokedByIp;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId, int appUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var earliestCreatedAt = now.AddDays(-_settings.AbsoluteSessionDays);

        return await _dbContext.UserSessions
            .AsNoTracking()
            .AnyAsync(session =>
                session.UserSessionId == sessionId &&
                session.AppUserId == appUserId &&
                session.RevokedAt == null &&
                session.CreatedAt > earliestCreatedAt &&
                session.AppUser.IsActive &&
                session.AppUser.DeletedAt == null,
                cancellationToken
            );
    }
}
