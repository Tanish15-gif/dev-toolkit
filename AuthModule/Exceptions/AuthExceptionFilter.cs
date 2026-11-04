using DevToolkit.Auth.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevToolkit.Auth.Exceptions;

public class AuthExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var statusCode = ApiExceptionResponse.GetStatusCode(context.Exception);

        if (statusCode >= 500)
        {
            return;
        }

        context.Result = new ObjectResult(ApiExceptionResponse.CreateProblem(context.HttpContext, context.Exception))
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }
}
