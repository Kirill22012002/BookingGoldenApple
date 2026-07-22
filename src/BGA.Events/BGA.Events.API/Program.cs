using BGA.Events.API;
using BGA.Events.Application;
using BGA.Events.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPresentation();
builder.Services.AddObservability(builder.Configuration);

var app = builder.Build();

app.ApplyMigrations();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapPrometheusScrapingEndpoint();
app.MapControllers();
app.Run();
