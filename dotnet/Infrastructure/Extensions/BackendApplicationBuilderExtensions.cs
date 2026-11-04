using DevToolkit.Infrastructure.Middleware;
using DevToolkit.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace DevToolkit.Infrastructure.Extensions;

public static class BackendApplicationBuilderExtensions
{
    /// <summary>Call after routing and trusted-proxy middleware, before authentication.</summary>
    public static IApplicationBuilder UseDevToolkitBackend(this IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetRequiredService<IOptions<BackendSettings>>().Value;

        if (settings.EnableExceptionHandling)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
        }

        if (settings.EnableAuthAbuseProtection)
        {
            app.UseMiddleware<AuthAbuseLimitMiddleware>();
        }

        return app;
    }
}
