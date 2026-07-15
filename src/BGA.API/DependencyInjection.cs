using BGA.API.ExceptionHandlers;
using BGA.API.Extensions;
using BGA.Application.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BGA.API;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddOptions<ApplicationSettingsOptions>()
            .BindConfiguration(ApplicationSettingsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>(); // Fallback

        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = factory.CreateValidationProblemDetails(
                        httpContext: context.HttpContext,
                        modelStateDictionary: context.ModelState,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "One or more validation errors occured.",
                        type: StatusCodes.Status400BadRequest.GetProblemType());

                    return new BadRequestObjectResult(problemDetails);
                };
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddTransient<ProblemDetailsFactory, CustomProblemDetailsFactory>();

        return services;
    }
}
