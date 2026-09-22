using AuthModule.DTOs.Auth;
using AuthModule.Entities;
using AuthModule.Enums.Auth;
using AuthModule.Interfaces.Repositories;
using AuthModule.Interfaces.Services;
using AuthModule.Settings;
using Microsoft.Extensions.Options;

namespace AuthModule.Services;

public class TokenService : ITokenService
{
    private readonly ICustomAuthRepository _authRepository;
    private readonly IAuthContextAccessor _authContextAccessor;
    private readonly IAuthEventLogger _authEventLogger;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserAgentParser _userAgentParser;
    private readonly JwtSettings _jwtSettings;

    public TokenService
    (
        ICustomAuthRepository authRepository,
        IAuthContextAccessor authContextAccessor,
        IAuthEventLogger authEventLogger,
        IJwtTokenService jwtTokenService,
        IUserAgentParser userAgentParser,
        IOptions<JwtSettings> jwtOptions
    )
    {
        _authRepository = authRepository;
        _authContextAccessor = authContextAccessor;
        _authEventLogger = authEventLogger;
        _jwtTokenService = jwtTokenService;
        _userAgentParser = userAgentParser;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var oldTokenHash = _jwtTokenService.HashRefreshToken(dto.RefreshToken);

        var storedToken = await _authRepository.GetRefreshTokenByHashAsync(
            oldTokenHash
        );

        if (storedToken == null)
        {
            await _authEventLogger.LogAsync(
                null,
                null,
                AuthEventType.RefreshTokenFailed,
                false,
                AuthFailureReason.InvalidRefreshToken.ToString()
            );

            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (storedToken.RevokedAt != null)
        {
            await _authEventLogger.LogAsync(
                storedToken.AppUserId,
                storedToken.AppUser.Email,
                AuthEventType.RefreshTokenFailed,
                false,
                AuthFailureReason.RevokedRefreshToken.ToString()
            );

            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            await _authEventLogger.LogAsync(
                storedToken.AppUserId,
                storedToken.AppUser.Email,
                AuthEventType.RefreshTokenFailed,
                false,
                AuthFailureReason.ExpiredRefreshToken.ToString()
            );

            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        if (
            storedToken.UserSessionId == null ||
            storedToken.UserSession == null ||
            storedToken.UserSession.RevokedAt != null
        ) throw new UnauthorizedAccessException("Session is not longer active");

        var user = storedToken.AppUser;

        if (!user.IsActive || user.DeletedAt != null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshToken);

        await _authRepository.RevokeRefreshTokenAsync(
            storedToken,
            _authContextAccessor.GetClientIpAddress(),
            newRefreshTokenHash
        );

        await SaveRefreshTokenAsync(user.AppUserId, storedToken.UserSessionId.Value, newRefreshToken);

        user.LastSeenAt = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user);

        var roles = user.AppUserRoles
            .Select(userRole => userRole.Role.RoleName)
            .ToList();

        await _authEventLogger.LogAsync(
            user.AppUserId,
            user.Email,
            AuthEventType.RefreshTokenSucceeded,
            true,
            "Refresh token rotated successfully."
        );

        await _authRepository.TouchUserSessionAsync(storedToken.UserSessionId.Value, _authContextAccessor.GetClientIpAddress());

        return _jwtTokenService.GenerateAuthResponse(
            user,
            roles,
            newRefreshToken,
            storedToken.UserSessionId.Value
        );
    }

    public async Task LogoutAsync(LogoutRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var revokedByIp = _authContextAccessor.GetClientIpAddress();

        var tokenHash = _jwtTokenService.HashRefreshToken(dto.RefreshToken);

        var storedToken = await _authRepository.GetRefreshTokenByHashAsync(
            tokenHash
        );

        if (storedToken == null || storedToken.RevokedAt != null)
        {
            return;
        }

        if (storedToken!.UserSession != null && storedToken.UserSession.RevokedAt == null)
        {
            storedToken.UserSession.RevokedAt = now;
            storedToken.UserSession.RevokedByIp = revokedByIp;
        }

        await _authRepository.RevokeRefreshTokenAsync(
            storedToken,
            revokedByIp
        );

        var user = storedToken.AppUser;
        user.LastLogoutAt = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user);

        await _authEventLogger.LogAsync(
            user.AppUserId,
            user.Email,
            AuthEventType.LogoutSucceeded,
            true,
            "User logged out successfully."
        );
    }

    public async Task SaveRefreshTokenAsync(int appUserId, Guid userSessionId, string refreshToken)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(refreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            AppUserId = appUserId,
            UserSessionId = userSessionId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = _authContextAccessor.GetClientIpAddress(),
            UserAgent = _authContextAccessor.GetUserAgent()
        };

        await _authRepository.AddRefreshTokenAsync(refreshTokenEntity);
    }
}