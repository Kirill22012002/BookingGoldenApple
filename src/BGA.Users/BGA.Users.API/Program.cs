using BGA.Users.API;
using BGA.Users.Application;
using BGA.Users.Infrastructure;

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
