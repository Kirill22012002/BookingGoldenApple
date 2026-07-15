# BookingGoldenApple

## Getting Started

This version of BookingGoldenApple is based on .NET 10.

### Project structure
The solution is split into four layers under `src`.

| Project                                            | Layer                                | Responsibility                                                                                                                               |
| -------------------------------------------------- | ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/BGA.API/BGA.API.csproj`                       | Presentation and startup application | ASP.NET Core entry point, controllers, DTOs, request validation attributes, exception handling, Swagger and composition of all layers.       |
| `src/BGA.Application/BGA.Application.csproj`       | Application                          | Use cases, application services, repository interfaces, unit of work interface, background processing and application settings.              |
| `src/BGA.Domain/BGA.Domain.csproj`                 | Domain                               | Core business models, enums and domain exceptions. This layer does not reference any other project.                                          |
| `src/BGA.Infrastructure/BGA.Infrastructure.csproj` | Infrastructure                       | EF Core `DbContext`, entity configurations, migrations, repository implementations, unit of work implementation and PostgreSQL registration. |

Project references must follow this dependency direction:

```text
BGA.API (Presentation/startup)
    -> BGA.Application
    -> BGA.Infrastructure

BGA.Infrastructure -> BGA.Application
BGA.Infrastructure -> BGA.Domain

BGA.Application    -> BGA.Domain

BGA.Domain         -> no project references
```

The same structure can be read as a layer diagram:

```text
                 Presentation / BGA.API
                   |             |
                   v             v
Application / BGA.Application <- Infrastructure / BGA.Infrastructure
                   |             |
                   v             v
                 Domain / BGA.Domain
