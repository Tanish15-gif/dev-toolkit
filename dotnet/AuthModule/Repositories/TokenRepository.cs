using Microsoft.EntityFrameworkCore;
using DevToolkit.Auth.Interfaces;
using DevToolkit.Auth.Entities;
using DevToolkit.Auth.Interfaces.Repositories;

namespace DevToolkit.Auth.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly IAuthDbContext _dbContext;
    public TokenRepository(IAuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync();
    }
    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
    {
        return await _dbContext.RefreshTokens
            .Include(token => token.UserSession)
            .Include(token => token.AppUser)
                .ThenInclude(user => user.AppUserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    public async Task<bool> RotateRefreshTokenAsync(RefreshToken oldToken, RefreshToken replacement, string? revokedByIp)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        var affected = await _dbContext.RefreshTokens
            .Where(token => token.RefreshTokenId == oldToken.RefreshTokenId &&
                token.RevokedAt == null && token.ExpiresAt > now &&
                token.UserSession != null && token.UserSession.RevokedAt == null &&
                token.AppUser.IsActive && token.AppUser.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.RevokedByIp, revokedByIp)
                .SetProperty(token => token.ReplacedByTokenHash, replacement.TokenHash));

        if (affected != 1) return false;

        // ExecuteUpdate bypasses tracking; keep later SaveChanges from overwriting consumption.
        _dbContext.Entry(oldToken).State = EntityState.Detached;
        _dbContext.RefreshTokens.Add(replacement);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string? revokedByIp, string replacedByTokenHash = null!)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = revokedByIp;
        refreshToken.ReplacedByTokenHash = replacedByTokenHash;

        await _dbContext.SaveChangesAsync();
    }
}
