# Railway Hosting

FolioTrace is prepared for Railway as four services:

- Railway Postgres.
- FolioTrace API, using `API/Dockerfile`.
- FolioTrace UI, using `UI/Dockerfile`.
- FoleoTrader, using `FoleoTrader/Dockerfile`.
- GitHub Actions CI, using `.github/workflows/ci.yml`, to validate the .NET solution, Svelte UI, and API/UI Railway Docker images before changes are merged.

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

The API service config includes watch patterns for `API/**`, `Features/**`, `Repository/**`, `FolioTrace.slnx`, and `.dockerignore`. Changes outside those paths should not trigger an API deployment.

Required variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__FolioTrace=<Railway Postgres Npgsql connection string>
FoleoTrader__Host=<foleotrader-railway-private-host>
FoleoTrader__Port=9878
```

Keep `API/appsettings.json` secret-free. Production database credentials should be configured as Railway service variables or GitHub environment secrets, not committed to source control.

Use one of these database settings:

```text
ConnectionStrings__FolioTrace=Host=<host>;Port=<port>;Database=<database>;Username=<user>;Password=<password>
DATABASE_URL=postgresql://<user>:<password>@<host>:<port>/<database>?sslmode=require
```

`ConnectionStrings__FolioTrace` takes precedence. If it is not set, the API can fall back to Railway-style `DATABASE_URL` values that use a `postgres://` or `postgresql://` URI. When `DATABASE_URL` includes `sslmode=require`, the API enables required SSL and trusts the server certificate for the generated Npgsql connection string.

Optional aggregate maintenance variables:

```text
AggregateMaintenance__Enabled=true
AggregateMaintenance__PeriodicDelay=00:10:00
AggregateMaintenance__EventTriggerCount=100
AggregateMaintenance__EventTriggerDelay=00:00:30
```

The API container binds to Railway's `PORT` on `0.0.0.0`. Railway terminates HTTPS at the edge, so production runs HTTP inside the container. Development still uses HTTPS redirection.

The API sends trading orders to FoleoTrader over FIX. `FoleoTrader__Host` must point at the FoleoTrader Railway service host, not `127.0.0.1`, because the API and FoleoTrader run in separate containers. `FoleoTrader__Port` should match the FoleoTrader FIX acceptor port, which defaults to `9878`.

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
Healthcheck Path: /health
```

The root directory should also stay as `/` for the UI service so the Dockerfile path is resolved from the repository root. The service config file pins Dockerfile builds and overrides any stale dashboard start command such as `start.sh`.

The UI service config includes watch patterns for `UI/**` and `.dockerignore`. Changes outside those paths should not trigger a UI deployment.

The UI healthcheck path is `/health` instead of `/` because the root route is protected by WorkOS and returns an authentication redirect for anonymous requests. Railway healthchecks should receive a plain `200 ok` from `/health`.

Required variables:

```text
NODE_ENV=production
API_BASE_URL=https://<api-railway-domain>/API
ORIGIN=https://<ui-railway-or-custom-domain>
WORKOS_CLIENT_ID=<workos-client-id>
WORKOS_API_KEY=<workos-api-key>
WORKOS_COOKIE_PASSWORD=<at-least-32-characters>
WORKOS_REDIRECT_URI=https://<ui-railway-or-custom-domain>/callback
```

The UI container uses the SvelteKit Node adapter and runs `node build/index.js`. Browser SSE requests continue to go through the existing UI route `/API/Notifications/Aggregates`, which proxies to the configured API base URL.

`ORIGIN` and `WORKOS_REDIRECT_URI` must use the same UI host. AuthKit stores the OAuth PKCE verifier in a host-scoped browser cookie before redirecting to WorkOS, and WorkOS later sends the browser back to `WORKOS_REDIRECT_URI`. If sign-in starts on a different Railway/custom host than the callback host, the callback cannot see the PKCE cookie and authentication fails with `PKCECookieMissingError`.

The UI redirects protected pages to `/sign-in` first. That endpoint then generates the WorkOS sign-in URL and returns the external redirect so SvelteKit can attach the PKCE `Set-Cookie` header before the browser leaves the app. If `PKCECookieMissingError` continues after deploying the latest UI image, confirm Railway deployed the current commit and that the WorkOS dashboard callback URL exactly matches `WORKOS_REDIRECT_URI`.

Because `ORIGIN` pins SvelteKit's generated request URL to the canonical host, the UI auth guard also checks Railway's forwarded host headers before generating a WorkOS sign-in URL. Requests that arrive on a Railway-provided domain or other alternate UI host are redirected to the `WORKOS_REDIRECT_URI` host first, which keeps the PKCE verifier cookie on the same host as `/callback`.

## FoleoTrader Service

Configure the Railway FoleoTrader service with:

```text
Root Directory: /
Config File Path: /FoleoTrader/railway.json
Dockerfile Path: FoleoTrader/Dockerfile
Start Command: dotnet FoleoTrader.dll --urls http://0.0.0.0:${PORT:-8080}
Healthcheck Path: /
```

The FoleoTrader container exposes two ports:

```text
PORT=${Railway assigned HTTP port}
FoleoTrader__Port=9878
```

The HTTP port serves the FoleoTrader monitor page. The FIX acceptor listens on `FoleoTrader__Port`, and the API connects to it with a QuickFIX/n initiator using these default session IDs:

```text
API SenderCompID=FOLEOAPI
API TargetCompID=FOLEOTRADER
FoleoTrader SenderCompID=FOLEOTRADER
FoleoTrader TargetCompID=FOLEOAPI
```

If the FIX port is changed, update both the FoleoTrader service variable `FoleoTrader__Port` and the API service variable `FoleoTrader__Port`.

## Smoke Checks

After deployment:

- Open `https://<api-railway-domain>/API/System/Health`.
- Open the FoleoTrader public domain.
- Open the UI public domain.
- Confirm dashboard stats load.
- Confirm Build and Clear typed confirmation actions submit.
- Confirm build or aggregate maintenance SSE updates refresh Stats for nerds.

## CI/CD Flow

Recommended flow:

- Open PRs against `main`. GitHub Actions runs .NET restore/build/test, `npm run check`, `npm run build`, and Docker builds for the API and UI Railway services.
- Connect the API, UI, and FoleoTrader Railway services to this GitHub repo and deploy from `main`.
- Keep the Railway services on root directory `/`, with config file paths `/API/railway.json`, `/UI/railway.json`, and `/FoleoTrader/railway.json`.
- Enable Railway auto-deploys from `main`. Railway will use the per-service config files and watch patterns to decide which service deploys.

If you prefer GitHub Actions to trigger deployments directly, create a Railway project token and store it as a GitHub secret named `RAILWAY_TOKEN`, then run `railway up --ci --service <service-name> --environment <environment-name>` from a workflow after the CI job succeeds.
