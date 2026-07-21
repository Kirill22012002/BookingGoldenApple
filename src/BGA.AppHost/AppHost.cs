var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BGA_Users_API>("bga-users-api");
builder.AddProject<Projects.BGA_Events_API>("bga-events-api");
builder.AddProject<Projects.BGA_Bookings_API>("bga-bookings-api");

builder.Build().Run();
