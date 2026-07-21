# FolioTrace UI — Remediation Plan

Handoff doc for implementation. Source: structural/performance review of `UI/` (SvelteKit, Svelte 5,
TypeScript strict mode) on 2026-07-20. Each task lists the problem, affected files, the fix, and how to
verify it. Work top to bottom within a phase; phases are ordered by risk/blast-radius, not effort. Run
`npm run check` (svelte-check/tsc) and `npx playwright test` after each phase.

Overall architecture is sound and doesn't need to change: `hooks.server.ts` centralizes
auth/session, `+layout.server.ts` loads shared per-request data, routes follow a consistent
`+page.server.ts` (load/actions) → `+page.svelte` (render) pattern, and state management is consistently
runes-based (no Svelte stores anywhere) — none of that should be touched. This plan is about duplication,
missing shared utilities, and a few scaling/UX gaps within that architecture.

---

## Phase 1 — Correctness & UX safety (do first)

### 1.1 Add error handling to BookmarkButton's save action
**Problem:** `UI/src/lib/components/BookmarkButton.svelte:17-43` — `saveBookmark()` has a `try/finally` but
no `catch`. If the POST to `/API/UserBookmarks` fails (`!response.ok`) or throws (network error), the
exception propagates as an unhandled promise rejection: `saving` still resets to `false` via `finally`, but
nothing is shown to the user — the button just silently doesn't bookmark, with no visible error state.

**Fix:** Add a `catch` block that sets a local `error` state (e.g. `let saveError = $state<string | null>(null)`)
and render a brief inline error message (or reuse whatever pattern comes out of task 1.2) instead of letting
the rejection go unhandled. Keep the existing `finally { saving = false; }`.

**Verify:** Manually simulate a failed POST (e.g. temporarily throw in a dev proxy, or use browser devtools
to block the request) and confirm the user sees an error state instead of a silently-stuck button.

