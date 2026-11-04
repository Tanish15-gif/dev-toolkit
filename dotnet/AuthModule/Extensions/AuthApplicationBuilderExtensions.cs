using DevToolkit.Infrastructure.Extensions;
using DevToolkit.Infrastructure.Middleware;

namespace DevToolkit.Auth.Extensions;

public static class AuthApplicationBuilderExtensions
{
    /// <summary>Call once, after routing/CORS and trusted-proxy middleware.</summary>
    public static IApplicationBuilder UseDevToolkitAuth(this IApplicationBuilder app)
    {
        app.UseDevToolkitBackend();
        app.UseAuthentication();
        app.UseMiddleware<OptionalAuthenticationMiddleware>();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseMiddleware<SubmissionIdempotencyMiddleware>();

        return app;
    }
}
