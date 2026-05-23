# Railway Hosting

FolioTrace is prepared for Railway as three services:

- Railway Postgres.
- FolioTrace API, using `API/Dockerfile`.
- FolioTrace UI, using `UI/Dockerfile`.

Local development remains unchanged. The API launch profile still uses `https://localhost:7058`, the UI dev server still uses the local HTTPS certificate scripts, and the UI still falls back to `https://localhost:7058/API` when `API_BASE_URL` is not set.

## API Service

Configure the Railway API service with:

```text
Root Directory: /
Config File Path: /API/railway.json
Dockerfile Path: API/Dockerfile
Start Command: dotnet API.dll --urls http://0.0.0.0:${PORT:-8080}
Healthcheck Path: /API/System/Health
```

The root directory must stay as `/` because `API/Dockerfile` builds from the repository root and needs the sibling `Features` and `Repository` projects. If Railway shows `Railpack could not determine how to build the app` or `Script start.sh not found`, it is not using this service config and is falling back to auto-detection or an old dashboard start command.

Required variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__FolioTrace=<Railway Postgres Npgsql connection string>
```

If `ConnectionStrings__FolioTrace` is not set, the API can fall back to Railway-style `DATABASE_URL` values that use a `postgres://` or `postgresql://` URI.

Optional aggregate maintenance variables:

```text
AggregateMaintenance__Enabled=true
AggregateMaintenance__PeriodicDelay=00:10:00
AggregateMaintenance__EventTriggerCount=100
AggregateMaintenance__EventTriggerDelay=00:00:30
```

The API container binds to Railway's `PORT` on `0.0.0.0`. Railway terminates HTTPS at the edge, so production runs HTTP inside the container. Development still uses HTTPS redirection.

Health check:

```text
/API/System/Health
```

## UI Service

Configure the Railway UI service with:

```text
Root Directory: /
Config File Path: /UI/railway.json
Dockerfile Path: UI/Dockerfile
Start Command: node build/index.js
Healthcheck Path: /
```

The root directory should also stay as `/` for the UI service so the Dockerfile path is resolved from the repository root. The service config file pins Dockerfile builds and overrides any stale dashboard start command such as `start.sh`.

Required variables:

```text
NODE_ENV=production
API_BASE_URL=https://<api-railway-domain>/API
```

The UI container uses the SvelteKit Node adapter and runs `node build/index.js`. Browser SSE requests continue to go through the existing UI route `/API/Notifications/Aggregates`, which proxies to the configured API base URL.

## Smoke Checks

After deployment:

- Open `https://<api-railway-domain>/API/System/Health`.
- Open the UI public domain.
- Confirm dashboard stats load.
- Confirm Build and Clear typed confirmation actions submit.
- Confirm build or aggregate maintenance SSE updates refresh Stats for nerds.
