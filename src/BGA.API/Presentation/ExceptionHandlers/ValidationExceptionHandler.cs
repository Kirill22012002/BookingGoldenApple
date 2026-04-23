using BGA.API.Application.Exceptions;
using BGA.API.Presentation.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BGA.API.Presentation.ExceptionHandlers;

public class ValidationExceptionHandler(
    ILogger<ValidationExceptionHandler> logger,
    ProblemDetailsFactory problemDetailsFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validation)
        {
            return false;
        }

        logger.LogWarning("Validation failed: {Message}", validation.Message);
        var statusCode = StatusCodes.Status400BadRequest;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetailsFactory.CreateValidationProblemDetails(
            httpContext: context,
            modelStateDictionary: validation.Errors.ToModelStateDictionary(),
            statusCode: statusCode,
            title: "One or more validation errors occured.",
            type: statusCode.GetProblemType()), cancellationToken);

        return true;
    }
}