```

Keep business rules in `BGA.Domain` or `BGA.Application`. `BGA.API` should translate HTTP requests and responses, while `BGA.Infrastructure` should contain persistence and external implementation details.

### Test structure
Tests are split by the project they verify. A test project should reference only its target project.

```text
tests/BGA.API.UnitTests                 -> src/BGA.API
tests/BGA.Application.UnitTests         -> src/BGA.Application
tests/BGA.Infrastructure.IntegrationTests -> src/BGA.Infrastructure
```

`BGA.Infrastructure.IntegrationTests` uses Testcontainers to start PostgreSQL in Docker, so Docker must be installed and running before starting these tests.

### Prerequisites
- PostgreSQL is required to run the application. Start it from the repository root with `docker compose up -d` (uses `docker-compose.yml`). Stop it with `docker compose down`.
- Docker is required for infrastructure integration tests.

### Configure connection string
Update the PostgreSQL connection string in `src/BGA.API/appsettings.json` under `ConnectionStrings:Default`.
Defaults match `docker-compose.yml` (`postgres`/`postgres`, DB: `bgaapi`, Port: `5432`).

Connection string format (Npgsql):
`Host=<host>;Port=<port>;Database=<db>;Username=<user>;Password=<password>`

Parameters:
| Key | Description | Example |
| --- | --- | --- |
| `Host` | PostgreSQL server address | `localhost` |
| `Port` | PostgreSQL server port | `5432` |
| `Database` | Database name | `bgaapi` |
| `Username` | DB user | `postgres` |
| `Password` | DB user password | `postgres` |

### Description of src/BGA.API/appsettings.json settings
- AppSettings__PoolingIntervalSec - (int), from 0 seconds to 2147483647 seconds, this is the interval between attempts to request bookings with pending status and process them.
- AppSettings__ProcessingDelaySec - (int), from 0 seconds to 2147483647 seconds, this is an artificial delay that simulates a request to a remote service.
- Jwt__Key - secret key used to sign JWT tokens. Replace the development value with a long random secret in production and do not store it directly in source control.
- Jwt__Issuer - token issuer value validated by JWT middleware.
- Jwt__Audience - token audience value validated by JWT middleware.
- Jwt__ExpirationMinutes - token lifetime in minutes.

### Building the solution
```powershell
dotnet build BookingGoldenApple.slnx
```

You can also build only the startup application:

```powershell
dotnet build src/BGA.API/BGA.API.csproj
```

### Running the solution
Make sure PostgreSQL is running before starting the API.
The database schema is managed by EF Core migrations. On application startup the API applies pending migrations automatically via `Database.Migrate()`.

```powershell
dotnet run --project src/BGA.API/BGA.API.csproj
```

### EF Core migrations
Migrations belong to the Infrastructure layer because `ApplicationDbContext` and persistence mappings live in `src/BGA.Infrastructure`.
Run EF Core commands from the repository root and use:

- `--project src/BGA.Infrastructure/BGA.Infrastructure.csproj` for the project where migration files are created.
- `--startup-project src/BGA.API/BGA.API.csproj` for the executable project that provides configuration and dependency injection.

Create a new migration:

```powershell
dotnet ef migrations add <MigrationName> --project src/BGA.Infrastructure/BGA.Infrastructure.csproj --startup-project src/BGA.API/BGA.API.csproj --context ApplicationDbContext --output-dir Migrations
```

Apply migrations to the configured database:

```powershell
dotnet ef database update --project src/BGA.Infrastructure/BGA.Infrastructure.csproj --startup-project src/BGA.API/BGA.API.csproj --context ApplicationDbContext
```

### Opening Swagger in browser

#### Using PowerShell
1. Run the following command:

```powershell
dotnet run --project src/BGA.API/BGA.API.csproj --launch-profile https
```

2. Open one of these URLs in a browser:

```text
https://localhost:7116/swagger/index.html
http://localhost:5068/swagger/index.html
```

#### Using VisualStudio/Rider
1. Open `BookingGoldenApple.slnx` in VisualStudio or Rider.
2. Run the `https` profile or click F5.
3. Open one of the Swagger URLs above if it does not open automatically.

### Using JWT in Swagger
1. Register a user with `POST /auth/register`.
2. Call `POST /auth/login` with the same credentials and copy the returned token.
3. Click `Authorize` in Swagger.
4. Paste only the raw JWT token and confirm. Swagger UI will add the `Bearer` prefix automatically.
5. Call protected endpoints with the authorized session.

### Running tests
Run all tests:

```powershell
dotnet test BookingGoldenApple.slnx
```

Run API unit tests:

```powershell
dotnet test tests/BGA.API.UnitTests/BGA.API.UnitTests.csproj
```

Run Application unit tests:

```powershell
dotnet test tests/BGA.Application.UnitTests/BGA.Application.UnitTests.csproj
```

Run Infrastructure integration tests:

```powershell
dotnet test tests/BGA.Infrastructure.IntegrationTests/BGA.Infrastructure.IntegrationTests.csproj
```

## API Documentation

### Role model
- `User` can register, log in, create bookings for future events, view a booking by id and cancel only their own bookings.
- `Admin` has all `User` permissions and can also create, update and delete events, plus cancel any booking.
- Anonymous users can read events and use authentication endpoints only.

### Endpoints
- `POST`:   /auth/register     - register a new user; role is optional and defaults to `User`
- `POST`:   /auth/login        - log in and receive a JWT token
- `GET`:    /events            - get list of all events
- `GET`:    /events/{id}       - get event by id; if not found returns 404
- `POST`:   /events            - create event; requires `Admin`
- `PUT`:    /events/{id}       - update event; requires `Admin`
- `DELETE`: /events/{id}       - remove event; requires `Admin`; if not found returns 404
- `POST`:   /events/{id}/book  - create booking for event; requires authentication
- `GET`:    /bookings/{id}     - get booking by id; requires authentication
- `DELETE`: /bookings/{id}     - cancel booking; requires authentication

#### `GET`: /events has the following filters and pagination parameters. All filters work together (logical AND)
- title - optional, search by name, case-insensitive, partial match.
- from - optional, events that begin no earlier than the specified date.
- to - optional, events that end no later than the specified date.
- page - optional, with default value: 1, the page to return
- pageSize - optional, with default value: 10, the number of elements on a page

#### `GET`: /events returns the following result
- items - the result of pagination and filtering
- totalItems - the total number of events
- pageNumber - the current page number
- pageSize - the number of elements on the current page

#### `POST` /events/{id}/book immediately returns the following result
- id - id of booking
- eventId - id of event
- status - current status of booking
And in location you can find URL for getting booking

#### `GET` /bookings/{id} returns the following result
- id - id of booking
- eventId - id of event
- status - status of booking processing, can be different (pending, confirmed, rejected, cancelled)
- createdAt - date of creating booking
- processedAt - date of processing booking

### Models descriptions:

#### BookingStatus can be different
- pending - created, wait for processing
- confirmed - processed and confirmed
- rejected - processed but rejected
- cancelled - cancelled by the booking owner or admin

### Authorization rules and errors
- `POST /events`, `PUT /events/{id}` and `DELETE /events/{id}` return `401` without a token and `403` for authenticated non-admin users.
- `POST /events/{id}/book`, `GET /bookings/{id}` and `DELETE /bookings/{id}` require a valid JWT token.
- Booking an event that has already started returns `400`.
- Creating more than 10 active bookings for the same user returns `409`.
- Cancelling another user's booking without the `Admin` role returns `403`.

### User flows: 

#### Create event => Create booking => Get booking status
- create event using `POST /events` as an admin
- create booking using `POST /events/{id}/book` as an authenticated user
- check status of booking using `GET` /bookings/{id}
