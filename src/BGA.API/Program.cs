using BGA.API.Infrastructure.Repositories;
using BGA.API.Application.Services.Implementations;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors.Select(e => e.ErrorMessage)
                )
                .Select(kv => $"{kv.Key}: {kv.Value}");

            var errorsString = string.Join(",", errors);

            var apiResult = new ApiResult
            {
                Success = false,
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = $"Validation error: {errorsString}"
            };


            return null;
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddSingleton<EventRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.Run();
