# BookingGoldenApple

`BookingGoldenApple` is now split into three ASP.NET Core microservices on `.NET 10`:

- `BGA.Users` - registration, login and JWT issuing
- `BGA.Events` - event catalog and event management
- `BGA.Bookings` - booking creation, booking status and cancellation

Each service owns its own database. In Docker Compose the system runs as a full stack with three PostgreSQL containers, Kafka, Zookeeper, Redis and three APIs.

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

### Runtime ports

| Component | Container | Host port | Purpose |
| --- | --- | --- | --- |
| `BGA.Users.API` | `users-api` | `56514` | registration, login, JWT issuing |
| `BGA.Events.API` | `events-api` | `56515` | event CRUD and seat availability |
| `BGA.Bookings.API` | `bookings-api` | `56516` | booking creation and cancellation |
| `PostgreSQL Users` | `users-db` | `5433` | `bga_users` database |
| `PostgreSQL Events` | `events-db` | `5434` | `bga_events` database |
| `PostgreSQL Bookings` | `bookings-db` | `5435` | `bga_bookings` database |
| `Kafka` | `kafka` | `9092` | inter-service messaging |
| `Redis` | `redis` | `6379` | cache for `BGA.Events` |
| `Zookeeper` | `zookeeper` | not published | Kafka coordination |

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

This is the main scenario for sprint 10: one command starts Kafka, Zookeeper, Redis, three PostgreSQL containers and three APIs.

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
- in full Docker mode connection strings and Redis/Kafka hosts are passed through environment variables from Compose;
- `BGA.Events.API` uses the internal Docker addresses `kafka:29092` and `redis:6379`;
- the locally started `BGA.Events.API` keeps using `localhost:6379`, so the same code works when only infrastructure runs in Docker.

### Run only infrastructure containers

Use this mode if you want Kafka, PostgreSQL and Redis in Docker, but prefer running the APIs locally from the SDK or from Visual Studio.

```powershell
docker compose up -d zookeeper kafka redis users-db events-db bookings-db
```

This starts:

- `zookeeper`
- `kafka`
- `redis`
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
| `redis` | `6379` |

Stop only infrastructure:

```powershell
docker compose stop zookeeper kafka redis users-db events-db bookings-db
```

### Run APIs locally against infrastructure containers

If you use `infra-only`, `appsettings.json` already points to the published Docker ports for PostgreSQL and Redis, so local runs from Visual Studio or `dotnet run` work without extra overrides. Kafka still uses `localhost:9092` locally.

`BGA.Users.API`

```powershell
dotnet run --project src/BGA.Users/BGA.Users.API/BGA.Users.API.csproj
```

`BGA.Events.API`

```powershell
dotnet run --project src/BGA.Events/BGA.Events.API/BGA.Events.API.csproj
```

`BGA.Bookings.API`

```powershell
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

If you want Aspire to start all three APIs while Docker runs only Kafka, PostgreSQL and Redis, use:

```powershell
docker compose up -d zookeeper kafka redis users-db events-db bookings-db
dotnet run --project src/BGA.AppHost/BGA.AppHost.csproj
```

`BGA.AppHost` now passes these values to child services automatically:

- `UsersDb` -> `Host=localhost;Port=5433;Database=bga_users;Username=postgres;Password=postgres`
- `EventsDb` -> `Host=localhost;Port=5434;Database=bga_events;Username=postgres;Password=postgres`
- `BookingsDb` -> `Host=localhost;Port=5435;Database=bga_bookings;Username=postgres;Password=postgres`
- `Kafka` -> `localhost:9092`
- `Redis` for `BGA.Events` -> `localhost:6379` from `appsettings.json`

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

## Caching strategy

`BGA.Events` uses Redis with the `Cache-Aside` pattern for reads and `delete-on-write` invalidation for a single event.

- `GET /events/{id}` caches one event by key `event:{id}`.
- `GET /events/top` caches the public top list by key `events:top10`.
- TTL is configured in `src/BGA.Events/BGA.Events.API/appsettings.json`: `event:{id}` lives for 5 minutes, `events:top10` lives for 10 minutes.
- `event:{id}` is invalidated after successful `Create`, `Update`, `Delete` and seat reservation processing, including the Kafka `BookingConfirmed` flow because it goes through `TryReserveSeatsAsync`.
- `events:top10` is not invalidated on every write and relies only on TTL, because a small delay is acceptable for a ranking view and aggressive invalidation would create unnecessary write pressure.
- Redis failures are logged inside the cache layer and do not fail the client request; the source of truth remains PostgreSQL.

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

## Authentication and authorization

- `BGA.Users` is the only service that issues JWT tokens through `POST /auth/login`.
- `BGA.Events` and `BGA.Bookings` validate the same `Jwt:Key`, `Jwt:Issuer` and `Jwt:Audience`.
- `POST /events`, `PUT /events/{id}` and `DELETE /events/{id}` require the `Admin` role.
- All bookings endpoints require authentication.

## BookingConfirmed Kafka flow

The services do not call each other over HTTP. Seat updates happen only through Kafka:

1. `POST /events/{eventId}/book` creates a booking in `BGA.Bookings` with status `pending`.
2. `BookingProcessingService` confirms pending bookings in the bookings database.
3. After `SaveChangesAsync`, `KafkaBookingConfirmedPublisher` publishes `BookingConfirmed` to topic `booking-confirmed`.
4. The Kafka message key is `EventId`, so confirmations for the same event stay ordered within one partition.
5. `BGA.Events` creates the topic at startup if it does not exist yet.
6. `BookingConfirmedConsumerHostedService` consumes the event in consumer group `bga-events-booking-confirmed`.
7. `BGA.Events` decreases available seats for the matching event and logs invalid, missing or over-capacity messages without crashing the subscriber.

## Booking lifecycle

Booking creation is asynchronous:

1. `POST /events/{eventId}/book` creates a booking with status `pending`
2. `BGA.Bookings` background processing service handles pending bookings
3. in the current sprint-9 flow the background processor moves the booking to `confirmed`
4. `DELETE /bookings/{id}` changes the booking status to `cancelled`
5. the domain model also contains `rejected`, but the current Kafka-based flow does not set it automatically

## Manual end-to-end verification

1. Start the full stack with `docker compose up -d --build`.
2. Register a regular user through `http://localhost:56514/swagger`.
3. Login through `POST /auth/login` and copy the JWT token.
4. Register a second user through `POST /auth/register` with `role = "Admin"`, login as that user, then authorize in `http://localhost:56515/swagger`.
5. Create an event and note its `availableSeats`.
6. Authorize with the regular user token in `http://localhost:56516/swagger`.
7. Call `POST /events/{eventId}/book` and copy the returned booking id.
8. Wait a few seconds for the background processor and Kafka consumer.
9. Check `GET /bookings/{id}` in `BGA.Bookings` and `GET /events/{id}` in `BGA.Events`.
10. The booking should be `confirmed`, and the event should have fewer available seats than before.
