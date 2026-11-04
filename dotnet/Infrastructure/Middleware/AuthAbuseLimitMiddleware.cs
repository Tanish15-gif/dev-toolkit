using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using DevToolkit.Infrastructure.Helpers;
using DevToolkit.Infrastructure.RateLimiting;
using DevToolkit.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace DevToolkit.Infrastructure.Middleware;

public sealed class AuthAbuseLimitMiddleware : IMiddleware, IDisposable
{
    private readonly BackendSettings _settings;
    private readonly PartitionedRateLimiter<string> _limiter;

    public AuthAbuseLimitMiddleware
    (
        IOptions<BackendSettings> options
    )
    {
        _settings = options.Value;
        _limiter = PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = key.StartsWith("account:", StringComparison.Ordinal)
                    ? _settings.AuthAccountPermitLimit
                    : _settings.AuthIpPermitLimit,
                Window = TimeSpan.FromSeconds(_settings.AuthWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            })
        );
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!_settings.AuthPathPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
        {
            await next(context);
            return;
        }

        using var ipLease = await _limiter.AcquireAsync(RateLimitKeyHelper.GetIpKey(context), 1, context.RequestAborted);

        if (!ipLease.IsAcquired)
        {
            await RejectAsync(context);
            return;
        }

        if (HttpMethods.IsPost(context.Request.Method) && context.Request.HasJsonContentType())
        {
            var bytes = await RequestBodyReader.ReadAsync(context.Request, _settings.AuthInspectionMaxBytes, context.RequestAborted);
            string? email = null;

            try
            {
                using var json = JsonDocument.Parse(bytes);

                if (json.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in json.RootElement.EnumerateObject())
                    {
                        if (property.Name.Equals("email", StringComparison.OrdinalIgnoreCase))
                        {
                            email = property.Value.ValueKind == JsonValueKind.String
                                ? property.Value.GetString()?.Trim().ToUpperInvariant()
                                : null;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // MVC will return its normal validation response for invalid JSON.
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email)));
                using var accountLease = await _limiter.AcquireAsync("account:" + emailHash, 1, context.RequestAborted);

                if (!accountLease.IsAcquired)
                {
                    await RejectAsync(context);
                    return;
                }
            }
        }

        await next(context);
    }

    private async Task RejectAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = _settings.AuthWindowSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await context.Response.WriteAsJsonAsync(new { message = "Too many attempts. Please wait and try again." }, context.RequestAborted);
    }

    public void Dispose()
    {
        _limiter.Dispose();
    }
}
