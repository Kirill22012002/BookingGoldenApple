# BookingGoldenApple

## Getting Started

This version of BookingGoldenApple is based on .NET 10.

### Prerequisites
- PostgreSQL (required to run the application). Start from the repository root: `docker compose up -d` (uses `docker-compose.yml`). Stop: `docker compose down`.

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

### Building the solution
```powershell
dotnet build src/BGA.API/BGA.API.csproj
```

### Running the solution
Make sure PostgreSQL is running before starting the API.
The DB schema is created automatically on startup via EF Core `EnsureCreated()`.
```powershell
dotnet run --project src/BGA.API/BGA.API.csproj
```

### Opening Swagger in browser

#### Using PowerShell
1. Run the following command
```powershell
dotnet run --project src/BGA.API/BGA.API.csproj --launch-profile https
```

2. Open the specified url in browser
`
https://localhost:7116/swagger/index.html
`
or
`
http://localhost:5068/swagger/index.html
`

#### Using VisualStudio/Rider
1. Open BookingGoldenApple.slnx in VisualStudio or Rider
2. Run https profile or click F5
3. Open the specified url in browser (if it doesn't open on its own) 
`
https://localhost:7116/swagger/index.html
`
or
`
http://localhost:5068/swagger/index.html
`

### Building and Running the tests
```powershell
dotnet build tests/BGA.API.Tests/BGA.API.Tests.csproj
```

```powershell
dotnet test tests/BGA.API.Tests/BGA.API.Tests.csproj
```

## API Documentation

### Endpoints: 
- `GET`:    /events            - get list of all events
- `GET`:    /events/{id}       - get event by id; if not found returns 404
- `POST`:   /events            - create event
- `PUT`:    /events/{id}       - update event
- `DELETE`: /events/{id}       - remove event; if not found returns 404
- `POST`:   /events/{id}/book  - create booking for event; if event not found returns 404

- `GET`:    /bookings/{id}     - get booking by id; if not found returns 404

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
- status - status of booking processing, can be different (pending, confirmed, rejected)
- createdAt - date of creating booking
- processedAt - date of processing booking

### Models descriptions:

#### BookingStatus can be different
- pending - created, wait for processing
- confirmed - processed and confirmed
- rejected - processed but rejected

### User flows: 

#### Create event => Create booking => Get booking status
- create event using `POST` /events
- create booking using `POST` /events/{id}/book
- check status of booking using `GET` /bookings/{id}
