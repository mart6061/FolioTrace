# FolioTrace — Aggregate Rebuild & Event Store Scaling Plan

Isolated design/implementation plan (not part of the API or UI remediation plans). Source: review of the
event-sourcing layer (`Repository/InMemoryEventsRepository.cs`, `Repository/MartenEventRepository.cs`,
`Features/Services/*`, and representative aggregate services `AccountService`, `HoldingService`,
`InstrumentService`, `HoldingPositionService`) on 2026-07-20, refined through discussion.

## Why this exists

The event store (`InMemoryEventsRepository`) loads and retains every event, for every stream, forever, in
process memory, with no eviction. Aggregates are rebuilt by replaying the *entire* applicable event history
from scratch on every cache miss — there is no incremental "seed from last known state" path anywhere in the
codebase. At the system's expected scale — 20 years of history, hundreds of events per day — this has three
compounding problems: rebuild cost grows without bound as history accumulates, the in-memory event store
itself grows without bound with no eviction, and (independently of both) some read paths do a full linear
scan on every single call even when the answer doesn't change. `HoldingPositionService`
(`Features/Holding/Services/HoldingPositionService.cs:95-102`) is the worst current offender — it loads the
*entire* `TransactionsStreamId` stream and recomputes running totals from scratch on every cache miss — and
should be the first target once the foundational work below lands.

This is bitemporal event sourcing: events carry both an `EventDateTime` (business-effective date) and an
`AuditDateTime` (when it was recorded), and corrections can be backdated — a new event can have an
`EventDateTime` in the past. The existing cache invalidation rule already handles this correctly (evict any
cached view whose valuation date is `>=` the new event's effective date, regardless of when it was
appended) — every task below has to preserve that invariant, not work around it.

Run the full `Test/` suite after every phase. Given the correctness stakes (this is the data layer for a
trading/portfolio system), get a second reviewer on Phase 3 specifically — don't merge snapshot logic on
CI-green alone.

---

## Phase 0 — Fix the cache-trust inconsistency (do first — this simplifies everything after it)

