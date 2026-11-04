using DevToolkit.Auth.Exceptions;
using DevToolkit.Auth.Constants;
using DevToolkit.Auth.DTOs;
using DevToolkit.Auth.Entities;
using DevToolkit.Auth.Enums;
using DevToolkit.Auth.Interfaces.Repositories;
using DevToolkit.Auth.Interfaces.Services;
using DevToolkit.Auth.Helpers;
using DevToolkit.Auth.Settings;
using DevToolkit.Auth.Interfaces;

namespace DevToolkit.Auth.Services;

public class CredentialAuthService : ICredentialAuthService
{
    private readonly ICustomAuthRepository _authRepository;
    private readonly IAuthEventLogger _authEventLogger;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;
    private readonly IPasswordHasherService _hasherService;
    private readonly ITokenService _tokenService;
    private readonly IAuthDbContext _dbContext;
    private readonly IEnumerable<IUserRegisteredHook> _userRegisteredHooks;
    public CredentialAuthService
    (
        ICustomAuthRepository authRepository,
        IAuthEventLogger authEventLogger,
        IJwtTokenService jwtTokenService,
        ISessionService sessionService,
        IPasswordHasherService hasherService,
        ITokenService tokenService,
        IEnumerable<IUserRegisteredHook> userRegisteredHooks,
        IAuthDbContext dbContext
    )
    {
        _authRepository = authRepository;
        _authEventLogger = authEventLogger;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _hasherService = hasherService;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _userRegisteredHooks = userRegisteredHooks;
    }
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        ValidateRegisterRequest(dto);

        var normalizedEmail = AuthHelpers.NormalizeEmail(dto.Email);

        var emailExists = await _authRepository.EmailExistsAsync(normalizedEmail);

        if (emailExists)
        {
            await _authEventLogger.LogAsync(
                null,
                dto.Email,
                AuthEventType.RegisterFailed,
                false,
                AuthFailureReason.EmailAlreadyExists.ToString()
            );

            throw new AuthConflictException("Email already exists.");
        }
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var user = new AppUser
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
                ? null
                : dto.PhoneNumber.Trim(),
            PasswordHash = _hasherService.HashPassword(dto.Password),
            EmailConfirmed = false,
            IsActive = true,
            AccessFailedCount = 0,
            CreatedAt = DateTime.UtcNow,
            PasswordUpdatedAt = DateTime.UtcNow
        };

        await _authRepository.CreateUserAsync(user);

        await _authRepository.AssignRoleAsync(user.AppUserId, RoleNames.User);

        foreach (var hook in _userRegisteredHooks)
        {
            await hook.OnUserRegisteredAsync(user);
        }

        var roles = await _authRepository.GetUserRolesAsync(user.AppUserId);

        var userSession = await _sessionService.CreateUserSessionAsync(user.AppUserId);

        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _tokenService.SaveRefreshTokenAsync(user.AppUserId, userSession.UserSessionId, refreshToken);

        await _authEventLogger.LogAsync(
            user.AppUserId,
            user.Email,
            AuthEventType.RegisterSucceeded,
            true,
            "User registered successfully."
        );

        var response = _jwtTokenService.GenerateAuthResponse(user, roles, refreshToken, userSession.UserSessionId);
        await transaction.CommitAsync();
        return response;
    }
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        ValidateLoginRequest(dto);

        var normalizedEmail = AuthHelpers.NormalizeEmail(dto.Email);

        var user = await _authRepository.GetUserByNormalizedEmailAsync(normalizedEmail);

        if (user == null)
        {
            await _authEventLogger.LogAsync(
                null,
                dto.Email,
                AuthEventType.LoginFailed,
                false,
                AuthFailureReason.UserNotFound.ToString()
            );
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        if (!user.IsActive || user.DeletedAt != null)
        {
            await _authEventLogger.LogAsync(
                user.AppUserId,
                user.Email,
                AuthEventType.LoginFailed,
                false,
                AuthFailureReason.AccountInactive.ToString()
            );

            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        if (user.LockoutEndAt != null && user.LockoutEndAt > DateTime.UtcNow)
        {
            await _authEventLogger.LogAsync(
                user.AppUserId,
                user.Email,
                AuthEventType.LoginFailed,
                false,
                AuthFailureReason.AccountLocked.ToString()
            );

            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var isPasswordValid = _hasherService.VerifyPassword(dto.Password, user.PasswordHash ?? "");

        if (!isPasswordValid)
        {
            var failureCount = await _authRepository.RecordFailedLoginAttemptAsync(user.AppUserId);
            if (failureCount >= 5)
            {
                await _authEventLogger.LogAsync(
                    user.AppUserId,
                    user.Email,
                    AuthEventType.AccountLocked,
                    false,
                    "Account locked after too many failed login attempts."
                );
            }
            await _authEventLogger.LogAsync(
                user.AppUserId,
                user.Email,
                AuthEventType.LoginFailed,
                false,
                AuthFailureReason.InvalidPassword.ToString()
            );

            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        if (!await _authRepository.CompletePasswordLoginAsync(user.AppUserId, user.PasswordHash!))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        var roles = await _authRepository.GetUserRolesAsync(user.AppUserId);

        var userSession = await _sessionService.CreateUserSessionAsync(user.AppUserId);

        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _tokenService.SaveRefreshTokenAsync(user.AppUserId, userSession.UserSessionId, refreshToken);

        await _authEventLogger.LogAsync(
            user.AppUserId,
            user.Email,
            AuthEventType.LoginSucceeded,
            true,
            "User logged in successfully"
        );

        var response = _jwtTokenService.GenerateAuthResponse(user, roles, refreshToken, userSession.UserSessionId);
        await transaction.CommitAsync();
        return response;
    }

    private static void ValidateRegisterRequest(RegisterRequestDto dto)
    {
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(dto,
            new System.ComponentModel.DataAnnotations.ValidationContext(dto), true);
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            throw new BadRequestException("Full name is required.");
        }
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new BadRequestException("Email is required.");
        }
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new BadRequestException("Password is required.");
        }
        if (dto.Password.Length < 8)
        {
            throw new BadRequestException("Password must be at least 8 characters.");
        }
    }
    private static void ValidateLoginRequest(LoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new BadRequestException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new BadRequestException("Password is required.");
        }
    }
}
