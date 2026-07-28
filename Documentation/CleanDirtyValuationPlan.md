# Clean and dirty valuation

Plan for adding a clean/dirty convention toggle to Valuation and the Asset Viewer.

## The requirement

Valuation and Asset Viewer gain a filter control toggling **Clean** and **Dirty**.

- **Dirty** — the price shown is the dirty price, and that is what values the position.
- **Clean** — the price shown is clean, with an accrued interest amount on a sub-line beneath it.
- **Clean totals** — a clean subtotal, then total accrued interest in the valuation currency, then the final total.

The final total always includes accrued interest.

## The property that makes this safe

Clean and dirty are two routes to the same number:

```
cleanSubtotal + totalAccruedInterest == finalTotal == dirtyTotal
```

The toggle is a presentation choice, not a different valuation. **Assert this in a test.** If the two paths ever disagree the toggle has become a correctness bug rather than a display option, and nothing else in the design will catch that.

## Decisions taken

**Book cost reflects the dirty price.** Valuation and profit and loss both work in dirty terms, so unrealised profit and loss compares like with like. This removes the mismatch that would otherwise overstate it by the accrued interest, and it means the display toggle stays a display toggle — it does not change what profit and loss computes.

**Tickets do not yet capture this.** Bonds are conventionally quoted clean while settlement is dirty, so a ticket needs to record which convention its traded price uses and derive the other. Until it does, book cost for a bond is only as dirty as whatever the settlement amount happened to contain. Treat that as a known gap rather than something this change fixes.

**Account beats User in the input policy resolver, deliberately.** A user's own setting is the weakest for format and negativity. Expected to change as other settings arrive; deferred for now, not a defect.

**The convention persists per user.** It belongs in `UserValuationPreferences` next to `holdingDateBasis` and `showZeroBalances`. Add it in the same change: that aggregate, its event, and the preferences form all have to be reopened otherwise.

**The accrued sub-line appears only where accrued interest is non-null.** In practice that means bonds. Rows are ragged in a mixed portfolio, which is accepted in exchange for never printing a meaningless zero under an equity.

**Reports carry the convention too.** A printed report should match what the screen showed, so report valuation columns gain it in this change rather than later. This widens the work to `ReportTools` and the report node model.

**Existing bond book costs are taken to be dirty already.** Proceeding on that basis. If it turns out to be false, unrealised profit and loss on bonds is overstated by the accrued until tickets capture the convention — worth a spot check on a seeded bond early, but not a blocker.

## What already exists

Do not rebuild these.

- `InstrumentValue.SelectPrice(basis, includeAccruedInterest = false)` — already takes the flag, already plumbed, currently never passed `true`.
- `InstrumentValue.DirtyQuote` — clean quote plus accrued, derived rather than stored.
- `InstrumentQuote.Add(amount)` — adds a constant to every quote, ordering preserved by construction.
- `InstrumentIncomeFixedIncome.AccruedInterest` — an `InstrumentPrice`, so **per unit**, exactly like a price.

The two axes are already separate: `InstrumentPriceBasis` (Bid/Mid/Ask/Last/NAV) chooses *which quote*; the accrual flag chooses *clean or dirty*. Keep them separate in the UI too.

## The two things most likely to be got wrong

**Accrued interest is stored per unit, like a price.** It is an `InstrumentPrice`, not a position amount. So it follows the price path exactly: `accrued × quantity`, then FX into the valuation currency. Treating it as an already-scaled amount will look plausible on a single-unit holding and be wrong everywhere else.

**Accrued must convert at the same FX rate the price used.** `SelectFX` selects a rate from the basis. Use the identical selection for accrued, not a fresh lookup and not the mid rate. If the two diverge, clean and dirty drift apart by the spread — and they will still each look internally consistent, so only the reconciliation test above will catch it.

## Backend

`Valuations.cs` and `ProfitLosses.cs` both call `SelectPrice(instrumentPriceBasis)` and both carry a comment marking the spot.

1. **Request and aggregate** — add the convention alongside `InstrumentPriceBasis` on `Valuations` (a bool reads fine; an enum reads better if a third convention is ever plausible).
2. **Profit and loss values dirty**, per the decision above, and does not take the display toggle. Thread the flag as a constant there rather than from the request.
3. **Per item** — `ValuationItem` needs the accrued amount, computed as described above.
4. **Totals** — `ValuationTotals` currently has `bookValue`, `bookCost`, `incompleteCount`. Add the clean subtotal and the accrued total. Compute the final total once and derive the other two from it, rather than summing twice.
5. **Equities and cash contribute zero.** Only `InstrumentPriceFixedIncome` pairs with `InstrumentIncomeFixedIncome`.

## UI

- **Toggle** — a `PillGroup` alongside the existing price basis selector, matching the filter idiom already on those pages. Two controls, because they are two orthogonal axes.
- **Plumbing** — query parameter, page `load`, API call. Follow how `instrumentPriceBasis` and `holdingDateBasis` already flow.
- **Sub-line** — accrued beneath the price in Clean mode only.
- **Totals block** — clean subtotal, accrued total, final total. Label the accrued row with the valuation currency, since it is converted and the per-item accrued is not.

Pages: the valuation view and `Asset/AssetExperience.svelte`.

## Scope

Everything needed is decided. The work spans:

- `Valuations` and `ProfitLosses`, plus the valuation request and `ValuationItem`/`ValuationTotals`
- `UserValuationPreferences` and its event, plus the preferences form
- The valuation view and `Asset/AssetExperience.svelte`
- Report nodes and `ReportTools`

Deliberately out of scope: the ticket change that captures whether a bond's traded price is clean or dirty. That is a separate piece and this one does not depend on it.

## Sequence

Backend first — it is provable without any UI:

1. Thread the flag through `Valuations`, with a test asserting `clean + accrued == dirty` on a seeded bond.
2. Add the per-item accrued and the totals, with a test that the three totals reconcile.
3. Pin `ProfitLosses` to dirty.
4. Add the preference to `UserValuationPreferences` and its event.
5. Then the UI: toggle, sub-line, totals block, and the preferences form.
6. Then reports: the convention on report nodes and `ReportTools`.

The ticket work — capturing whether a bond's traded price is clean or dirty — is a separate change and is not required to land this one.

## Verifying

The database has seeded bonds with clean bid/mid/ask quotes, so this is testable end to end without new data. Accrued interest is editable on the Instrument Values page, which is how to set up a bond with a known accrual and check the totals reconcile in the browser.

**Stop the API before running `dotnet test`.** Its file locks make the build fail at the *copy* step, which a `grep 'error CS'` will miss, and the tests then run against a stale `Test.dll` and report a pass that means nothing.
