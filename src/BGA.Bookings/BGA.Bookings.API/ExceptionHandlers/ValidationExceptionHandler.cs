using BGA.Bookings.API.Extensions;
using BGA.Bookings.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BGA.Bookings.API.ExceptionHandlers;

public sealed class ValidationExceptionHandler(
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
