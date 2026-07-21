# BookingGoldenApple

`BookingGoldenApple` is now split into three ASP.NET Core microservices on `.NET 10`:

- `BGA.Users` - registration, login and JWT issuing
- `BGA.Events` - event catalog and event management
- `BGA.Bookings` - booking creation, booking status and cancellation

Each service owns its own database. For local development all three logical databases live inside one PostgreSQL container.

## Architecture

### Top-level projects

| Project | Responsibility |
| --- | --- |
| `src/BGA.AppHost` | Aspire orchestration. Starts all APIs from one entry point. |
| `src/BGA.Contracts` | Shared contracts placeholder for inter-service contracts. |
| `src/BGA.Users/*` | Users microservice: API, Application, Domain, Infrastructure. |
| `src/BGA.Events/*` | Events microservice: API, Application, Domain, Infrastructure. |
| `src/BGA.Bookings/*` | Bookings microservice: API, Application, Domain, Infrastructure. |

### Service boundaries

| Service | Owns | Database |
| --- | --- | --- |
| `BGA.Users` | users, roles, authentication | `bga_users` |
| `BGA.Events` | events and seats metadata | `bga_events` |
| `BGA.Bookings` | bookings and booking processing | `bga_bookings` |

### Source structure

```text
src/
  BGA.AppHost/
  BGA.Contracts/
  BGA.Users/
    BGA.Users.API/
    BGA.Users.Application/
    BGA.Users.Domain/
    BGA.Users.Infrastructure/
  BGA.Events/
    BGA.Events.API/
    BGA.Events.Application/
    BGA.Events.Domain/
    BGA.Events.Infrastructure/
  BGA.Bookings/
    BGA.Bookings.API/
    BGA.Bookings.Application/
    BGA.Bookings.Domain/
    BGA.Bookings.Infrastructure/
```

### Test structure

```text
tests/
  BGA.Users/
    BGA.Users.API.UnitTests/
    BGA.Users.API.E2ETests/
    BGA.Users.Infrastructure.IntegrationTests/
  BGA.Events/
    BGA.Events.Application.UnitTests/
    BGA.Events.API.E2ETests/
    BGA.Events.Infrastructure.IntegrationTests/
  BGA.Bookings/
    BGA.Bookings.Application.UnitTests/
    BGA.Bookings.API.UnitTests/
    BGA.Bookings.API.E2ETests/
    BGA.Bookings.Infrastructure.IntegrationTests/
```

## Prerequisites

- `.NET 10 SDK`
- `Docker` / `Docker Desktop`

## PostgreSQL for local development

Local setup uses one PostgreSQL container with three logical databases:

- `bga_users`
- `bga_events`
- `bga_bookings`

Start PostgreSQL from the repository root:

```powershell
docker compose up -d
```

Stop it:

```powershell
docker compose down
```

If you previously used the old single-database setup and want a clean local state:

```powershell
docker compose down -v
docker compose up -d
```

Connection strings are already configured in:

- `src/BGA.Users/BGA.Users.API/appsettings.json`
- `src/BGA.Events/BGA.Events.API/appsettings.json`
- `src/BGA.Bookings/BGA.Bookings.API/appsettings.json`

Each API calls `Database.Migrate()` on startup, so pending migrations are applied automatically.

## Running the system

### Run everything through Aspire

This is now the fastest way to start all three APIs together.

1. Start PostgreSQL:

```powershell
docker compose up -d
```

2. Start Aspire AppHost:

```powershell
dotnet run --project src/BGA.AppHost/BGA.AppHost.csproj
```

What happens next:

- Aspire starts `BGA.Users.API`, `BGA.Events.API` and `BGA.Bookings.API`
- the Aspire dashboard opens automatically in the browser
- from the dashboard you can open each service and inspect logs/endpoints

Important:

- right now `Aspire AppHost` orchestrates the three APIs
- PostgreSQL is still started separately via `docker compose`

### Run services individually

PostgreSQL must already be running.

```powershell
dotnet run --project src/BGA.Users/BGA.Users.API/BGA.Users.API.csproj
dotnet run --project src/BGA.Events/BGA.Events.API/BGA.Events.API.csproj
dotnet run --project src/BGA.Bookings/BGA.Bookings.API/BGA.Bookings.API.csproj
```

Swagger opens automatically for each API because `launchSettings.json` uses `launchUrl: swagger`.

Default local URLs:

| Service | HTTPS | HTTP |
| --- | --- | --- |
| `BGA.Users.API` | `https://localhost:56511/swagger` | `http://localhost:56514/swagger` |
| `BGA.Events.API` | `https://localhost:56513/swagger` | `http://localhost:56515/swagger` |
| `BGA.Bookings.API` | `https://localhost:56512/swagger` | `http://localhost:56516/swagger` |

## Building

Build the whole solution:

```powershell
dotnet build BookingGoldenApple.slnx
```

## EF Core migrations

Each service has its own `DbContext` and its own migrations:

- `UsersDbContext`
- `EventsDbContext`
- `BookingsDbContext`

Examples:

### Users

```powershell
dotnet ef migrations add <MigrationName> --project src/BGA.Users/BGA.Users.Infrastructure/BGA.Users.Infrastructure.csproj --startup-project src/BGA.Users/BGA.Users.API/BGA.Users.API.csproj --context UsersDbContext --output-dir Migrations
```

### Events

```powershell
dotnet ef migrations add <MigrationName> --project src/BGA.Events/BGA.Events.Infrastructure/BGA.Events.Infrastructure.csproj --startup-project src/BGA.Events/BGA.Events.API/BGA.Events.API.csproj --context EventsDbContext --output-dir Migrations
```

### Bookings

```powershell
dotnet ef migrations add <MigrationName> --project src/BGA.Bookings/BGA.Bookings.Infrastructure/BGA.Bookings.Infrastructure.csproj --startup-project src/BGA.Bookings/BGA.Bookings.API/BGA.Bookings.API.csproj --context BookingsDbContext --output-dir Migrations
```

## Running tests

Run all tests:

```powershell
dotnet test BookingGoldenApple.slnx
```

Run a specific group:

```powershell
dotnet test tests/BGA.Users/BGA.Users.API.E2ETests/BGA.Users.API.E2ETests.csproj
dotnet test tests/BGA.Events/BGA.Events.Application.UnitTests/BGA.Events.Application.UnitTests.csproj
dotnet test tests/BGA.Bookings/BGA.Bookings.Infrastructure.IntegrationTests/BGA.Bookings.Infrastructure.IntegrationTests.csproj
```

Integration and E2E tests use Docker/Testcontainers, so Docker must be running.

## API overview

### Users API

- `POST /auth/register`
- `POST /auth/login`

### Events API

- `GET /events`
- `GET /events/{id}`
- `POST /events`
- `PUT /events/{id}`
- `DELETE /events/{id}`

### Bookings API

- `POST /events/{eventId}/book`
- `GET /bookings/{id}`
- `DELETE /bookings/{id}`

## Booking lifecycle

Booking creation is asynchronous:

1. `POST /events/{eventId}/book` creates a booking with status `pending`
2. `BGA.Bookings` background processing service handles pending bookings
3. booking status can later become:
   - `pending`
   - `confirmed`
   - `rejected`
   - `cancelled`
