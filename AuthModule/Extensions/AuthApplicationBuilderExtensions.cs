using DevToolkit.Auth.Middleware;

namespace DevToolkit.Auth.Extensions;

public static class AuthApplicationBuilderExtensions
{
    /// <summary>Call once, after routing, trusted-proxy handling, and CORS.</summary>
    public static IApplicationBuilder UseDevToolkitAuth(this IApplicationBuilder app)
    {
        app.UseMiddleware<AuthAbuseLimitMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        return app;
    }
}
