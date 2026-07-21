using BGA.Bookings.API.Extensions;
using BGA.Bookings.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BGA.Bookings.API.ExceptionHandlers;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    ProblemDetailsFactory problemDetailsFactory,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.Headers["x-request-id"]);

        var (statusCode, title) = MapException(exception);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            httpContext: context,
            statusCode: statusCode,
            title: title,
            type: statusCode.GetProblemType(),
            detail: GetSafeErrorMessage(exception, context));

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails
        });
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        AppException appException => ((int)appException.StatusCode, appException.Message),
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument provided."),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized."),
        OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Request cancelled by client."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };

    private static string? GetSafeErrorMessage(Exception exception, HttpContext context)
    {
        var env = context.RequestServices.GetRequiredService<IHostEnvironment>();
        if (env.IsDevelopment())
        {
            return exception.Message;
        }

        return exception is AppException ? exception.Message : null;
    }
}
