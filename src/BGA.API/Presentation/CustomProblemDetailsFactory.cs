using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BGA.API.Presentation;

public class CustomProblemDetailsFactory : ProblemDetailsFactory
{
    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode ?? StatusCodes.Status500InternalServerError,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance
        };

        Customize(problemDetails, httpContext);

        return problemDetails;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var problemDetails = new ValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode ?? StatusCodes.Status400BadRequest,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance
        };

        Customize(problemDetails, httpContext);

        return problemDetails;
    }

    private static void Customize(ProblemDetails problemDetails, HttpContext httpContext)
    {
        problemDetails.Extensions.TryAdd("traceId", httpContext.TraceIdentifier);
        problemDetails.Extensions.TryAdd("timestamp", DateTimeOffset.UtcNow);
        problemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";
    }
}
