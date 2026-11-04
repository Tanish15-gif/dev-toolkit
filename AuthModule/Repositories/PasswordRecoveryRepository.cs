using DevToolkit.Auth.Exceptions;
using Microsoft.EntityFrameworkCore;
using DevToolkit.Auth.Interfaces;
using DevToolkit.Auth.Entities;
using DevToolkit.Auth.Interfaces.Repositories;

namespace DevToolkit.Auth.Repositories;

public class PasswordRecoveryRepository : IPasswordRecoveryRepository
{
    private readonly IAuthDbContext _dbContext;
    public PasswordRecoveryRepository
    (
        IAuthDbContext dbContext
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

        if (activeOtps.Count == 0)
        {
            return;
        }
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
        await _dbContext.OtpCodes.Where(otp => otp.OtpCodeId == otpCode.OtpCodeId && otp.ConsumedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(otp => otp.ConsumedAt, otpCode.ConsumedAt));
        _dbContext.Entry(otpCode).State = EntityState.Detached;
    }

    public async Task RecordFailedOtpAttemptAsync(int otpCodeId, int maxAttempts)
    {
        var now = DateTime.UtcNow;
        await _dbContext.OtpCodes
            .Where(otp => otp.OtpCodeId == otpCodeId && otp.ConsumedAt == null &&
                otp.ExpiresAt > now && otp.AttemptCount < maxAttempts)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(otp => otp.AttemptCount, otp => otp.AttemptCount + 1)
                .SetProperty(otp => otp.ConsumedAt, otp => otp.AttemptCount + 1 >= maxAttempts ? now : (DateTime?)null));
        var tracked = _dbContext.OtpCodes.Local.FirstOrDefault(otp => otp.OtpCodeId == otpCodeId);
        if (tracked != null)
        {
            _dbContext.Entry(tracked).State = EntityState.Detached;
        }
    }

    public async Task ConsumeOtpAndCreateResetSessionAsync(OtpCode otpCode, PasswordResetSession resetSession, int maxAttempts)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;

            var affected = await _dbContext.OtpCodes
                .Where(otp => otp.OtpCodeId == otpCode.OtpCodeId && otp.ConsumedAt == null &&
                    otp.ExpiresAt > now && otp.AttemptCount < maxAttempts &&
                    otp.AppUser != null && otp.AppUser.IsActive && otp.AppUser.DeletedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(otp => otp.ConsumedAt, now));
            if (affected != 1)
            {
                throw new BadRequestException("Invalid or expired verification code.");
            }
            _dbContext.Entry(otpCode).State = EntityState.Detached;

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
            var affected = await _dbContext.PasswordResetSessions
                .Where(session => session.PasswordResetSessionId == resetSession.PasswordResetSessionId &&
                    session.Purpose == "password-reset" &&
                    session.ConsumedAt == null && session.ExpiresAt > now &&
                    session.AppUser.IsActive && session.AppUser.DeletedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.ConsumedAt, now));
            if (affected != 1)
            {
                throw new BadRequestException("Invalid or expired reset session.");
            }
            _dbContext.Entry(resetSession).State = EntityState.Detached;

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
        // Persist only the changed password fields on the tracked user.

        await _dbContext.SaveChangesAsync();
    }
}
