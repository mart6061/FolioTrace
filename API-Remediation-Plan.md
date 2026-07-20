# FolioTrace API — Remediation Plan

Handoff doc for implementation. Source: structural/performance review of `API/` on 2026-07-20.
Each task lists the problem, affected files, the fix, and how to verify it. Work top to bottom within a
phase; phases are ordered by risk/blast-radius, not effort. Existing tests live in `Test/` — run the full
suite after each phase (`dotnet test`) and add new tests where noted.

---

## Phase 1 — Correctness & safety (do first)

### 1.1 Fix middleware pipeline order
**Problem:** `API/Program.cs:110-115` registers trace capture and request logging *before*
`ApiUnhandledExceptionLoggingMiddleware`. Any exception thrown inside those two middlewares (or inside
`next()` before the exception handler is reached) skips the problem+json handler and falls through to the
default ASP.NET Core error page — unformatted and unlogged.

**Fix:** Reorder so the unhandled-exception handler wraps everything else:
```csharp
app.UseApiUnhandledExceptionLogging();
app.UseRequestTraceCapture();
app.UseApiRequestLogging();
app.UseMiddleware<ApiReadinessMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```
**Verify:** Add/confirm a test that throws inside a downstream middleware (or a route) and asserts the
response is a well-formed `problem+json` with the correct status code, not the ASP.NET default error page.

### 1.2 Un-invert `UseHttpsRedirection()`
**Problem:** `API/Program.cs:99-108` only calls `UseHttpsRedirection()` inside `IsDevelopment()`. Non-dev
environments never redirect HTTP→HTTPS at the app level.

**Fix:** Move `app.UseHttpsRedirection();` out of the `if (IsDevelopment())` block so it runs in all
environments (keep `UseSwagger`/`UseSwaggerUI` dev-only). If a reverse proxy already terminates TLS and
redirect is intentionally proxy-owned, replace this with a code comment explaining that instead of leaving
it silently dev-only — confirm with whoever owns deployment before deciding which.

**Verify:** Manual check / integration test that a plain HTTP request outside Development gets a 307/308 to
HTTPS.

### 1.3 Add exception handling around FIX client startup
**Problem:** `API/FoleoTrader/FoleoTraderFixClient.cs:36-40`:
```csharp
public Task StartAsync(CancellationToken cancellationToken)
{
    _ = StartWhenReadyAsync(cancellationToken);
    return Task.CompletedTask;
}
```
`StartWhenReadyAsync` is fire-and-forget. If it throws (e.g. `ReplayStoredExecutionReportsAsync` hitting a
bad file, or the readiness wait throwing), the exception is unobserved — FIX replay dies silently and
nothing logs it.

**Fix:** Wrap the body of `StartWhenReadyAsync` in try/catch with `logger.LogError(...)`, matching the
pattern already used in `RecordFIXOperation`/`WaitForLogonAsync` in the same file. Consider also flipping
`ApiReadinessState` to unready (or logging at Critical) if startup fails, so ops has a signal.

**Verify:** Unit test that forces `ReplayStoredExecutionReportsAsync` to throw and asserts the exception is
logged rather than swallowed/unobserved.

### 1.4 Move plaintext DB password out of source control
**Problem:** `API/appsettings.Development.json:9-11` has a plaintext Postgres connection string/password
checked in, despite `API.csproj` declaring a `UserSecretsId` for exactly this purpose. `WorkOS:ApiKey` is
correctly left empty in the same file — the handling is inconsistent.

**Fix:** Remove the password from `appsettings.Development.json` (leave `ConnectionStrings:FolioTrace`
empty or point at a placeholder), move the real local value into `dotnet user-secrets` for the API project,
and document the one-time setup step (e.g. in `README.md` or `CONTRIBUTING.md`):
`dotnet user-secrets set "ConnectionStrings:FolioTrace" "Host=...;Password=..."` from `API/`.
If this password is also used in CI/docker-compose for local dev spin-up, keep a separate
non-secret-flagged compose file for that instead of `appsettings.Development.json`.

