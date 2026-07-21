# BookingGoldenApple

`BookingGoldenApple` is now split into three ASP.NET Core microservices on `.NET 10`:

- `BGA.Users` - registration, login and JWT issuing
- `BGA.Events` - event catalog and event management
- `BGA.Bookings` - booking creation, booking status and cancellation

Each service owns its own database. In Docker Compose the system runs as a full stack with three PostgreSQL containers, Kafka, Zookeeper and three APIs.

## Architecture

### Top-level projects

| Project | Responsibility |
| --- | --- |
| `src/BGA.AppHost` | Aspire orchestration. Starts all APIs from one entry point. |
| `src/BGA.Contracts` | Shared inter-service contracts and topic names for Kafka messaging. |
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

## Docker Compose

The root [`docker-compose.yml`](docker-compose.yml) supports two launch modes.

### Run the full application

This is the main scenario for sprint 9: one command starts Kafka, Zookeeper, three PostgreSQL containers and three APIs.

```powershell
docker compose up -d --build
```

Useful follow-up commands:

```powershell
docker compose ps
docker compose logs -f
docker compose down
docker compose down -v
```

After startup the services are available at:

| Service | URL |
| --- | --- |
| `BGA.Users.API` | `http://localhost:56514/swagger` |
| `BGA.Events.API` | `http://localhost:56515/swagger` |
| `BGA.Bookings.API` | `http://localhost:56516/swagger` |
| `Kafka` | `localhost:9092` |

Notes:

- each API calls `Database.Migrate()` on startup, so migrations are applied automatically;
- in full Docker mode connection strings and Kafka host are passed through environment variables from Compose;
- the APIs inside Docker use the internal Kafka address `kafka:29092`.

### Run only infrastructure containers

Use this mode if you want Kafka and PostgreSQL in Docker, but prefer running the APIs locally from the SDK.

```powershell
docker compose up -d zookeeper kafka users-db events-db bookings-db
```

This starts:

- `zookeeper`
- `kafka`
- `users-db`
- `events-db`
- `bookings-db`

Published infrastructure ports:

| Container | Host port |
| --- | --- |
| `users-db` | `5433` |
| `events-db` | `5434` |
| `bookings-db` | `5435` |
| `kafka` | `9092` |

Stop only infrastructure:

```powershell
docker compose stop zookeeper kafka users-db events-db bookings-db
```

### Run APIs locally against infrastructure containers

If you use `infra-only`, override the connection string for each service because local `appsettings.json` still points to `localhost:5432`.

`BGA.Users.API`

```powershell
$env:ConnectionStrings__Default = "Host=localhost;Port=5433;Database=bga_users;Username=postgres;Password=postgres"
dotnet run --project src/BGA.Users/BGA.Users.API/BGA.Users.API.csproj
```

`BGA.Events.API`

```powershell
$env:ConnectionStrings__Default = "Host=localhost;Port=5434;Database=bga_events;Username=postgres;Password=postgres"
$env:Kafka__BootstrapServers = "localhost:9092"
dotnet run --project src/BGA.Events/BGA.Events.API/BGA.Events.API.csproj
```

`BGA.Bookings.API`

```powershell
$env:ConnectionStrings__Default = "Host=localhost;Port=5435;Database=bga_bookings;Username=postgres;Password=postgres"
$env:Kafka__BootstrapServers = "localhost:9092"
dotnet run --project src/BGA.Bookings/BGA.Bookings.API/BGA.Bookings.API.csproj
```

Swagger opens automatically for local runs because `launchSettings.json` uses `launchUrl: swagger`.

Default local URLs:

| Service | HTTPS | HTTP |
| --- | --- | --- |
| `BGA.Users.API` | `https://localhost:56511/swagger` | `http://localhost:56514/swagger` |
| `BGA.Events.API` | `https://localhost:56513/swagger` | `http://localhost:56515/swagger` |
| `BGA.Bookings.API` | `https://localhost:56512/swagger` | `http://localhost:56516/swagger` |

### Run AppHost against infrastructure containers

If you want Aspire to start all three APIs while Docker runs only Kafka and PostgreSQL, use:

```powershell
docker compose up -d zookeeper kafka users-db events-db bookings-db
dotnet run --project src/BGA.AppHost/BGA.AppHost.csproj
```

`BGA.AppHost` now passes these values to child services automatically:

- `UsersDb` -> `Host=localhost;Port=5433;Database=bga_users;Username=postgres;Password=postgres`
- `EventsDb` -> `Host=localhost;Port=5434;Database=bga_events;Username=postgres;Password=postgres`
- `BookingsDb` -> `Host=localhost;Port=5435;Database=bga_bookings;Username=postgres;Password=postgres`
- `Kafka` -> `localhost:9092`

If needed, you can override them through `src/BGA.AppHost/appsettings.json` or user secrets/environment variables for `BGA.AppHost`.

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
