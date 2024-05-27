# Migrations
0. Open terminal (e.g. Visual Studio -> View -> Terminal)

1. Install dotnet-ef tool
```bash
dotnet tool install --global dotnet-ef
```

2. Add migration
```bash
dotnet ef migrations add AddPartyAndElection --project .\Scalection.ApiService\Scalection.ApiService.csproj
```

3. Migrations get executed during application startup by Scalection.MigrationService