using Microsoft.EntityFrameworkCore;
using TanishElectronics.Api.Data;
using TanishElectronics.Api.Entities;
using TanishElectronics.Api.Interfaces.Repositories.Auth;

namespace TanishElectronics.Api.Repositories.Auth;

public class PasswordRecoveryRepository : IPasswordRecoveryRepository
{
    private readonly TanishElectronicsDbContext _dbContext;
    public PasswordRecoveryRepository
    (
        TanishElectronicsDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }
    public async Task<OtpCode?> GetLatestOtpAsync(string normalizedEmail, string purpose)
    {
        return await _dbContext.OtpCodes
            .AsNoTracking()
            .Where(opt =>
                opt.NormalizedEmail == normalizedEmail &&
                opt.Purpose == purpose
            )
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddOtpAsync(OtpCode otpCode)
    {
        _dbContext.OtpCodes.Add(otpCode);

        await _dbContext.SaveChangesAsync();
    }

    public async Task InvalidateActiveOtpsAsync(string normalizedEmail, string purpose)
    {
        var now = DateTime.UtcNow;

        var activeOtps = await _dbContext.OtpCodes
            .Where(otp =>
                otp.NormalizedEmail == normalizedEmail &&
                otp.Purpose == purpose &&
                otp.ConsumedAt == null
            )
            .ToListAsync();

        if (activeOtps.Count == 0) return;

        foreach (var otp in activeOtps)
        {
            otp.ConsumedAt = now;
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task<OtpCode?> GetLatestActiveOtpAsync(string normalizedEmail, string purpose)
    {
        return await _dbContext.OtpCodes
            .Where(otp =>
                otp.NormalizedEmail == normalizedEmail &&
                otp.Purpose == purpose &&
                otp.ConsumedAt == null
            )
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateOtpAsync(OtpCode otpCode)
    {
        _dbContext.OtpCodes.Update(otpCode);

        await _dbContext.SaveChangesAsync();
    }

    public async Task ConsumeOtpAndCreateResetSessionAsync(OtpCode otpCode, PasswordResetSession resetSession)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;

            var existingSessions = await _dbContext.PasswordResetSessions
                .Where(session =>
                    session.AppUserId == resetSession.AppUserId &&
                    session.ConsumedAt == null
                )
                .ToListAsync();

            foreach (var existingSession in existingSessions)
            {
                existingSession.ConsumedAt = now;
            }

            otpCode.ConsumedAt = now;

            _dbContext.OtpCodes.Update(otpCode);

            _dbContext.PasswordResetSessions.Add(resetSession);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PasswordResetSession?> GetPasswordResetSessionByHashAsync(string tokenHash)
    {
        return await _dbContext.PasswordResetSessions
            .Include(session => session.AppUser)
            .FirstOrDefaultAsync(session =>
                session.TokenHash == tokenHash
            );
    }

    public async Task CompletePasswordResetAsync(AppUser user, PasswordResetSession resetSession, string? revokedByIp)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;

            user.UpdatedAt = now;
            resetSession.ConsumedAt = now;

            var activeSessions = await _dbContext.UserSessions
            .Where(session =>
                session.AppUserId == user.AppUserId &&
                session.RevokedAt == null
            )
            .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.RevokedAt = now;
                session.RevokedByIp = revokedByIp;
            }

            var otherResetSessions = await _dbContext.PasswordResetSessions
                .Where(session =>
                    session.AppUserId == user.AppUserId &&
                    session.ConsumedAt == null &&
                    session.PasswordResetSessionId != resetSession.PasswordResetSessionId
                )
                .ToListAsync();

            foreach (var session in otherResetSessions)
            {
                session.ConsumedAt = now;
            }

            var activeRefreshTokens = await _dbContext.RefreshTokens
                .Where(token =>
                    token.AppUserId == user.AppUserId &&
                    token.RevokedAt == null
                )
                .ToListAsync();

            foreach (var refreshToken in activeRefreshTokens)
            {
                refreshToken.RevokedAt = now;
                refreshToken.RevokedByIp = revokedByIp;
            }

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task CompletePasswordChangeAsync(AppUser user, string? revokedByIp)
    {
        var now = DateTime.UtcNow;

        var activeSessions = await _dbContext.UserSessions
            .Where(session =>
            session.AppUserId == user.AppUserId &&
            session.RevokedAt == null
        )
        .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.RevokedAt = now;
            session.RevokedByIp = revokedByIp;
        }

        var activeRefreshTokens = await _dbContext.RefreshTokens
            .Where(token =>
                token.AppUserId == user.AppUserId &&
                token.RevokedAt == null
            )
            .ToListAsync();

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAt = now;
            refreshToken.RevokedByIp = revokedByIp;
        }
        _dbContext.AppUsers.Update(user);

        await _dbContext.SaveChangesAsync();
    }
}