**Verify:** `git grep -i password` in `API/appsettings*.json` returns nothing; local dev still starts
correctly after running the user-secrets command.

---

## Phase 2 — Performance (do second)

### 2.1 Stop unconditionally buffering response bodies in RequestTraceCaptureMiddleware
**Problem:** `API/RequestTraceCaptureMiddleware.cs:50-51,63-67` swaps `context.Response.Body` for a
`MemoryStream` for *every* request that passes `ShouldCapture` (an enabled/path check), then always copies
the full buffered response back to the original stream — even when `settings.CaptureBodies == false`, where
none of the buffered bytes are actually used. `CaptureBodies: true` is the default in both
`appsettings.json` and `appsettings.Development.json`, so this runs on every non-excluded request in every
environment as shipped.

**Fix:**
- Read `settings.CaptureBodies` before swapping `context.Response.Body`. If body capture is off, only track
  timing/status/headers — write straight through to the original stream, don't allocate a `MemoryStream`.
- When body capture *is* on, keep the current approach but cap the buffer (e.g. only buffer up to
  `settings.MaximumBodyCharacters` bytes, truncate/stop buffering beyond that rather than buffering the
  whole payload then trimming after the fact) to bound memory use for large responses (trade file downloads,
  big JSON lists).
- Consider whether large binary responses (e.g. `/TradeFiles/{id}/File`, see 2.4) should be excluded from
  trace capture entirely via `ExcludedPathPrefixes`, since capturing binary bodies as text is unlikely to be
  useful.

**Verify:** Load-test or unit-test that response streaming behavior/memory allocation differs correctly
between `CaptureBodies: true` and `false`; confirm existing request-trace tests in `Test/` still pass.

### 2.2 Move request-trace repository writes off the synchronous request path
**Problem:** `RequestTraceCaptureMiddleware.AppendAsync` (`API/RequestTraceCaptureMiddleware.cs:99-109`)
calls `repository.AppendAsync(traceEvent, CancellationToken.None)` inline, twice per traced request (once
per request, once per response), using `CancellationToken.None` so it can't be cancelled on client
disconnect. This is inconsistent with the sibling log-message trace path, which already goes through an
async `Channel` (`RequestTraceLogQueue` → `RequestTraceLogBackgroundService`).

**Fix:** Route request/response trace events through the same `RequestTraceLogQueue` (or a second queue with
the same shape) instead of writing to the repository inline. This takes the DB write off the hot request
path and makes the design consistent with the log-event path. Keep the existing try/catch-and-log-warning
behavior in the queue consumer.

**Verify:** Confirm request latency (P50/P99 in a local load test, or at minimum a `dotnet test` timing
assertion if one exists) doesn't include repository round-trip time; confirm trace events still show up
correctly end-to-end.

### 2.3 Bound the RequestTraceLogQueue channel
**Problem:** `API/RequestTraceLogQueue.cs:8-13` uses `Channel.CreateUnbounded<RequestTraceEvent>()`. If the
consumer falls behind (DB slowness), the queue grows without limit — no backpressure, can OOM the process.

**Fix:** Switch to `Channel.CreateBounded<RequestTraceEvent>(capacity)` with `BoundedChannelFullMode.DropOldest`
(logging should never block or crash the app) and log a warning/counter when drops occur. Pick a capacity
based on expected burst size (e.g. a few thousand events) — make it configurable via `RequestTraceOptions`
if there's already a config surface for this feature.

**Verify:** Unit test that once the channel is full, writes don't block and old/new events are dropped
per the configured policy rather than throwing or growing unbounded.

### 2.4 Fix `/Diagnostics/FIXTrace` full-stream-in-memory paging
**Problem:** `API/ApiEndpointRegistration.cs:427-466` loads the *entire* FIX operations event stream via
`eventRepository.LoadStreamAsync<FoleoTraderFIXOperationRecordedEvent>(...)`, then filters/sorts/pages with
LINQ in memory. This stream grows unbounded over the life of the FIX connection (every inbound/outbound
admin+app message). Contrast with the sibling `/Diagnostics/RequestTrace` endpoint
(`API/ApiEndpointRegistration.cs:322-365`), which pushes filtering/paging into `repository.SearchAsync(...)`.

