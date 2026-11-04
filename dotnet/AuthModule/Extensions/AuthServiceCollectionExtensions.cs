using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using DevToolkit.Auth.Authorization;
using DevToolkit.Auth.Constants;
using DevToolkit.Auth.Controllers;
using DevToolkit.Auth.Data;
using DevToolkit.Auth.Filters;
using DevToolkit.Auth.Interfaces;
using DevToolkit.Auth.Interfaces.Repositories;
using DevToolkit.Auth.Interfaces.Services;
using DevToolkit.Auth.Repositories;
using DevToolkit.Auth.Services;
using DevToolkit.Auth.Services.Context;
using DevToolkit.Auth.Settings;
using DevToolkit.Infrastructure.Email;
using DevToolkit.Infrastructure.Extensions;
using DevToolkit.Infrastructure.Settings;
using DevToolkit.Infrastructure.HealthChecks;
using DevToolkit.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace DevToolkit.Auth.Extensions;

public static class AuthServiceCollectionExtensions
{
    /// <summary>Registers a ready-to-use auth context using the host's database provider.</summary>
    public static IServiceCollection AddDevToolkitAuth
    (
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> configureDatabase
    )
    {
        services.AddDbContext<AuthDbContext>(configureDatabase);

        return services.AddDevToolkitAuth<AuthDbContext>(configuration);
    }

    /// <summary>Uses a host context inheriting AuthDbContext or implementing IAuthDbContext.</summary>
    public static IServiceCollection AddDevToolkitAuth<TContext>
    (
        this IServiceCollection services,
        IConfiguration configuration
    )
        where TContext : DbContext, IAuthDbContext
    {
        services.AddScoped<IAuthDbContext>(provider => provider.GetRequiredService<TContext>());

        return services.AddJwtAuthModule(configuration);
    }

    /// <summary>Advanced entry point when IAuthDbContext is already registered by the host.</summary>
    public static IServiceCollection AddJwtAuthModule
    (
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        if (jwtSettings == null || !jwtSettings.IsValid())
        {
            throw new InvalidOperationException(
                "Configure JwtSettings with a secret of at least 32 UTF-8 bytes, issuer, audience, and positive token lifetimes."
            );
        }

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("JwtSettings"))
            .Validate(settings => settings.IsValid(), "JwtSettings are invalid.")
            .ValidateOnStart();

        services.AddOptions<OtpSettings>()
            .Bind(configuration.GetSection(OtpSettings.SectionName))
            .Validate(settings => settings.IsValid(), "Otp requires positive lifetimes and a Base64 key of at least 32 bytes.")
            .ValidateOnStart();

