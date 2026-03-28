using BGA.API.Application.Services.Implementations;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Presentation;
using BGA.API.Infrastructure.Repositories.Interfaces;
using BGA.API.Infrastructure.Repositories.Implementations;
using Microsoft.AspNetCore.Mvc;
using BGA.API.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddControllers()
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<ProblemDetailsFactory, CustomProblemDetailsFactory>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddSingleton<IEventRepository, EventRepository>();

var app = builder.Build();

app.UseExceptionHandler(); // fallback
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.Run();
