using DevToolkit.Auth.Constants;
using DevToolkit.Infrastructure.Metadata;
using DevToolkit.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DevToolkit.Infrastructure.Middleware;

public sealed class OptionalAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BackendSettings _settings;

    public OptionalAuthenticationMiddleware(RequestDelegate next, IOptions<BackendSettings> options)
    {
        _next = next;
        _settings = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var selected = context.GetEndpoint()?.Metadata.GetMetadata<OptionalAuthenticationAttribute>() != null ||
            _settings.OptionalAuthenticationPaths.Any(prefix => context.Request.Path.StartsWithSegments(prefix));

        if (selected && !string.IsNullOrWhiteSpace(context.Request.Headers.Authorization))
        {
            var authentication = await context.AuthenticateAsync(AuthSchemes.CustomJwt);

            if (!authentication.Succeeded)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await context.Response.WriteAsJsonAsync(new { message = "Your session has ended. Please sign in again." }, context.RequestAborted);
                return;
            }
        }

        await _next(context);
    }
}
