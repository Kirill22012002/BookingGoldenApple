# BookingGoldenApple

## Getting Started

This version of BookingGoldenApple is based on .NET 10.

### Building the solution
```powershell
dotnet build src/BGA.API/BGA.API.csproj
```

### Running the solution
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
- `GET`:    /events       - get list of all events
- `GET`:    /events/{id}  - get event by id; if not found returns 404
- `POST`:   /events       - create event
- `PUT`:    /events/{id}  - update event
- `DELETE`: /events/{id}  - remove event; if not found returns 404

#### `GET`: /event has the following filters and pagination parameters. All filters work together (logical AND)
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
