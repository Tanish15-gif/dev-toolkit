using DevToolkit.Infrastructure.Helpers;

namespace DevToolkit.Infrastructure.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware
    (
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "Request failed after its response started. TraceId: {TraceId}", context.TraceIdentifier);
                throw;
            }

            var problem = ApiExceptionResponse.CreateProblem(context, exception);

            if (problem.Status >= 500)
            {
                _logger.LogError(exception, "Request failed. TraceId: {TraceId}", context.TraceIdentifier);
            }

            context.Response.Clear();
            context.Response.StatusCode = problem.Status!.Value;
            context.Response.Headers.CacheControl = "no-store";

            await context.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted
            );
        }
    }
}