### 0.1 Remove the redundant per-read staleness check in `Get(valuationDate)`
**Problem:** 15 of the ~17 feature services with a no-`asAt` `Get(valuationDate)` overload — `AccountService`
(`Features/Account/Services/AccountService.cs:50-67`), `HoldingService`
(`Features/Holding/Services/HoldingService.cs:52-74`), `InstrumentService`
(`Features/Instrument/Services/InstrumentService.cs:53-75`), and the equivalent in Currency/Country/FX/
Broker/Ticket/User/etc. — call `eventRepository.GetLastEventIDAsync(streamId, valuationDate)` on *every*
call, including cache hits, to re-validate freshness before trusting the cached value:
```csharp
var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.AccountsStreamId, valuationDate.Value);
lock (cacheLock)
{
    if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == (lastEventID ?? Constants.Initialisation.EmptyViewEventID))
        return cached;
}
```
That `GetLastEventIDAsync(streamId, valuationDateTime, asOfDateTime: null)` call itself does a full linear
`foreach` scan over the entire stream (`Repository/InMemoryEventsRepository.cs:80-104`) to find the latest
matching event — on every call, whether or not the cache would have hit. Since no-`asAt` reads are the
dominant access pattern (most callers don't specify an audit date), this means the busiest read path pays a
full stream scan on every request, cache hit or not.

This is inconsistent with the rest of the codebase's own assumptions: `IsCached` (used by
`AggregateMaintenanceCoordinator`'s background warm loop, e.g. `AccountService.cs:31-38`) is a plain
`cache.ContainsKey` with no re-validation — the warm loop already trusts "cached until the invalidator evicts
it." `HoldingPositionService.Get` (`Features/Holding/Services/HoldingPositionService.cs:57-73`) follows that
same simpler trust-the-cache pattern with no re-check. The 15 services with the extra check are the outliers,
not the other way around.

**Fix:** Remove the `GetLastEventIDAsync` re-validation call from all 15 `Get(valuationDate)` overloads;
trust `cache.TryGetValue` alone, matching `HoldingPositionService`'s existing pattern. The safety this check
was likely guarding against — an event type whose `IAggregateCacheInvalidator` registration is missing or
wrong in `Features/Services/ServiceCollectionExtensions.cs` (a large hand-maintained list, ~90 entries) —
should be caught once, at startup, not paid for on every read forever. See 0.2.

**Important — the cache key shape must not change.** Both `AccountCacheKey.ForAllAuditHistory` (`AccountService.cs:89`)
and `HoldingPositionCacheKey.ForAllAuditHistory` (`HoldingPositionService.cs:129-130`) key "current" entries
with a stable `AsAtDateTime: null` sentinel, not a resolved timestamp. That's what makes caching work at all
for a "moving target" concept like "current" — a literal `DateTime.UtcNow` must never be folded into the
cache key itself, or every call would produce a different key, `TryGetValue` would never hit, and the fix in
this task would make things *worse*, not better (rebuild-on-every-call instead of rebuild-on-invalidation).
This task only removes the validation step that runs *before* the `TryGetValue` lookup — the `null`-sentinel
key itself is unchanged. What resolves to a literal `DateTime.UtcNow` is the *build* step on a cache miss
(e.g. `HoldingPositionService.cs:66`, `AuditDateTimeBuilder.Create()`), which stamps the materialized
aggregate's `AsOfDateTime` property — that value drifts on every rebuild, but rebuilds only happen on
invalidation, not on every read, so cache reuse is unaffected.

**Verify:** Existing tests for each service's `Get(valuationDate)` should pass unchanged (same results, no
behavior change from the caller's perspective — only the redundant validation is removed). Add a
micro-benchmark or at least a manual timing check confirming repeated `Get(valuationDate)` calls for an
already-cached, non-invalidated valuation date no longer scan the stream.

### 0.2 Add a startup-time invalidator completeness check
**Problem:** As noted in 0.1, the per-read check being removed was plausibly guarding against incomplete
`IAggregateCacheInvalidator` wiring — if a new event type is added to an aggregate's event union but nobody
registers an invalidator for it in `ServiceCollectionExtensions.cs`, that aggregate's cache would silently
never be invalidated by that event type, and (after 0.1) there would be no runtime check left to catch it.

**Fix:** Add a startup validation step (e.g. in `EventStoreStartupHostedService` or a new lightweight check
run during app startup) that, for each aggregate's event interface (`IAccountEvent`, `IHoldingEvent`, etc.),
enumerates all concrete implementing types (via reflection over the loaded assemblies, scoped to the
`FolioTrace` domain namespace) and asserts a matching `IAggregateCacheInvalidator<TEvent>` is registered in
DI for each one. Fail startup loudly (throw, don't just log) if a gap is found — this is exactly the kind of
class of bug that should be impossible to ship silently.

**Verify:** Add a test that registers an aggregate event type without a corresponding invalidator and
confirms the startup check throws; confirm the real DI configuration passes the check as-is today (i.e. no
existing gaps — if the check finds one, that's a genuine pre-existing bug worth fixing separately before this
plan continues).

### 0.3 Stop over-evicting fixed-`asAt` cache entries
**Problem:** `InvalidateFrom` (e.g. `AccountService.cs:93-105`) evicts every cache entry — including
fixed-`asAt` ("as of a specific historical audit date") entries — whose `ValuationDateTime >=` the new
event's effective date. But a fixed-`asAt` view is mathematically immutable once computed: a new event
always has `AuditDateTime = now`, which is later than any fixed historical `asAt`, and the aggregate
constructors already filter out events with `AuditDateTime > asOfDateTime`
(`Features/Account/Aggregates/Accounts.cs:39`). So a new event can never actually change a fixed-`asAt`
result — evicting it is unnecessary work, and after Phase 3 lands, unnecessary re-computation/re-snapshotting
of something that was already correct forever.

**Fix:** In each service's `InvalidateFrom`, skip cache keys where `AsAtDateTime.HasValue` — only evict the
no-`asAt` ("current") entries. This is a small, low-risk, mechanical change across the same ~15 services
touched in 0.1.

**Verify:** Add a test that populates both a "current" and a fixed-`asAt` cache entry for the same valuation
date, appends a new event affecting that date, and confirms only the "current" entry is evicted.

### 0.4 Unify how "current" resolves its `AsOfDateTime`
**Problem:** The two `Get(valuationDate)` implementations examined resolve "current" differently.
`AccountService`'s current-view build (`AccountService.cs:60-61`, via the `Accounts(valuationDateTime, items)`
constructor) derives `AsOfDateTime` from the data itself — the max `AuditDateTime` actually present among
applicable events (`Accounts.cs:179-185`, `GetLatestAuditDateTime`). `HoldingPositionService`'s current-view
build (`HoldingPositionService.cs:66`) instead calls `AuditDateTimeBuilder.Create()`, which is literally
`new AuditDateTime(DateTime.UtcNow)` (`Features/Types/AuditDateTime/AuditDateTimeBuilder.cs:11`). These can
diverge in what gets displayed/stamped: if the last relevant event was recorded five minutes ago, the
data-derived approach stamps `AsOfDateTime` as "five minutes ago" (when the data actually last changed),
while the wall-clock approach stamps it as "right now" (when the read happened to occur) — even though both
are serving equally fresh data out of the same cache bucket. Neither is wrong, but the inconsistency means
the same conceptual field ("as of when is this current?") means something subtly different depending on
which aggregate you're looking at.

**Fix:** Pick one convention and apply it across all "current" builds. Recommend the data-derived approach
(`AccountService`'s) — it's deterministic given the same event set (two rebuilds of the same underlying data
produce the same `AsOfDateTime`, useful for the byte-identical comparisons in 3.3's verification), and
doesn't imply more precision than the read actually reflects. Update the services using
`AuditDateTimeBuilder.Create()` for their current-view builds to instead derive from the data, matching
`GetLatestAuditDateTime`'s pattern.

This also sets the convention Phase 3 needs: a persisted "current" snapshot's *lookup* should resolve to
"latest available snapshot row for this valuation date," never an exact-match on a stored timestamp — see
the note in 3.1.

**Verify:** For each affected service, confirm `AsOfDateTime` on a "current" build reflects the max
`AuditDateTime` in the underlying data, not the wall-clock time of the read; add a test that reads twice in
quick succession with no intervening event and confirms an identical `AsOfDateTime` both times (this would
already pass for `AccountService` today, and should start passing for the services being changed).

---

## Phase 1 — Index the in-memory event store (fixes the linear-scan problem)

### 1.1 Maintain each stream pre-sorted instead of scanning on every read
**Problem:** `InMemoryEventsRepository.streams` (`Repository/InMemoryEventsRepository.cs:13`) is a
`Dictionary<Guid, List<IAuditEventBase>>` in append order. Every `LoadStreamAsync` call does
`events.ToList()` (a full copy) and every aggregate constructor (e.g. `Accounts.cs:38-55`) then does its own
`.Where(...).OrderBy(...).ThenBy(...).ThenBy(...)` over the *entire* result — an O(n) filter plus O(n log n)
sort, on every single rebuild, regardless of how much of the stream is actually relevant to the requested
valuation date.

**Fix:** Maintain each stream's list pre-sorted by the same composite key the aggregates already sort by —
`(EventDateTime, AuditDateTime, EventID)` — instead of raw append order. Since backdated corrections mean
new events don't always sort to the end, `AddEvent` (`InMemoryEventsRepository.cs:276-295`) needs to become
an insertion-sort: binary-search for the insertion point, `List.Insert` there. Insert cost is O(log n) to
find + O(n) to shift, which is the right trade given appends happen at "hundreds per day" while reads happen
far more often. `EventID` values are `Guid.CreateGuid7()` (time-ordered), so they're a convenient,
already-available tiebreaker consistent with `AuditDateTime` ordering — no extra bookkeeping needed there.

Once the list is guaranteed pre-sorted, `LoadStreamAsync` callers and the aggregate constructors can drop
their own `OrderBy/ThenBy` entirely (the constructor still needs its `Where` filter for the requested
valuation/audit cutoff, but can binary-search for the range boundary and slice instead of scanning
everything) — this removes an O(n log n) cost baked into every one of the ~10 aggregate types today,
independent of anything else in this plan.

**Verify:** Add tests confirming stream contents remain correctly ordered after (a) normal forward-appended
events and (b) a backdated correction inserted with an `EventDateTime` earlier than already-appended events.
Confirm aggregate construction produces identical results before/after removing the constructors' own
sort (should be a pure no-op given the input is now guaranteed sorted).

### 1.2 Also fix `GetLastEventIDAsync`'s linear scan (used by the remaining callers after Phase 0)
**Problem:** `GetLastEventIDAsync(streamId, valuationDateTime, asOfDateTime)`
(`InMemoryEventsRepository.cs:80-104`) does a `foreach` scan to find the latest event matching both date
cutoffs. After Phase 0 removes its use from the hot `Get(valuationDate)` path, it's still used elsewhere
(cache-warming diagnostics, potentially other call sites — grep for remaining usages before assuming it's
fully retired).

**Fix:** Once 1.1's pre-sorted structure exists, this becomes a binary-search-to-find-upper-bound instead of
a linear scan, for the same reason as 1.1.

**Verify:** Same approach as 1.1 — confirm identical results, check timing improves for large streams.

---

## Phase 2 — Bound in-memory growth (independent of snapshotting, but both target the same risk)

### 2.1 Add LRU eviction to the per-aggregate cache dictionaries
**Problem:** Every feature service's `cache` dictionary (`AccountService.cache`, `HoldingService.cache`,
`HoldingPositionService.cache`, etc.) grows without bound for the life of the process — every distinct
`(valuationDate, asAt, filter)` combination ever queried stays resident forever. Over years of usage across
many users running ad-hoc historical reports, this working set has no upper limit today.

**Fix:** Replace the plain `Dictionary<TKey, TAggregate>` with a bounded LRU structure (e.g.
`Microsoft.Extensions.Caching.Memory.MemoryCache` with a size limit, or a small hand-rolled LRU wrapper
consistent with the existing `Lock`-based concurrency pattern used throughout these services). Size limits
should be configurable per aggregate type, since item counts and payload sizes vary a lot between e.g.
`Accounts` (small, few items) and `HoldingPositions` (potentially large). This is safe to evict aggressively
once Phase 3 lands, since a cache miss then costs "reload nearest snapshot + short delta replay," not a full
history rebuild.

**Verify:** Add a test confirming the cache respects its configured size bound and evicts least-recently-used
entries first; confirm `GetDiagnostics()` (already exposes cache size/memory estimates via
`CacheMemoryEstimator`) reflects the bound correctly.

### 2.2 Trim in-memory raw events once they're covered by a durable snapshot
**Problem:** Even after Phase 1's indexing fix, `InMemoryEventsRepository` still retains every raw event
for every stream, forever — there's no mechanism to shed old data from process memory, only from the
durable Postgres store (which should always retain everything). At "hundreds of events/day" over 20 years,
this is on the order of a few million events system-wide with no upper bound on memory footprint.

**Fix:** This depends on Phase 3 (snapshotting) existing first, since a durable snapshot is what makes it
safe to trim: once a cold snapshot exists for a given stream at boundary B, raw events strictly before B are
no longer needed in memory for normal rebuilds (any rebuild for a valuation date after B starts from the
snapshot). Add a retention rule — keep events at/after the oldest available cold-snapshot boundary per
stream (plus a safety margin) resident in memory; trim older ones. Provide a fallback path for the rare case
where older raw events are genuinely needed (compliance export, deep audit diagnostics) that queries
Postgres directly via `MartenEventRepository` rather than assuming everything is resident.

Don't guess at thresholds — the codebase already exposes `CacheMemoryEstimator`/
`EventRepositoryCacheDiagnostics` (`Features/Services/EventRepositoryCacheDiagnostics.cs`,
`Features/Services/CacheMemoryEstimator.cs`) via diagnostics endpoints. Use those to measure real growth in
production and tune the retention window empirically.

**Verify:** Add a test that trims a stream's in-memory events below a snapshot boundary, then confirms a
rebuild for a valuation date after that boundary still produces correct results (sourced from snapshot +
retained delta), and that a request for data before the boundary correctly falls back to Postgres rather than
returning incomplete/wrong results.

---

## Phase 3 — Persisted snapshotting (the core of this plan)

### 3.1 Define snapshot storage
**Problem:** There is no persisted representation of a materialized aggregate anywhere — only raw events are
durable (in Postgres via Marten). Every process restart starts every aggregate cache empty and rebuilds
everything from scratch via `AggregateMaintenanceHostedService`'s warm loop.

**Fix:** Add a new Marten document type, e.g. `AggregateSnapshot`, with fields: `SnapshotID`,
`AggregateKind` (discriminator matching the existing `FeatureAggregateAttribute`/diagnostics naming, e.g.
"Accounts", "HoldingPositions"), `StreamId`, `ValuationDateTime` boundary, `AuditDateTime` boundary,
`LastEventID`, `LastAuditDateTime`, `PayloadJson`, `CreatedAtUtc`, `SourceEventCount` (for diagnostics).
Index on `(AggregateKind, StreamId, ValuationDateTime, AuditDateTime)` for "nearest preceding valid
snapshot" lookups. For plain event-sourced aggregates (Accounts, Instruments, Holdings, etc.) the payload is
the materialized `Items` list — these are already JSON-serializable (`Accounts` has a `[JsonConstructor]`
constructor, confirming this). For computed/derived aggregates like `HoldingPositions`, the payload should be
the computed per-item totals (quantity, book cost, last event ID per holding) rather than raw events —
smaller, and the delta-merge in 3.3 becomes a natural "add more movements to existing totals" operation
instead of a list replay.

**Snapshot lookup for "current" must resolve to "latest available," never an exact-match on a timestamp** —
same rule as 0.1/0.4 for the in-memory cache. A "current" snapshot naturally gets superseded by a newer row
every time it's rebuilt (each carrying whatever `AuditDateTime`/`CreatedAtUtc` was current at that rebuild),
which is fine and even useful for auditability — but the query serving a "give me current" request should
always be "give me the most recent row for this valuation date," not a lookup keyed on a specific instant.
Persisting multiple historical snapshot rows for the same "current" valuation date over time is expected
behavior, not a bug; retire/mark-superseded older rows (per 3.4) rather than deleting them outright if
there's any value in keeping a short history for diagnostics.

**Verify:** Round-trip test: serialize a materialized aggregate to a snapshot document, deserialize it back,
confirm it's identical to the original.

### 3.2 Snapshot cadence — mutability tiers
**Problem:** Not every valuation date should get a persisted snapshot with equal priority — recent dates
churn constantly (corrections land routinely for "today" and the last week or two), so persisting and
re-persisting a snapshot for them is wasted write volume for something that'll be invalidated again
tomorrow.

**Fix:** Define three configurable tiers, extending the existing `AggregateMaintenanceDateWindowOptions`
concept rather than inventing new scheduling infrastructure:
- **Hot** (e.g. last ~7-14 days, configurable): no persisted snapshot — stays as in-memory rebuild-on-miss
  only, same as today.
- **Warm** (e.g. ~2 weeks to 3 months old): persist a snapshot; expect occasional invalidation and
  re-snapshot on next warm/access.
- **Cold** (older than the warm threshold, or past an explicit period-lock/reporting-close date if one
  exists in the domain — worth checking whether `ReportConfigService`/`ValuationSetting` already models a
  "period closed" concept before inventing a day-count threshold): persist and treat as long-lived. An
  invalidation here should be rare enough to be worth logging/alerting on when it happens.

Extend `AggregateMaintenanceCoordinator`'s warm loop (`Features/Services/AggregateMaintenanceCoordinator.cs:109-124`)
so that, after warming a boundary that has crossed into the warm/cold tier, it also persists a snapshot for
it via 3.1's storage, rather than only populating the in-memory cache as it does today.

**Verify:** Test that a valuation date crossing from hot to warm gets a persisted snapshot on the next
maintenance run; confirm hot-tier dates never get one.

### 3.3 Rebuild-from-snapshot
**Problem:** Every aggregate constructor currently replays from an empty state (`Items = []`) every time,
regardless of how much of the history is unchanged since the last computation.

**Fix:** Add a seeded-construction path per aggregate: instead of `new Accounts(valuationDate, allEvents)`,
support `new Accounts(valuationDate, snapshotSeed, eventsAfterSnapshotBoundary)`, starting `Items` from the
snapshot's materialized state and applying only the delta events after the snapshot's boundary (using 1.1's
pre-sorted structure to fetch just that range cheaply). This touches every aggregate record — same shape
each time, so do it one aggregate at a time rather than as one large change, and prioritize
`HoldingPositions`/`TransactionsStreamId` first since it's the demonstrated worst case today (full-history
scan on every rebuild).

The service-level `Get` methods then become: find nearest valid snapshot ≤ target (from 3.1's indexed
storage) → fetch delta events via 1.1 → seeded-construct → cache in the (now-bounded, per 2.1) in-memory
cache.

**Verify:** For each migrated aggregate, add a test that compares a snapshot-seeded rebuild against a full
from-scratch replay for the same valuation date and confirms byte-identical results — including at least one
case where a backdated correction lands with an `EventDateTime` before an existing snapshot's boundary (see
3.4).

