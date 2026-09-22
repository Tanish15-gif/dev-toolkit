using Google.Apis.Auth;
using AuthModule.Constants;
using AuthModule.DTOs.Auth;
using AuthModule.Entities;
using AuthModule.Enums.Auth;
using AuthModule.Interfaces;
using AuthModule.Interfaces.Repositories;
using AuthModule.Interfaces.Services;
using AuthModule.Services.Helpers;
using AuthModule.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthModule.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly ICustomAuthRepository _authRepository;
    private readonly GoogleAuthSettings _googleAuthSettings;
    private readonly IAuthContextAccessor _authContextAccessor;
    private readonly IAuthEventLogger _authEventLogger;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;
    private readonly ITokenService _tokenService;
    private readonly OtpSettings _otpSettings;
    private readonly IPasswordResetTokenService _passwordResetTokenService;
    private readonly IServiceProvider _serviceProvider;

    public GoogleAuthService
    (
        ICustomAuthRepository authRepository,
        IOptions<GoogleAuthSettings> googleAuthSettings,
        IAuthContextAccessor authContextAccessor,
        IAuthEventLogger authEventLogger,
        IJwtTokenService jwtTokenService,
        ISessionService sessionService,
        ITokenService tokenService,
        IOptions<OtpSettings> otpOptions,
        IPasswordResetTokenService passwordResetTokenService,
        IServiceProvider serviceProvider
    )
    {
        _authRepository = authRepository;
        _googleAuthSettings = googleAuthSettings.Value;
        _authContextAccessor = authContextAccessor;
        _authEventLogger = authEventLogger;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _tokenService = tokenService;
        _otpSettings = otpOptions.Value;
        _passwordResetTokenService = passwordResetTokenService;
        _serviceProvider = serviceProvider;
    }

    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto)
    {
        ///<section>
        /// Validation of the necessary data!
        if (string.IsNullOrWhiteSpace(dto.IdToken))
        {
            throw new UnauthorizedAccessException("Google token is required");
        }
        if (string.IsNullOrWhiteSpace(_googleAuthSettings.ClientId))
        {
            throw new UnauthorizedAccessException("google client id is missing");
        }
        ///</section>

        /// <section>
        /// Getting the Google data from the Google
        /// Name, Email, Profile Picture
        GoogleJsonWebSignature.Payload payload;
        ///</section>

        try
        {
            ///<section>
            /// Validating the Google token from the client Id
            payload = await GoogleJsonWebSignature.ValidateAsync(
                dto.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleAuthSettings.ClientId }
                }
            );
            ///</section>
        }
        catch
        {
            ///<section>
            /// If Google token is invalid throw
            await _authEventLogger.LogAsync(
                null,
                null,
                AuthEventType.LoginFailed,
                false,
                "Invalid Google Token."
            );
            throw new UnauthorizedAccessException("Invalid Google Token");
            /// </section>
        }

        /// <section>
        /// Checking if Google values are not null
        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new UnauthorizedAccessException("Google account email was not found.");
        }

        if (!payload.EmailVerified)
        {
            throw new UnauthorizedAccessException("Google email is not verified.");
        }

        if (string.IsNullOrWhiteSpace(payload.Subject))
        {
            throw new UnauthorizedAccessException("Google account identifier was not found.");
        }
        /// </section>

        var provider = ExternalLoginProvider.Google.ToString();
        var providerUserId = payload.Subject.Trim();
        var normalizedEmail = AuthHelpers.NormalizeEmail(payload.Email);

        var externalLogin = await _authRepository.GetExternalLoginAsync(provider, providerUserId);

        AppUser? user = externalLogin?.AppUser;

        var userWasCreated = false;

        if (user == null)
        {
            user = await _authRepository.GetUserByNormalizedEmailAsync(normalizedEmail);

            if (user == null)
            {
                var hasGooglePicture = !string.IsNullOrWhiteSpace(payload.Picture);

                user = new AppUser
                {
                    ClerkUserId = null,

                    FullName = string.IsNullOrWhiteSpace(payload.Name)
                        ? payload.Email.Trim()
                        : payload.Name.Trim(),

                    Email = payload.Email.Trim(),
                    NormalizedEmail = normalizedEmail,
                    PhoneNumber = null,
                    PasswordHash = null,

                    EmailConfirmed = true,
                    IsActive = true,
                    AccessFailedCount = 0,

                    ProfileImageUrl = hasGooglePicture
                        ? payload.Picture : null,

                    ProfileImageSource = hasGooglePicture
                        ? provider : null,

                    ProfileImageUpdatedAt = hasGooglePicture
                        ? DateTime.UtcNow
                        : null,

                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow
                };

                await _authRepository.CreateUserAsync(user);

                await _authRepository.AssignRoleAsync(user.AppUserId, RoleNames.Customer);

                await OnUserRegistered(user);

                userWasCreated = true;

                await _authEventLogger.LogAsync(
                    user.AppUserId,
                    user.Email,
                    AuthEventType.RegisterSucceeded,
                    true,
                    "User registered with Google."
                );
            }
            else
            {
                AuthHelpers.EnsureUserIsActive(user);
            }

            // Existing User update profile and all.

            if (!userWasCreated)
            {
                user.EmailConfirmed = true;
                user.LastLoginAt = DateTime.UtcNow;
                user.LastSeenAt = DateTime.UtcNow;

                var canUseGoogleAvatar = string.IsNullOrWhiteSpace(user.ProfileImageUrl) ||
                    string.Equals(
                        user.ProfileImageSource,
                        provider,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (canUseGoogleAvatar && !string.IsNullOrWhiteSpace(payload.Picture))
                {
                    user.ProfileImageUrl = payload.Picture;
                    user.ProfileImageSource = provider;
                    user.ProfileImageUpdatedAt = DateTime.UtcNow;
                }
                await _authRepository.UpdateUserAsync(user);
            }
        }

        if (externalLogin == null)
        {
            var newExternalLogin = new ExternalLogin
            {
                AppUserId = user.AppUserId,
                Provider = provider,
                ProviderUserId = providerUserId,
                Email = payload.Email.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _authRepository.AddExternalLoginAsync(newExternalLogin);
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
            "User logged in with Google."
        );

        return _jwtTokenService.GenerateAuthResponse(user, roles, refreshToken, userSession.UserSessionId);
    }

    public async Task<GooglePasswordReauthResponseDto> ReauthenticateGoogleForPasswordAsync(GooglePasswordReauthRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdToken)) throw new InvalidOperationException("Google token is required");

        var appUserId = _authContextAccessor.GetAuthenticatedUserId();

        var user = await _authRepository.GetUserByIdWithExternalLoginsAsync(appUserId);

        if (user == null || !user.IsActive || user.DeletedAt != null) throw new UnauthorizedAccessException("Account not found");

        if (!string.IsNullOrWhiteSpace(user.PasswordHash)) throw new InvalidOperationException("This account already has a password.");

        var googleProvider = ExternalLoginProvider.Google.ToString();

        var linkedGoogleAccount = user.ExternalLogins.FirstOrDefault(login =>
            string.Equals(
                login.Provider,
                googleProvider,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (linkedGoogleAccount == null) throw new InvalidOperationException("No Google account is connected");

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                dto.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience =
                        new[]
                        {
                            _googleAuthSettings.ClientId
                        }
                }
            );
        }
        catch
        {
            throw new UnauthorizedAccessException("Google verification failed.");
        }

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Subject)) throw new UnauthorizedAccessException("Google verification failed.");

        if (!string.Equals(
            linkedGoogleAccount.ProviderUserId,
            payload.Subject.Trim(),
            StringComparison.Ordinal
            )
        )
        {
            throw new UnauthorizedAccessException("Please verify using the Google account connected to this profile.");
        }

        if (payload.IssuedAtTimeSeconds is not long issuedAtSeconds) throw new UnauthorizedAccessException("Google verification session is invalid.");

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtSeconds);

        var tokenAge = DateTimeOffset.UtcNow - issuedAt;

        if (tokenAge < TimeSpan.Zero || tokenAge > TimeSpan.FromMinutes(5)) throw new UnauthorizedAccessException("Google verification expired. Please try again.");

        var plainSetUpToken = _passwordResetTokenService.GenerateToken();

        var setUpTokenHash = _passwordResetTokenService.HashToken(plainSetUpToken);

        var now = DateTime.UtcNow;

        var setupSession = new PasswordResetSession
        {
            AppUserId = user.AppUserId,
            TokenHash = setUpTokenHash,

            ExpiresAt = now.AddMinutes(_otpSettings.ResetTokenExpiryMinutes),

            ConsumedAt = null,
            CreatedAt = now,
            CreatedByIp = _authContextAccessor.GetClientIpAddress()
        };
        await _authRepository.CreatePasswordSetupSessionAsync(setupSession);

        return new GooglePasswordReauthResponseDto
        {
            SetupToken = plainSetUpToken,
            ExpiresInSeconds = _otpSettings.ResetTokenExpiryMinutes * 60
        };
    }

    private async Task OnUserRegistered(AppUser user)
    {
        // Resolve the optional hook — if not registered in the host project, skip silently
        var hook = _serviceProvider.GetService<IUserRegisteredHook>();
        if (hook != null)
            await hook.OnUserRegisteredAsync(user);
    }
}