**Fix:** Add a paginated/filtered query method to the FIX-operations repository (mirroring
`IRequestTraceRepository.SearchAsync`) and use it here instead of `LoadStreamAsync` + in-memory
`Where/OrderByDescending/Skip/Take`. If a fully general query isn't feasible short-term, at minimum cap how
much of the stream is loaded (e.g. most-recent N events) rather than the full history.

**Verify:** Test with a large synthetic FIX operations stream (thousands of events) and confirm response
time/memory doesn't scale linearly with total stream size, only with page size.

### 2.5 Parallelize independent lookups instead of sequential awaits
**Problem:** Repeated pattern of independent `await` calls run sequentially where they could run in
parallel:
- `API/FoleoTrader/FoleoTraderOrderProcessor.cs:28-31` — `tickets`, `instruments`, `brokers`,
  `existingOrders` all awaited one after another.
- Same file, `ProcessExecutionReportAsync:152-154` — `tickets`, `holdings`, `instruments`.
- Recurs in a couple of `ApiEndpointRegistration.cs` handlers (`/Tickets/Details`,
  `/AssetAllocationMappings`).

**Fix:** Where the calls are genuinely independent (no data dependency between them), fetch the `Task`s
first and `await Task.WhenAll(...)` before consuming results, e.g.:
```csharp
var ticketsTask = ticketService.Get(request.EventDateTime, asAt);
var instrumentsTask = instrumentService.Get(request.EventDateTime, asAt);
var brokersTask = brokerService.Get(request.EventDateTime, asAt);
var existingOrdersTask = foleoTraderOrderService.Get(request.EventDateTime, asAt);
await Task.WhenAll(ticketsTask, instrumentsTask, brokersTask, existingOrdersTask);
var tickets = ticketsTask.Result;
// ...
```
Confirm none of these underlying services have shared-state/thread-safety issues before parallelizing
(check if they're backed by a single cached collection accessed concurrently — should be fine if read-only,
flag to a human reviewer if unsure).

**Verify:** Existing tests in `Test/` covering `FoleoTraderOrderProcessor` should still pass with identical
output; add a test asserting all four services are invoked (order-independent) if not already covered.

### 2.6 Cache the reflection lookup in UserConsistencyEndpointFilter
**Problem:** `API/Auth/UserConsistencyEndpointFilter.cs:38-53` does an uncached
`argument.GetType().GetProperty("UserID", ...)` + `.GetValue(argument)` on every non-null argument, for
every request to every protected endpoint (this filter is applied globally to `protectedApi` routes in
`ApiEndpointRegistration.cs:49-51`).

**Fix:** Cache a compiled getter delegate per argument `Type` in a `ConcurrentDictionary<Type, Func<object,
object?>?>`, same pattern already used in `EventPropertyDetailsFactory.cs:13,81-89`. Build the delegate once
per type (reflection + `Expression.Compile()`, or cache the `PropertyInfo` at minimum if a full compiled
getter is overkill) and reuse it on subsequent requests with the same argument type.

**Verify:** Unit test hitting the same endpoint type repeatedly and asserting (via a counter or mock) that
reflection resolution only happens once per distinct type, not once per request.

---

## Phase 3 — Structural cleanup (do third — larger diffs, coordinate on timing)

### 3.1 Split `ApiEndpointRegistration.cs` by feature
**Problem:** `API/ApiEndpointRegistration.cs` is ~3,800 lines covering Accounts, Brokers, Holdings, Tickets,
Users, Events, Diagnostics, Notifications, System, etc., all in one file/class — inconsistent with
`Auth/AuthEndpointRegistration.cs` and `TradeFiles/TradeFileEndpointRegistration.cs`, which each get their
own file/folder.

