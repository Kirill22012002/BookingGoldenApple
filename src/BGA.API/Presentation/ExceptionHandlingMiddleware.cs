using BGA.API.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BGA.API.Presentation;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
                context.Request.Method,
                context.Request.Path,
                context.Request.Headers["x-request-id"]);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted) return;
        context.Response.ContentType = "application/json";
        var statusCode = MapException(exception);
        context.Response.StatusCode = statusCode;

        var factory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problemDetails = factory.CreateProblemDetails(
            httpContext: context,
            statusCode: statusCode,
            title: "An error occured",
            type: statusCode.GetProblemType(),
            detail: exception.Message);

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static int MapException(Exception exception) => exception switch
    {
        KeyNotFoundException => StatusCodes.Status404NotFound,
        ArgumentException => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
}