        services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuth"));
        services.Configure<AppInfoSettings>(configuration.GetSection(AppInfoSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddDevToolkitBackend(configuration);
        services.AddHttpContextAccessor();
        services.AddControllers().AddApplicationPart(typeof(CustomAuthController).Assembly);
        services.TryAddScoped<AuthExceptionFilter>();
        services.TryAddScoped<IEmailService, SmtpEmailService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = AuthSchemes.CustomJwt;
            options.DefaultChallengeScheme = AuthSchemes.CustomJwt;
        })
        .AddJwtBearer(AuthSchemes.CustomJwt, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var sessionIdValue = context.Principal?.FindFirstValue("sid");

                    if (!int.TryParse(userIdValue, out var appUserId) ||
                        !Guid.TryParse(sessionIdValue, out var sessionId))
                    {
                        context.Fail("Invalid authentication session.");
                        return;
                    }

                    var repository = context.HttpContext.RequestServices.GetRequiredService<ISessionRepository>();
                    var active = await repository.IsSessionActiveAsync(
                        sessionId,
                        appUserId,
                        context.HttpContext.RequestAborted
                    );

                    if (!active)
                    {
                        context.Fail("This session is no longer active.");
                    }
                }
            };
        });

        services.AddScoped<IAuthorizationHandler, AppRoleAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.AdminOnly, policy =>
            {
                policy.AuthenticationSchemes.Add(AuthSchemes.CustomJwt);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AppRoleRequirement(RoleNames.Admin, RoleNames.Owner));
            });

            options.AddPolicy(AppPolicies.UserOnly, policy =>
            {
                policy.AuthenticationSchemes.Add(AuthSchemes.CustomJwt);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AppRoleRequirement(RoleNames.User));
            });

            options.AddPolicy(AppPolicies.AdminOrUser, policy =>
            {
                policy.AuthenticationSchemes.Add(AuthSchemes.CustomJwt);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AppRoleRequirement(RoleNames.User, RoleNames.Admin, RoleNames.Owner));
            });
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddRateLimit(options, RateLimitPolicies.AuthAttempt, 5, TimeSpan.FromMinutes(5));
            AddRateLimit(options, RateLimitPolicies.OtpRequest, 3, TimeSpan.FromMinutes(10));
            AddRateLimit(options, RateLimitPolicies.OtpVerify, 10, TimeSpan.FromMinutes(5));
            AddRateLimit(options, RateLimitPolicies.PublicRead, 120, TimeSpan.FromMinutes(1));
            AddRateLimit(options, RateLimitPolicies.FormSubmit, 10, TimeSpan.FromMinutes(1));
            AddRateLimit(options, RateLimitPolicies.ImageUpload, 8, TimeSpan.FromMinutes(5));
            AddRateLimit(options, RateLimitPolicies.AdminAction, 80, TimeSpan.FromMinutes(1));
            AddRateLimit(options, RateLimitPolicies.ChatMessage, 15, TimeSpan.FromMinutes(1));
        });

        services.TryAddScoped<IPasswordHasherService, PasswordHasherService>();
        services.TryAddScoped<IJwtTokenService, JwtTokenService>();
        services.TryAddScoped<IAuthContextAccessor, AuthContextAccessor>();
        services.TryAddScoped<IAuthEventLogger, AuthEventLogger>();
        services.TryAddScoped<ICredentialAuthRepository, CredentialAuthRepository>();
        services.TryAddScoped<ITokenRepository, TokenRepository>();
        services.TryAddScoped<IGoogleAuthRepository, GoogleAuthRepository>();
        services.TryAddScoped<IPasswordRecoveryRepository, PasswordRecoveryRepository>();
        services.TryAddScoped<ISessionRepository, SessionRepository>();
        services.TryAddScoped<ICustomAuthRepository, CustomAuthRepository>();
        services.TryAddScoped<ICredentialAuthService, CredentialAuthService>();
        services.TryAddScoped<ITokenService, TokenService>();
        services.TryAddScoped<IGoogleAuthService, GoogleAuthService>();
        services.TryAddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
        services.TryAddScoped<ISessionService, SessionService>();
        services.TryAddScoped<ICustomAuthService, CustomAuthService>();
        services.TryAddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
        services.TryAddSingleton<IUserAgentParser, UserAgentParser>();
        services.TryAddSingleton<IOtpSecurityService, OtpSecurityService>();
        services.TryAddSingleton<IPasswordResetTokenService, PasswordResetTokenService>();
        services.AddAuthModuleHealthChecks(configuration);

        return services;
    }

    public static IServiceCollection AddAuthModuleHealthChecks(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var timeoutSeconds = configuration?.GetValue<int?>("Backend:HealthCheckTimeoutSeconds") ?? 5;

        services.AddHealthChecks().AddCheck<SessionStoreHealthCheck>(
            "auth-session-store",
            tags: ["ready", "database"],
            timeout: TimeSpan.FromSeconds(timeoutSeconds)
        );

        return services;
    }

    private static void AddRateLimit
    (
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        TimeSpan window
    )
    {
        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                RateLimitKeyHelper.GetUserOrIpKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }
            )
        );
    }
}
