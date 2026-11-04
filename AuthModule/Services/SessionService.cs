using DevToolkit.Auth.DTOs;
using DevToolkit.Auth.Entities;
using DevToolkit.Auth.Exceptions;
using DevToolkit.Auth.Interfaces.Repositories;
using DevToolkit.Auth.Interfaces.Services;

namespace DevToolkit.Auth.Services;

public class SessionService : ISessionService
{
    private readonly IAuthContextAccessor _authContextAccessor;
    private readonly ICustomAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserAgentParser _userAgentParser;
    public SessionService
    (
        IAuthContextAccessor authContextAccessor,
        ICustomAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        IUserAgentParser userAgentParser
    )
    {
        _authContextAccessor = authContextAccessor;
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _userAgentParser = userAgentParser;
    }
    public async Task SignOutOtherSessionsAsync(SignOutOtherSessionsRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentRefreshToken))
        {
            throw new UnauthorizedAccessException("Current session was not found.");
        }
        var appUserId = _authContextAccessor.GetAuthenticatedUserId();

        var currentTokenHash = _jwtTokenService.HashRefreshToken(dto.CurrentRefreshToken);

        var currentToken = await _authRepository.GetRefreshTokenByHashAsync(currentTokenHash);

        if (
            currentToken == null ||
            currentToken.AppUserId != appUserId ||
            currentToken.RevokedAt != null ||
            currentToken.UserSessionId == null || currentToken.UserSession == null ||
            currentToken.UserSession.RevokedAt != null ||
            currentToken.ExpiresAt <= DateTime.UtcNow
        )
        {
            throw new UnauthorizedAccessException("Current session is invalid.");
        }

        await _authRepository.RevokeOtherSessionsAsync(appUserId, currentToken.UserSessionId!.Value, _authContextAccessor.GetClientIpAddress());
    }

    public async Task SignOutAllSessionsAsync()
    {
        var appUserId = _authContextAccessor.GetAuthenticatedUserId();

        await _authRepository.RevokeAllRefreshTokensAsync(appUserId, _authContextAccessor.GetClientIpAddress());
    }

    public async Task<List<ActiveSessionDto>> GetActiveSessionsAsync()
    {
        var appUserId = _authContextAccessor.GetAuthenticatedUserId();
        var currentSessionId = _authContextAccessor.GetAuthenticatedSessionId();

        var sessions = await _authRepository.GetActiveUserSessionsAsync(appUserId);

        return sessions.Select(session =>
            new ActiveSessionDto
            {
                UserSessionId = session.UserSessionId,
                DeviceType = session.DeviceType,
                DeviceName = session.DeviceName,
                BrowserName = session.BrowserName,
                BrowserVersion = session.BrowserVersion,
                OperatingSystem = session.OperatingSystem,
                IpAddress = session.IpAddress,
                CreatedAt = session.CreatedAt,
                LastSeenAt = session.LastSeenAt,

                IsCurrent =
                    session.UserSessionId == currentSessionId
            }
        ).ToList();
    }

    public async Task RevokeSessionAsync(Guid userSessionId)
    {
        var appUserId = _authContextAccessor.GetAuthenticatedUserId();
        var currentSessionId = _authContextAccessor.GetAuthenticatedSessionId();

        if (userSessionId == Guid.Empty)
        {
            throw new BadRequestException("Session identifier is required");
        }
        if (userSessionId == currentSessionId)
        {
            throw new BadRequestException("Use normal sign out to end the current session.");
        }
        var revoked = await _authRepository.RevokeUserSessionAsync(appUserId, userSessionId, _authContextAccessor.GetClientIpAddress());

        if (!revoked)
        {
            throw new KeyNotFoundException("Active session not found");
        }
    }

    public async Task<UserSession> CreateUserSessionAsync(int appUserId)
    {
        var userAgent = _authContextAccessor.GetUserAgent();

        var details = _userAgentParser.Parse(userAgent);

        var now = DateTime.UtcNow;

        var session = new UserSession
        {
            AppUserId = appUserId,

            DeviceType = details.DeviceType,
            DeviceName = details.DeviceName,

            BrowserName = details.BrowserName,
            BrowserVersion = details.BrowserVersion,

            OperatingSystem =
                details.OperatingSystem,

            UserAgent = userAgent,
            IpAddress = _authContextAccessor.GetClientIpAddress(),

            CreatedAt = now,
            LastSeenAt = now,

            RevokedAt = null,
            RevokedByIp = null
        };

        return await _authRepository.AddUserSessionAsync(session);
    }
}
