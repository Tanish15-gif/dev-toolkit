using System.ComponentModel.DataAnnotations;
using DevToolkit.Auth.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevToolkit.Infrastructure.Helpers;

public static class ApiExceptionResponse
{
    public static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            BadRequestException or ValidationException => StatusCodes.Status400BadRequest,
            BadHttpRequestException requestException => requestException.StatusCode,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ForbiddenAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ConflictException or DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            TimeoutException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    public static ProblemDetails CreateProblem(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var detail = statusCode >= 500
            ? "The request could not be completed. Please try again later."
            : exception is DbUpdateConcurrencyException
                ? "The resource changed. Reload it before trying again."
                : exception.Message;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode >= 500 ? "Server error." : "Request failed.",
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        return problem;
    }
}