### 1.2 Establish a shared error-surfacing pattern
**Problem:** There is no shared toast/banner/snackbar component anywhere in the codebase (confirmed: no
`toast`, `Banner`, or `Snackbar` component exists). Server-load errors have one consistent pattern
(`+page.server.ts` catches and returns `{ error: string }`, rendered via a `status-panel-error` div — see
`Diagnostics/FIXTrace/+page.server.ts:27-33` / `+page.svelte:231-234`), but client-triggered actions (button
clicks, form submits outside SvelteKit's own `use:enhance` flow) have no equivalent, as shown by 1.1.

**Fix:** Add one small shared component, e.g. `$lib/components/InlineStatus.svelte` (or a lightweight
toast if preferred), that takes a message/kind and renders consistently. Use it for `BookmarkButton` (1.1)
and audit other client-side `fetch()` calls outside `+page.server.ts` load/actions for the same gap (grep
for `await fetch(` inside `.svelte` files — `BookmarkButton.svelte` was the only one found in this review,
but re-check after any new client-fetch code lands).

**Verify:** Visual check that error states render consistently across the components that use the new
pattern.

---

## Phase 2 — Deduplication (do second — mechanical, low-risk, high line-count reduction)

### 2.1 Extract shared CSV/HTML export utilities
**Problem:** Nine files define byte-for-byte identical `downloadFile()`, `csvValue()`, and `htmlValue()`
functions: `Report/ReportExperience.svelte`, `Data/Reference/Brokers/+page.svelte`,
`Data/Reference/Accounts/+page.svelte`, `Data/Reference/Currencies/CurrencyExperience.svelte`,
`Data/Reference/Countries/CountryExperience.svelte`, `Value/InstrumentValues/InstrumentValueExperience.svelte`,
`Data/Reference/Instruments/InstrumentBaseExperience.svelte`, `Value/FXRates/FXValueExperience.svelte`,
`Value/FXs/FXBaseExperience.svelte` (confirmed via grep for `function downloadFile` / `function htmlValue`
across `UI/src`).

**Fix:** Create `UI/src/lib/export.ts` exporting `downloadFile(filename, content, mimeType)`,
`csvValue(value)`, and `htmlValue(value)` (copy the existing implementation verbatim from any one of the
nine files — they're identical, so no behavior decision needed). Update all nine files to import from
`$lib/export` and delete their local copies.

**Verify:** `npm run check` passes; manually export a CSV and an HTML table from two or three of the updated
pages and confirm output is byte-identical to before the change (diff against a pre-change export if
possible).

### 2.2 Extract shared `getFormString` helper
**Problem:** The identical helper is defined in 15 separate `+page.server.ts` files (confirmed via grep):
```ts
function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
```
Files: `Data/Reference/{Accounts,Instruments,Holdings,Currencies,Countries,Brokers}/+page.server.ts`,
`Blotter/+page.server.ts`, `Value/{InstrumentValues,FXs,FXRates}/+page.server.ts`,
`User/Preferences/+page.server.ts`, `Data/Configuration/{ReportTools,AssetAllocationTools,AssetAllocation,AccountTools}/+page.server.ts`.

**Fix:** Move to `UI/src/lib/server/forms.ts` (new file, or add to an existing shared server-utils file if
one fits better) and update all 15 call sites to import it. Purely mechanical — no behavior change.

**Verify:** `npm run check` passes; existing form-submission flows (spot-check 2-3 of the 15 routes) still
save correctly.

### 2.3 Reduce fetch/error-handling boilerplate in `$lib/server/api.ts`
**Problem:** `UI/src/lib/server/api.ts` is 2,861 lines; nearly all ~100 exported functions repeat:
```ts
const response = await fetchApi(url);
if (!response.ok)
  throw new Error(`API returned ${response.status} ${response.statusText}`);
return (await response.json()) as SomeType;
```
Error handling is also inconsistent — most functions throw a plain `Error` (callers can't branch on HTTP
status), while a handful (`postSystemBuild`, `postSystemClearCache`, several FX/instrument
POST endpoints) throw the richer `ApiError` with `.status` and `readApiError(errorText)` parsing of the
response body.

**Fix:** Add one generic helper:
```ts
async function apiFetch<T>(fetchApi: FetchApi, url: string, init?: RequestInit): Promise<T> {
  const response = await fetchApi(url, init);
  if (!response.ok) {
    const errorText = await response.text().catch(() => '');
    throw new ApiError(response.status, readApiError(errorText) ?? response.statusText);
  }
  return (await response.json()) as T;
}
```
(match `ApiError`'s actual constructor signature and `readApiError`'s actual behavior — both already exist
in the file, reuse them rather than redefining). Then migrate the ~100 functions to call `apiFetch<T>(...)`
instead of hand-rolling the check. Do this incrementally (e.g. one route-group of functions per commit)
rather than as one giant diff, since it's a lot of surface area — group by the route folders that consume
each function so partial progress is safe to ship.

**Verify:** `npm run check` passes; run the full Playwright suite after each incremental batch; spot-check
that error responses now consistently carry `.status` where a caller inspects it (grep for
`instanceof ApiError` or `.status` checks on caught errors to confirm no regressions in status-dependent
branches).

---

## Phase 3 — Performance & scaling (do third)

### 3.1 Add server-side pagination to unbounded reference/valuation list pages
**Problem:** `getInstruments`, `getCountries`, `getCurrencies`, `getInstrumentValues`, `getFXRates`,
`getBrokers` (all in `$lib/server/api.ts`) fetch entire collections with no page/limit params; the
corresponding `+page.server.ts` loads pass the full result to the client, which filters/sorts entirely
client-side inside `$derived` blocks (e.g. `InstrumentBaseExperience.svelte:35-49`,
`CountryExperience.svelte:40-75`) with no debounce on the filter input — every keystroke re-filters/re-sorts
the full unpaginated array (see 3.2). No virtualization library exists in the project. By contrast,
`Diagnostics/FIXTrace` and `Diagnostics/RequestTrace` already implement server-side `page`/`pageSize`
pagination correctly — the pattern exists, it's just not applied to the reference-data routes.

**Fix:** Port the FIXTrace/RequestTrace pagination pattern (server-side `page`/`pageSize` query params,
paginated API response shape) to the reference/valuation routes, starting with whichever has the largest
real-world row count (check with the team — likely Instruments or InstrumentValues). This depends on the
underlying API endpoints supporting server-side paging/filtering; if they don't yet, this task blocks on the
equivalent API-side work (see the API remediation plan, task 2.4, for the analogous
`/Diagnostics/FIXTrace` fix — the same repository-level pagination capability may need to be added for
these entities too). Coordinate with whoever owns the API repo layer before starting.

**Verify:** Load-test with a large synthetic dataset (thousands of rows) and confirm page load time/payload
size scales with page size, not total row count.

### 3.2 Debounce client-side filter inputs
**Problem:** `CountryExperience.svelte:40-75`, `CurrencyExperience.svelte:40-75`, and
`InstrumentBaseExperience.svelte:35-49` each run `.filter().sort()` over the full dataset inside a
`$derived`, recomputed on every keystroke in the filter text input, with no debounce.

**Fix:** Debounce the derived filter input (e.g. a small `$state` for the raw input value updated
immediately for responsiveness, and a debounced — ~150-200ms — derived value that actually drives the
filter/sort computation). This is independent of 3.1 and can ship first/separately since it's much smaller;
worth doing even after pagination lands, since a given page of results can still benefit from debounced
re-filtering if any client-side filtering remains.

**Verify:** Type quickly into a filter box on one of the three pages and confirm (via browser perf profiler
or just visual smoothness) that filtering no longer runs on every single keystroke.

### 3.3 Add tab-visibility awareness to AggregateUpdateWatcher
**Problem:** `UI/src/lib/components/AggregateUpdateWatcher.svelte:35-58` opens its own `EventSource` plus a
1-second `setInterval` per mounted instance, with no `document.visibilitychange` handling — used on 15+
pages (Asset, Brokers, Accounts, Blotter, Holdings, Currencies, Countries, InstrumentValues, Instruments,
FXRates, FXs, DataList, AssetAllocationTools, AssetAllocation, ReportTools), each independently opening its
own SSE connection. Cleanup on unmount is already correct (`clearInterval` + `removeEventListener` +
`source.close()`) — this is not a leak, just wasted connections/ticks while backgrounded.

**Fix (two independent parts, either can ship alone):**
- Pause the `setInterval` polling loop when `document.hidden` is true (listen for
  `document.visibilitychange`, skip the poll check while hidden, resume on visible).
- Longer-term/optional: consolidate the per-component `EventSource` into a single app-level connection
  shared via Svelte context or a module-level singleton, so 15 mounted components share one SSE stream
  instead of opening 15. This is a bigger change (touches how notifications are dispatched to each
  component) — treat as a separate, optional follow-up rather than bundling with the visibility fix.

**Verify:** Open a page using the watcher, background the tab, and confirm (via Network tab / a temporary
console log) that polling pauses; confirm reload-on-visible still works correctly when the tab regains
focus.

### 3.4 Add a TTL or invalidation to the process-wide API version cache
**Problem:** `UI/src/routes/+layout.server.ts:11-12,68-80` caches the API version string in module-level
(process-wide, all-users) mutable state with no TTL:
```ts
let cachedApiVersion: string | null = null;
let apiVersionRequest: Promise<string> | null = null;
```
After an API redeploy, the footer's "API version" string could show a stale value indefinitely until the UI
Node process itself restarts. Low impact (display-only) but worth fixing since it's a DIY cache outside
SvelteKit's own load-caching mechanisms.

**Fix:** Add a simple TTL (e.g. re-fetch if the cached value is older than 5 minutes) or move this to use
SvelteKit's built-in `fetch` caching / a short-lived in-memory cache with an explicit expiry timestamp
alongside the cached value.

**Verify:** Confirm the version string updates within the TTL window after an API version bump, without
requiring a UI restart.

---

## Phase 4 — Type safety & testing (do fourth — larger investment, coordinate on scope)

### 4.1 Generate UI types from the API's OpenAPI spec
**Problem:** No codegen exists (confirmed: no `openapi`/`swagger`/`codegen`/`generated` references anywhere
in `UI/`). `$lib/types.ts` (~1,186 lines) and ~100 hand-written request DTOs in `$lib/server/api.ts` are
manually kept in sync with the API's C# DTOs. Evidence of drift pain already exists: `UserReferenceEvent`
(`$lib/server/api.ts:535-541`) hedges on its event discriminator with three optional casing variants
(`$type?`, `type?`, `Type?`) because the actual serialization format isn't reliably known at compile time.

**Fix:** Introduce `openapi-typescript` (or similar) driven off the API's existing Swagger/OpenAPI document
(the API project already has `AddSwaggerGen`/`AddEndpointsApiExplorer` configured per the API review).
Generate types into a build-time or checked-in file and gradually replace hand-written types in
`$lib/types.ts` with generated ones, starting with the entities most prone to drift (anything with a
discriminated/polymorphic shape like the event types). This is a larger, incremental effort — scope it as
its own project rather than a single PR; get sign-off on the codegen tool choice before starting.

**Verify:** `npm run check` passes with generated types in place; the `UserReferenceEvent` casing hedge
(and any similar workarounds found during migration) should become unnecessary once the real API shape is
known at compile time — remove them as confirmation the generated types are accurate.

### 4.2 Add runtime validation at the session-response boundary
**Problem:** `UI/src/hooks.server.ts:134` — `return (await response.json()) as CurrentUser;` is a raw type
assertion on network data with no runtime schema check. If `/Auth/Session`'s response shape changes
unexpectedly, this fails silently rather than at a clear validation boundary.

**Fix:** Add a lightweight runtime validator (zod or valibot) for at least this one security-relevant
boundary (session/current-user shape), and consider extending it to other externally-sourced response
shapes as time allows. Don't attempt to validate all ~100 API response types at once — this one is the
highest-value target since it gates auth.

**Verify:** Add a unit test that feeds a malformed session response through the validator and confirms it
fails clearly (throws/logs) rather than silently producing a malformed `CurrentUser`.

### 4.3 Replace the Playwright scaffold test and add real e2e coverage
**Problem:** `UI/tests/example.spec.ts` is still the untouched default Playwright template — it navigates to
`https://playwright.dev/` and checks its title, testing nothing about FolioTrace. `UI/.github/workflows/playwright.yml`
runs the full suite across chromium/firefox/webkit on every push/PR, meaning CI time is spent hitting an
external website. The only real test in the repo, `UI/tests/inputPolicy.spec.ts`, is a pure-logic unit test
for `$lib/inputPolicy.ts` — good, but there's zero browser-level coverage of auth redirect, any CRUD flow,
or the SSE-based update mechanism (`AggregateUpdateWatcher`).

**Fix:**
1. Delete `example.spec.ts` immediately (Phase 1-level urgency for this one specific deletion, since it's
   wasted CI time — feel free to do it alongside Phase 1 rather than waiting).
2. Add minimal real e2e coverage: unauthenticated-redirect-to-SSO, one full CRUD flow on a reference-data
   page (create/edit/delete an entity), and FIXTrace pagination (load page 2, confirm different rows).
   Scope beyond these three can be a follow-up; don't try to cover every route in one PR.

**Verify:** New tests pass locally via `npx playwright test`; CI run time doesn't regress (should improve,
since the external-site test is gone).

---

## Suggested execution order for Codex

1. Phase 1 (1.1 → 1.2) as one small PR, plus deleting `example.spec.ts` (4.3 step 1) — bundle these since
   they're all quick, low-risk fixes.
2. Phase 2 items 2.1 and 2.2 together as one mechanical PR (pure extraction, no behavior change).
3. Phase 2.3 (`api.ts` cleanup) as its own PR, done incrementally in batches — larger surface area, higher
   care needed even though individually low-risk.
4. Phase 3.2 (debounce) and 3.3 (visibility pause) together as one PR — small, independent, same theme.
5. Phase 3.4 (version cache TTL) can ride along with 3.2/3.3 or ship alone — trivial.
6. Phase 3.1 (server-side pagination) after confirming with the API-side owner whether the repository layer
   needs new pagination support first (may depend on API remediation plan task 2.4's pattern).
7. Phase 4.2 (session runtime validation) any time after Phase 1 — small, high-value, no dependencies.
8. Phase 4.1 (OpenAPI codegen) and 4.3 step 2 (e2e coverage) last — both are larger, standalone efforts;
   scope and sequence with the team rather than doing in one sitting.

Run `npm run check` and `npx playwright test` after every PR before moving to the next.
