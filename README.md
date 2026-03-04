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