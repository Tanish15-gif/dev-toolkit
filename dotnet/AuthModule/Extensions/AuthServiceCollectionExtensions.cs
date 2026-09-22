using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

using AuthModule.Authorization;
using AuthModule.Constants;
using AuthModule.Interfaces;
using AuthModule.Interfaces.Repositories;
using AuthModule.Interfaces.Services;
using AuthModule.Repositories;
using AuthModule.Services;
using AuthModule.Services.Context;
using AuthModule.Services.Helpers;
using AuthModule.Settings;
using DevToolkit.Infrastructure.HealthChecks;
using DevToolkit.Infrastructure.RateLimiting;

namespace AuthModule.Extensions;

public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Wires up the entire auth module: JWT bearer auth (with live session
    /// revocation check), Google/OTP settings, RBAC policies, auth-specific
    /// rate limits, and every auth service/repository registration.
    /// Requires the host project to have already registered its own
    /// DbContext (via AddDbContext) and an ISessionRepository implementation
    /// backed by that DbContext.
    /// </summary>
    public static IServiceCollection AddJwtAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---- Settings binding ----
        services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuth"));
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            throw new InvalidOperationException("Jwt settings are missing");

        services.AddOptions<OtpSettings>()
            .Bind(configuration.GetSection(OtpSettings.SectionName))
            .Validate(
                s => s.ExpiryMinutes > 0 && s.ResendCooldownSeconds > 0 &&
                     s.MaxAttempts > 0 && !string.IsNullOrWhiteSpace(s.HashKey),
                "OTP configuration is incomplete.")
            .ValidateOnStart();

        // ---- JWT bearer authentication ----
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthSchemes.CustomJwt;
                options.DefaultChallengeScheme = AuthSchemes.CustomJwt;
                options.DefaultScheme = AuthSchemes.CustomJwt;
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
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    // SignalR hub connections pass the token via query string
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    },

                    // Portable version: goes through ISessionRepository instead
                    // of a concrete DbContext, so this works against ANY
                    // project's data layer as long as it implements the interface.
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

                        var sessionRepository = context.HttpContext.RequestServices
                            .GetRequiredService<ISessionRepository>();

                        var sessionIsActive = await sessionRepository.IsSessionActiveAsync(sessionId, appUserId);

                        if (!sessionIsActive)
                            context.Fail("This session has been revoked.");
                    }
                };
            });

        // ---- RBAC ----
        services.AddScoped<IAuthorizationHandler, AppRoleAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.AdminOnly, policy =>
            {
                policy.AuthenticationSchemes.Add(AuthSchemes.CustomJwt);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AppRoleRequirement(RoleNames.Admin, RoleNames.Owner));
            });
            options.AddPolicy(AppPolicies.CustomerOnly, policy =>
            {
                policy.AuthenticationSchemes.Add(AuthSchemes.CustomJwt);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AppRoleRequirement(RoleNames.Customer));
            });
            options.AddPolicy(AppPolicies.AdminOrCustomer, policy =>
            {
                policy.AuthenticationSchemes.Add(AuthSchemes.CustomJwt);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AppRoleRequirement(RoleNames.Admin, RoleNames.Owner, RoleNames.Customer));
            });
        });

        // ---- Auth-specific rate limits ----
        // Safe to call AddRateLimiter here even though the host project also
        // calls it for its own policies (PublicRead, FormSubmit, etc.) —
        // ASP.NET Core merges every AddRateLimiter configuration action
        // against the same RateLimiterOptions instance.
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(RateLimitPolicies.AuthAttempt, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitKeyHelper.GetUserOrIpKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(RateLimitPolicies.OtpRequest, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitKeyHelper.GetUserOrIpKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(10),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(RateLimitPolicies.OtpVerify, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitKeyHelper.GetUserOrIpKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        // ---- DI: auth services, repositories, singletons ----
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IAuthContextAccessor, AuthContextAccessor>();
        services.AddScoped<IAuthEventLogger, AuthEventLogger>();

        services.AddScoped<ICredentialAuthRepository, CredentialAuthRepository>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<IGoogleAuthRepository, GoogleAuthRepository>();
        services.AddScoped<IPasswordRecoveryRepository, PasswordRecoveryRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ICustomAuthRepository, CustomAuthRepository>();

        services.AddScoped<ICredentialAuthService, CredentialAuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ICustomAuthService, CustomAuthService>();

        services.AddSingleton<IUserAgentParser, UserAgentParser>();
        services.AddSingleton<IOtpSecurityService, OtpSecurityService>();
        services.AddSingleton<IPasswordResetTokenService, PasswordResetTokenService>();

        return services;
    }

    /// <summary>
    /// Registers the auth module's session-store health check.
    /// Call this on the IServiceCollection after the host project has
    /// already called AddHealthChecks() for its own checks.
    /// </summary>
    public static IServiceCollection AddAuthModuleHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<SessionStoreHealthCheck>("session-store", tags: new[] { "ready", "database" });

        return services;
    }
}