**Fix:** Extract each `MapXEndpoints` group into its own file under a matching subfolder, e.g.:
- `Accounts/AccountEndpointRegistration.cs`
- `Brokers/BrokerEndpointRegistration.cs`
- `Holdings/HoldingEndpointRegistration.cs`
- `Tickets/TicketEndpointRegistration.cs`
- `Users/UserEndpointRegistration.cs`
- `Events/*EventEndpointRegistration.cs` (per event type, or one file if small)
- `Diagnostics/DiagnosticsEndpointRegistration.cs`

Keep `ApiEndpointRegistration.MapFolioTraceApi()` as the single composition root that calls each
`app.MapXEndpoints()` extension method, same as it does today for Auth/TradeFiles. This should be a
mechanical extract-method + move-file refactor — no behavior change. Do this as its own PR since the diff
will be large and mostly noise (file moves); keep any real logic changes out of this PR.

**Verify:** `dotnet build` + full `dotnet test` pass with zero behavior change; diff review should show only
moves, not logic edits. Swagger UI route list should be identical before/after.

### 3.2 Deduplicate the "get with optional audit date" endpoint boilerplate
**Problem:** The same shape is copy-pasted across ~12 handlers in `ApiEndpointRegistration.cs` (e.g. lines
538-545, 552-559, 737-744, 751-758, 765-772, 779-786, 820-827, 834-841 — line numbers will shift after 3.1,
re-locate by pattern match):
```csharp
var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
return auditDateTime.HasValue
    ? Results.Ok(await xService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
    : Results.Ok(await xService.Get(valuationDate));
```

**Fix:** Add a small shared helper, e.g.:
```csharp
internal static async Task<IResult> GetAsAt<T>(
    DateTime eventDateTime,
    DateTime? auditDateTime,
    Func<DateTime, DateTime?, Task<T>> getter)
{
    var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
    var result = await getter(valuationDate, auditDateTime.HasValue ? AuditDateTimeBuilder.Create(auditDateTime.Value) : null);
    return Results.Ok(result);
}
```
(adjust signature to match each service's actual `Get` overloads — some take a nullable audit date directly,
some have separate overloads; normalize the service interfaces first if needed so one helper shape covers
all of them). Replace each duplicated block with a call to the helper. Do this after 3.1 so the diff lands
in the smaller, already-split files.

**Verify:** Existing endpoint tests should pass unchanged; add a test for the helper itself covering both
the with-audit-date and without-audit-date branches.

### 3.3 Move FIX order-processing business logic into `Features`
**Problem:** `API/FoleoTrader/FoleoTraderOrderProcessor.cs` contains trading domain logic — order
validation (`ValidateOrder`, ~lines 246-303), prorated trade-allocation construction
(`CreateProratedTradeEvent`, ~lines 305-364), cash-holding resolution (`ResolveCashHoldingID`, ~lines
366-385), settlement-date calculation — inside the API project, while equivalent ticket/trade domain logic
(`TicketEventBuilder`, `TicketTradeExecutionEventBuilder`) lives in `Features`. This breaks the intended
`API → Features → Repository` layering (API should be a thin transport/host layer).

**Fix:** This is the largest and riskiest change in this plan — treat it as its own reviewed PR, not bundled
with anything else.
1. Identify the pure business-logic methods (validation, allocation math, settlement-date calc,
   cash-holding resolution) versus the FIX-transport-specific glue (talking to `FoleoTraderFixClient`,
   building/parsing FIX messages).
2. Move the business-logic methods into a new `Features` service (e.g.
   `Features/FoleoTraderOrders/FoleoTraderOrderService.cs`), following the existing builder pattern used by
   `TicketEventBuilder`.
3. Leave `API.FoleoTrader.FoleoTraderOrderProcessor` as a thin orchestrator: receive FIX execution
   reports/order requests, call into the new `Features` service, translate results back to FIX
   messages/API responses.
4. Move associated unit tests from wherever they currently live (check `Test/`) to test the `Features`
   service directly, independent of any FIX/API scaffolding.

