using DevToolkit.Auth.Entities;

namespace DevToolkit.Auth.Interfaces.Repositories;

public interface ITokenRepository
{
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> RotateRefreshTokenAsync(RefreshToken oldToken, RefreshToken replacement, string? revokedByIp);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string? revokedByIp, string replacedByTokenHash = null!);
}
