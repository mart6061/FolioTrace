# FolioTrace API

## Local database configuration

The API does not keep database credentials in tracked configuration. Configure the local PostgreSQL connection string once with .NET user secrets from the `API` directory:

```powershell
dotnet user-secrets set "ConnectionStrings:FolioTrace" "Host=127.0.0.1;Port=5432;Database=FolioTrace;Username=FolioTrace;Password=<password>"
```

The project already declares its `UserSecretsId`, so the value is loaded automatically in Development. Deployment environments can instead provide `ConnectionStrings__FolioTrace` or `DATABASE_URL`. When both are present, `ConnectionStrings:FolioTrace` takes precedence.

Start the API from the solution root with:

```powershell
dotnet run --project API/API.csproj --launch-profile https
```