### 3.4 Snapshot invalidation — same rule as the live cache, extended to durable storage
**Problem:** A snapshot is only valid until a backdated correction arrives with an `EventDateTime` at or
before its valuation boundary — same bitemporal risk the live cache already handles via `InvalidateFrom`.

**Fix:** Extend `AggregateCacheInvalidationService`/`InvalidateFrom` (post Phase 0 fix) to also retire
matching snapshot documents in Marten, not just in-memory cache entries — same condition
(`snapshot.ValuationDateTime >= newEvent.EventDateTime`). Since audit dates only increase, treat snapshots
as immutable once written: retire (soft-delete or mark superseded) rather than mutate, and let the
maintenance loop compute and persist a fresh one at the new audit date next time that boundary is warmed.

**Verify:** Test that appending a backdated correction retires the correct snapshot(s) (and only those —
confirm boundaries before the correction's `EventDateTime` are untouched) and that the next warm/read for
that boundary produces a fresh, correct snapshot.

### 3.5 Seed in-memory caches from persisted snapshots on startup
**Problem:** Today, every process restart starts every aggregate cache empty; `AggregateMaintenanceHostedService`
has to fully rebuild everything via the warm loop before caches are populated again.

**Fix:** On startup (in `EventStoreStartupHostedService` or a new step after it), seed each aggregate
service's in-memory cache directly from the latest valid persisted snapshot per warmed boundary, rather than
relying purely on the periodic warm loop to rebuild from scratch. This is mostly a byproduct of 3.1-3.3
existing — flagged separately because it changes startup behavior and is worth its own test coverage.

**Verify:** Restart the API in a test/staging environment with existing snapshots present and confirm warm
boundaries are immediately served from cache without a full rebuild (check via logs/diagnostics — the
existing `AggregateMaintenanceRunResult`/diagnostics should show fewer `MissingAggregates` on the first run
after restart).

---

## Phase 4 — Rollout safety

### 4.1 Add a snapshot-vs-replay verification mode during rollout
**Problem:** A subtly wrong snapshot-plus-delta result is worse than a slow full replay — this is financial
position data, so silent drift is a serious risk, not just a performance concern.

**Fix:** For a sample of requests (or via a diagnostics endpoint, matching the existing `/Diagnostics/*`
pattern), compute both the snapshot-based result and a full from-scratch replay, and log/alert on any
mismatch. Keep this enabled through the rollout of Phase 3 for each aggregate, disable per-aggregate once
confidence is established.

**Verify:** Intentionally introduce a snapshot bug in a test branch (e.g. skip a delta event) and confirm the
verification mode catches the mismatch.

---

## Suggested execution order

1. Phase 0 (0.1 → 0.3) first — it's a real, present-day performance fix on its own, it removes the
   asymmetry that would otherwise complicate Phase 3, and it's low-risk/mechanical.
2. Phase 1 (event indexing) next — standalone value (removes O(n log n) sort cost everywhere), and a
   prerequisite for efficient delta-range queries in Phase 3.
3. Phase 2.1 (LRU on aggregate caches) can happen any time after Phase 0 — independent, low-risk.
4. Phase 3 (snapshotting) — the main effort. Do 3.1-3.2 (storage + cadence) first, then 3.3-3.4 per
   aggregate, starting with `HoldingPositions`/Transactions. Do 3.5 (startup seeding) once at least one
   aggregate's snapshot pipeline is proven.
5. Phase 2.2 (trimming in-memory raw events) only after Phase 3 has real snapshot coverage to trim against —
   don't do this before snapshots exist, or there's nothing safe to trim to.
6. Phase 4 (verification mode) runs alongside Phase 3's per-aggregate rollout, not as a separate later step.
