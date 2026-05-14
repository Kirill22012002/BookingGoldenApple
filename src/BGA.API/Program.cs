using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Application.Services.Implementations;
using BGA.API.Presentation;
using BGA.API.Infrastructure.BackgroundServices;
using BGA.API;
using BGA.API.Presentation.Extensions;
using BGA.API.Presentation.ExceptionHandlers;
using Microsoft.EntityFrameworkCore;
using BGA.API.Infrastructure.DataAccess;
using BGA.API.Infrastructure.DataAccess.Repositories.Implementations;
using BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ApplicationSettingsOptions>()
    .BindConfiguration(ApplicationSettingsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Fallback

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

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddTransient<ProblemDetailsFactory, CustomProblemDetailsFactory>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHostedService<BookingProcessingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.Run();
