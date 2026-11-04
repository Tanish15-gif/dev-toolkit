using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DevToolkit.Infrastructure.Helpers;
using DevToolkit.Infrastructure.Metadata;
using DevToolkit.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace DevToolkit.Infrastructure.Middleware;

public sealed class SubmissionIdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BackendSettings _settings;

    public SubmissionIdempotencyMiddleware(RequestDelegate next, IOptions<BackendSettings> options)
    {
        _next = next;
        _settings = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, SubmissionIdempotencyStore store)
    {
        var request = context.Request;
        var selected = context.GetEndpoint()?.Metadata.GetMetadata<SubmissionIdempotencyAttribute>() != null ||
            _settings.IdempotencyPaths.Any(value => request.Path.Equals(new PathString(value)));

        if (!selected || !HttpMethods.IsPost(request.Method) ||
            !request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            await _next(context);
            return;
        }

        if (request.Path.StartsWithSegments("/auth/custom"))
        {
            throw new BadHttpRequestException("Authentication responses cannot use submission retry protection.");
        }

        if (!Guid.TryParse(key, out var identifier) || identifier == Guid.Empty)
        {
            throw new BadHttpRequestException("Use a non-empty UUID as the Idempotency-Key.");
        }

        if (!request.HasJsonContentType())
        {
            throw new BadHttpRequestException("Retry protection requires a JSON request.", StatusCodes.Status415UnsupportedMediaType);
        }

        var body = await RequestBodyReader.ReadAsync(request, _settings.IdempotencyMaxRequestBytes, context.RequestAborted);
        var fingerprint = Encoding.UTF8.GetBytes(request.ContentType + "\n" + request.QueryString + "\n");
        using var requestHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        requestHash.AppendData(fingerprint);
        requestHash.AppendData(body);
        var hash = Convert.ToHexString(requestHash.GetHashAndReset());
        var actor = "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("The authenticated user identifier is missing.");
            }

            actor = "user:" + userId;
        }

        var storeKey = actor + ":" + request.Path.Value?.ToUpperInvariant() + ":" + identifier;
        var entry = store.Acquire(storeKey);

        if (entry == null)
        {
            throw new TimeoutException("The submission retry store is full.");
        }

        var ownsGate = false;

        try
        {
            await entry.Gate.WaitAsync(context.RequestAborted);
            ownsGate = true;

            if (entry.RequestHash != null && entry.RequestHash != hash)
            {
                throw new DevToolkit.Auth.Exceptions.ConflictException("This identifier was already used for different request details.");
            }

            if (entry.CannotReplay)
            {
                throw new DevToolkit.Auth.Exceptions.ConflictException("The earlier outcome cannot be replayed safely. Check the existing submission before retrying.");
            }

            context.Response.Headers.CacheControl = "no-store";

            if (entry.Response != null)
            {
                context.Response.StatusCode = entry.StatusCode;
                context.Response.ContentType = entry.ContentType;
                context.Response.Headers["Idempotency-Replayed"] = "true";

                if (entry.Location != null) context.Response.Headers.Location = entry.Location;
                if (entry.ETag != null) context.Response.Headers.ETag = entry.ETag;

                await context.Response.Body.WriteAsync(entry.Response, context.RequestAborted);
                return;
            }

            entry.RequestHash = hash;
            var originalBody = context.Response.Body;
            using var captured = new LimitedMemoryStream(_settings.IdempotencyMaxResponseBytes);
            context.Response.Body = captured;

            try
            {
                await _next(context);
            }
            catch
            {
                // A handler may have committed its side effect before throwing.
                entry.CannotReplay = true;
                throw;
            }
            finally
            {
                context.Response.Body = originalBody;
            }

            var response = captured.ToArray();

            if (context.Response.StatusCode >= 500 || context.Response.Headers.ContainsKey("Set-Cookie") ||
                context.Response.Headers.ContainsKey("Content-Encoding"))
            {
                entry.CannotReplay = true;
            }
            else
            {
                entry.Response = response;
                entry.ContentType = context.Response.ContentType;
                entry.StatusCode = context.Response.StatusCode;
                entry.Location = context.Response.Headers.Location.FirstOrDefault();
                entry.ETag = context.Response.Headers.ETag.FirstOrDefault();
            }

            await originalBody.WriteAsync(response, context.RequestAborted);
        }
        finally
        {
            if (ownsGate)
            {
                entry.Gate.Release();
            }

            store.Release(entry);
        }
    }
}