**Verify:** Full existing `FoleoTraderOrderProcessor` test coverage must still pass after the move (same
inputs → same outputs). Get a second reviewer on this PR specifically given the trading-correctness stakes
(prorated allocation math, settlement dates) — don't just rely on automated tests.

### 3.4 Resolve the FoleoTrader naming collision and stale WorkOSSsoClient filename
**Problem:**
- Two unrelated `FoleoTraderOptions` classes exist: `API/FoleoTrader/FoleoTraderOptions.cs` (FIX initiator
  config) and the sibling project's `FoleoTrader/FoleoTraderOptions.cs` (FIX acceptor/simulator config) —
  same name, different shape, different projects. Easy to edit the wrong one.
- `API/Auth/WorkOSSsoClient.cs` actually defines `WorkOSAuthKitClient`/`IWorkOSAuthKitClient`, with
  `IWorkOSSsoClient` kept only as a backwards-compat alias (per the comment in that file) — the filename is
  stale from an incomplete SSO→AuthKit rename.

**Fix:**
- Rename `API/FoleoTrader/FoleoTraderOptions.cs`'s class to something more specific, e.g.
  `FoleoTraderConnectionOptions` (or `FoleoTraderApiOptions`), and update its config section name / all
  references (`Program.cs:68`, `Configure<FoleoTraderOptions>`) accordingly. Do the equivalent for the
  simulator project's version if it makes sense there too (e.g. `FoleoTraderAcceptorOptions`), or at least
  add a doc-comment on each class noting the other one exists in the sibling project, if a rename isn't
  wanted right now.
- Rename `API/Auth/WorkOSSsoClient.cs` → `API/Auth/WorkOSAuthKitClient.cs` to match its actual class name.
  If `IWorkOSSsoClient` is genuinely still needed for backwards compat, keep it but move it into the same
  renamed file or a clearly-labeled `WorkOSAuthKitClient.Legacy.cs`, and add a `[Obsolete]` attribute with a
  removal-target note if it's meant to go away eventually.

**Verify:** `dotnet build` succeeds after each rename (compiler catches all references); `git grep` for the
old type names returns nothing outside of comments explicitly documenting the rename history.

---

## Not included in this plan (flagged for a human decision, not Codex)

- **`WorkOSAuthKitClient` setting a global static `WorkOSConfiguration.WorkOSClient`** from its constructor
  (`API/Auth/WorkOSSsoClient.cs:31-46`) — works today with a single singleton instance, but is a design
  smell (implicit global mutable state) that may be intentional if the underlying WorkOS SDK requires it.
  Needs a decision on whether the SDK has a non-static-config API before touching this.
- **`TradeFileWorkbookGenerator` building the whole worksheet in one in-memory `StringBuilder`** and
  `/TradeFiles/{id}/File` loading the whole file into memory before serving it (no streaming, no HTTP range
  support) — only worth fixing if trade files are expected to grow large; confirm typical/max file size with
  product owner before prioritizing.
- **`FolioTraceUserIdentityService.EnsureUserAsync` loading the full user event stream on every login** —
  likely fine if `LoadStreamAsync` hits an in-memory cache (there's a `/Diagnostics/Memory` endpoint
  suggesting these streams are cached), but confirm cache-hit behavior before treating this as a real
  problem.

---

## Suggested execution order for Codex

1. Phase 1 (1.1 → 1.4) as one PR — small, low-risk, high-value correctness fixes.
2. Phase 2 items 2.1–2.4 together (all touch the request-trace/diagnostics area) as one PR; 2.5 and 2.6 can
   be a second, independent PR since they touch unrelated files.
3. Phase 3.1 (file split) alone as a mechanical PR.
4. Phase 3.2 (dedup) after 3.1 lands.
5. Phase 3.3 (move business logic to Features) as its own carefully reviewed PR — do this last and get
   explicit sign-off given the trading-correctness stakes.
6. Phase 3.4 (naming) can happen any time after Phase 1, independently of the others.

Run `dotnet build` and `dotnet test` (from repo root, or `Test/Test.csproj`) after every PR before moving to
the next.
