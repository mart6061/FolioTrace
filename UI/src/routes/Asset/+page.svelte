<script lang="ts">
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { formatDisplayDateTime, formatTableDateTime, toApiDateTime } from '$lib/dates';
  import { holdingDateBasisOptions } from '$lib/valuationPreferences';
  import type { AggregateKind, Currency, HoldingHistoryEvent, TransactionReferenceEvent, ValuationHistoryEvent, ValuationItem } from '$lib/types';

  let { data } = $props();

  const valuationDependencyKinds: AggregateKind[] = [
    'HoldingPositions',
    'Holdings',
    'InstrumentValues',
    'FXs',
    'FXRates'
  ];

  let openHistoryHoldingID = $state('');
  let historyByHoldingID = $state<Record<string, { events: HoldingHistoryEvent[]; error: string; loading: boolean }>>({});
  let valuationHistory = $state<{ events: ValuationHistoryEvent[]; error: string; loading: boolean; open: boolean }>({
    events: [],
    error: '',
    loading: false,
    open: false
  });

  const valuations = $derived(data.valuations);
  const accounts = $derived(data.accounts?.items ?? []);
  const currencies = $derived(data.currencies?.items ?? []);
  const asOfSummary = $derived(data.auditDateTime && valuations ? formatDisplayDateTime(valuations.asOfDateTime) : 'now');

  function formatNumber(value: number | null | undefined, digits = 4) {
    if (value === null || value === undefined || !Number.isFinite(value))
      return '-';

    return new Intl.NumberFormat('en-GB', {
      maximumFractionDigits: digits,
      minimumFractionDigits: 0
    }).format(value);
  }

  function formatMoney(value: number | null | undefined, currency: string) {
    if (value === null || value === undefined || !Number.isFinite(value))
      return '-';

    return new Intl.NumberFormat('en-GB', {
      currency,
      maximumFractionDigits: 2,
      minimumFractionDigits: 2,
      style: 'currency'
    }).format(value);
  }

  function formatWeightPercent(value: number | null | undefined) {
    if (value === null || value === undefined || !Number.isFinite(value))
      return '-';

    return `${new Intl.NumberFormat('en-GB', {
      maximumFractionDigits: 2,
      minimumFractionDigits: 2
    }).format(value)}%`;
  }

  function holdingKindLabel(value: string) {
    switch (value) {
      case 'PositionMemo':
        return 'Position memo';
      case 'PositionCash':
        return 'Position cash';
      case 'PositionAsset':
        return 'Position asset';
      default:
        return value;
    }
  }

  function accountRowClass(item: ValuationItem) {
    return item.complete ? 'border-slate-100' : 'border-amber-200 bg-amber-50/70';
  }

  function currencyLabel(currency: Currency) {
    return currency.alphabeticCode;
  }

  function handleAccountChange(event: Event) {
    const select = event.currentTarget as HTMLSelectElement;
    const account = accounts.find((item) => item.accountID === select.value);

    if (!account?.bookCurrency)
      return;

    const currencySelect = select.form?.elements.namedItem('valuationCurrency') as HTMLSelectElement | null;

    if (currencySelect && [...currencySelect.options].some((option) => option.value === account.bookCurrency))
      currencySelect.value = account.bookCurrency;
  }

  function statusDetail(item: ValuationItem) {
    if (item.complete)
      return 'Complete: local price and FX are available, so book value is included in totals.';

    return `Incomplete: ${item.incompleteReason ?? 'price or FX data is missing'}. Book value is excluded from totals.`;
  }

  async function toggleHistory(holdingID: string) {
    if (openHistoryHoldingID === holdingID) {
      openHistoryHoldingID = '';
      delete historyByHoldingID[holdingID];
      return;
    }

    openHistoryHoldingID = holdingID;

    if (historyByHoldingID[holdingID])
      return;

    await loadHistory(holdingID);
  }

  async function loadHistory(holdingID: string) {
    historyByHoldingID[holdingID] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Reference/Holdings/History', window.location.origin);
      historyUrl.searchParams.set('holdingID', holdingID);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByHoldingID[holdingID] = {
        events: await response.json() as HoldingHistoryEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByHoldingID[holdingID] = {
        events: [],
        error: error instanceof Error ? error.message : 'Unable to load history.',
        loading: false
      };
    }
  }

  async function toggleValuationHistory() {
    valuationHistory.open = !valuationHistory.open;

    if (!valuationHistory.open || valuationHistory.events.length || valuationHistory.loading)
      return;

    await loadValuationHistory();
  }

  async function loadValuationHistory() {
    valuationHistory = { events: [], error: '', loading: true, open: true };

    try {
      const historyUrl = new URL('/Asset/History', window.location.origin);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      if (data.accountID)
        historyUrl.searchParams.set('accountID', data.accountID);

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      valuationHistory = {
        events: await response.json() as ValuationHistoryEvent[],
        error: '',
        loading: false,
        open: true
      };
    } catch (error) {
      valuationHistory = {
        events: [],
        error: error instanceof Error ? error.message : 'Unable to load valuation history.',
        loading: false,
        open: true
      };
    }
  }

  function holdingEventSummary(event: HoldingHistoryEvent) {
    if (isTransactionHistoryEvent(event))
      return '';

    if (event.$type === 'HoldingActiveModifiedEvent')
      return event.active ? 'Activated' : 'Deactivated';

    return [
      event.name,
      event.holdingKind,
      typeof event.default === 'boolean' ? event.default ? 'Default' : 'Non-default' : ''
    ].filter(Boolean).join(' - ');
  }

  function isTransactionHistoryEvent(event: HoldingHistoryEvent): event is TransactionReferenceEvent {
    return event.$type === 'TransactionCreditEvent' ||
      event.$type === 'TransactionDebitEvent' ||
      event.$type === 'TransactionCancellationEvent';
  }

  function transactionEventLabel(event: TransactionReferenceEvent) {
    if (event.$type === 'TransactionCreditEvent')
      return 'Credit';
    if (event.$type === 'TransactionDebitEvent')
      return 'Debit';
    if (event.$type === 'TransactionCancellationEvent')
      return 'Cancellation';

    return event.$type || 'Transaction';
  }

  function transactionEventSummary(event: TransactionReferenceEvent) {
    if (event.$type === 'TransactionCancellationEvent')
      return `Cancelled ${event.cancelledEventID ?? 'transaction'} from set ${event.eventSetID}`;

    return [
      `Quantity ${formatNumber(event.quantity)}`,
      `Book cost ${formatNumber(event.bookCost)}`,
      `Settlement ${formatTableDateTime(event.settlementDateTime)}`
    ].join(' - ');
  }

  function transactionEventDetail(event: TransactionReferenceEvent) {
    if (event.$type === 'TransactionCancellationEvent')
      return event.cancelledIDGroup?.length
        ? `Cancelled group ${event.cancelledIDGroup.join(', ')}`
        : '';

    return `Event set ${event.eventSetID}`;
  }

  function eventHoldingDetail(event: HoldingHistoryEvent) {
    const holdingID = 'holdingID' in event ? event.holdingID : '';
    const accountID = 'accountID' in event ? event.accountID : '';
    const instrumentID = 'instrumentID' in event ? event.instrumentID : '';

    return [
      holdingID ? `Holding ${holdingID}` : '',
      accountID ? `Account ${accountID}` : '',
      instrumentID ? `Instrument ${instrumentID}` : ''
    ].filter(Boolean).join(' - ');
  }
</script>

<svelte:head>
  <title>Asset | FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-content">
        <div class="page-header-main">
          <p class="page-kicker">Value</p>
          <div class="page-title-row">
            <h1 class="page-title">Asset</h1>
          </div>
        </div>
        <div class="page-header-aside">
          <button
            class="btn btn-secondary"
            onclick={toggleValuationHistory}
            type="button"
          >
            {valuationHistory.open ? 'Hide history' : 'History'}
          </button>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section grid gap-5">
    <form class="house-form grid gap-3 md:grid-cols-2 xl:grid-cols-[minmax(260px,0.9fr)_minmax(220px,1fr)_minmax(220px,1fr)_minmax(220px,1fr)_minmax(8rem,0.45fr)_auto]" method="GET">
      {#if data.auditDateTime}
        <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
      {/if}

      <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
        Valuation date
        <DateTimeInput class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="valuationDate" step="1" value={data.valuationDate} />
      </label>

      <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
        Account
        <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="accountID" onchange={handleAccountChange}>
          <option value="">All accounts</option>
          {#each accounts as account (account.accountID)}
            <option selected={account.accountID === data.accountID} value={account.accountID}>{account.name}</option>
          {/each}
        </select>
      </label>

      <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
        Holding basis
        <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="holdingDateBasis">
          {#each holdingDateBasisOptions as option (option.value)}
            <option selected={option.value === data.holdingDateBasis} value={option.value}>{option.label}</option>
          {/each}
        </select>
      </label>

      <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
        Price basis
        <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="instrumentPriceBasis">
          {#each data.instrumentPriceBasisOptions as option (option)}
            <option selected={option === data.instrumentPriceBasis} value={option}>{option}</option>
          {/each}
        </select>
      </label>

      <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
        Currency
        <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="valuationCurrency">
          {#if currencies.length}
            {#each currencies as currency (currency.alphabeticCode)}
              <option selected={currency.alphabeticCode === data.valuationCurrency} value={currency.alphabeticCode}>{currencyLabel(currency)}</option>
            {/each}
          {:else}
            <option value={data.valuationCurrency}>{data.valuationCurrency}</option>
          {/if}
        </select>
      </label>

      <button class="h-10 w-full self-end rounded-md bg-teal-700 px-4 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 md:w-auto" type="submit">Apply</button>
    </form>

    {#if data.error}
      <div class="status-panel status-panel-error" role="status">{data.error}</div>
    {:else if valuations}
      {#each valuationDependencyKinds as aggregateKind (aggregateKind)}
        <AggregateUpdateWatcher {aggregateKind} valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={valuations.lastEventID} />
      {/each}

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{valuations.accounts.length}</span> account valuations</div>
        <div>Valuation {formatDisplayDateTime(valuations.valuationDateTime)} | As-of {asOfSummary}</div>
        <div>Book value {formatMoney(valuations.totals.bookValue, valuations.valuationCurrency)} | Book cost {formatMoney(valuations.totals.bookCost, valuations.valuationCurrency)}</div>
        <div>{valuations.totals.incompleteCount} incomplete rows excluded from book value</div>
      </div>

      {#if valuationHistory.open}
        <section>
          {#if valuationHistory.loading}
            <div class="text-sm text-slate-600">Loading history...</div>
          {:else if valuationHistory.error}
            <div class="status-panel status-panel-error">{valuationHistory.error}</div>
          {:else}
            <HistoryEventsCard
              eventDateTime={valuations.valuationDateTime ?? data.valuationDate}
              asAtDateTime={data.auditDateTime}
              events={valuationHistory.events}
              emptyMessage="No excluded holding or transaction events for this valuation."
            />
          {/if}
        </section>
      {/if}

      {#each valuations.accounts as account (account.accountID)}
        <section class="house-card grid gap-3">
          <div class="house-card-header">
            <div>
              <h2 class="house-heading">{account.accountName}</h2>
              <p class="house-muted text-sm">Book currency {account.bookCurrency} | Valuation currency {account.valuationCurrency}</p>
            </div>
            <div class="text-right text-sm">
              <div class="font-semibold text-slate-950">{formatMoney(account.totals.bookValue, account.valuationCurrency)}</div>
              <div class="house-muted">Cost {formatMoney(account.totals.bookCost, account.valuationCurrency)}</div>
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-slate-200 text-sm">
              <thead class="bg-slate-100 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
                <tr>
                  <th class="px-3 py-2">Name</th>
                  <th class="px-3 py-2">Kind</th>
                  <th class="px-3 py-2 text-right">Quantity</th>
                  <th class="px-3 py-2 text-right">Weight %</th>
                  <th class="px-3 py-2 text-right">Local price</th>
                  <th class="px-3 py-2 text-right">FX</th>
                  <th class="px-3 py-2 text-right">Quote price</th>
                  <th class="px-3 py-2 text-right">Book value</th>
                  <th class="px-3 py-2 text-right">Book cost</th>
                  <th class="px-3 py-2 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 bg-white">
                {#each account.items as item (item.holdingID)}
                  <tr class={accountRowClass(item)}>
                    <td class="px-3 py-2">
                      <div class="font-medium text-slate-950">{item.name}</div>
                      <div class="text-xs text-slate-500">{item.instrumentName}</div>
                    </td>
                    <td class="px-3 py-2 text-slate-700">{holdingKindLabel(item.holdingKind)}</td>
                    <td class="px-3 py-2 text-right font-mono">{formatNumber(item.quantity)}</td>
                    <td class="px-3 py-2 text-right font-mono">{formatWeightPercent(item.weightPercent)}</td>
                    <td class="px-3 py-2 text-right font-mono">{formatMoney(item.localPrice, item.priceCurrency)}</td>
                    <td class="px-3 py-2 text-right font-mono">
                      {#if item.fxRate}
                        <div>{formatNumber(item.fxRate, 8)}</div>
                        <div class="text-xs text-slate-500">{item.fxDisplayPair ?? item.fxPair ?? 'Same currency'}</div>
                      {:else}
                        -
                      {/if}
                    </td>
                    <td class="px-3 py-2 text-right font-mono">{formatMoney(item.quotePrice, item.valuationCurrency)}</td>
                    <td class="px-3 py-2 text-right font-mono font-semibold text-slate-950">{formatMoney(item.bookValue, item.valuationCurrency)}</td>
                    <td class="px-3 py-2 text-right font-mono">{formatMoney(item.bookCost, item.valuationCurrency)}</td>
                    <td class="px-3 py-2 text-right">
                      <div class="flex items-center justify-end gap-2">
                        <button class="btn btn-secondary" onclick={() => toggleHistory(item.holdingID)} type="button">
                          {openHistoryHoldingID === item.holdingID ? 'Hide' : 'History'}
                        </button>
                        <button
                          aria-label={statusDetail(item)}
                          class={`group relative inline-flex h-6 w-6 items-center justify-center rounded-full border text-xs font-semibold ${
                            item.complete
                              ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
                              : 'border-amber-300 bg-amber-100 text-amber-900'
                          }`}
                          title={statusDetail(item)}
                          type="button"
                        >
                          i
                          <span class="pointer-events-none absolute right-0 top-7 z-20 hidden w-64 rounded-md border border-slate-200 bg-white px-3 py-2 text-left text-xs font-medium leading-5 text-slate-700 shadow-lg group-hover:block group-focus:block">
                            {statusDetail(item)}
                          </span>
                        </button>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryHoldingID === item.holdingID}
                    {@const history = historyByHoldingID[item.holdingID]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="10">
                        <div>
                          {#if history?.loading}
                            <div class="text-sm text-slate-600">Loading history...</div>
                          {:else if history?.error}
                            <div class="status-panel status-panel-error">{history.error}</div>
                          {:else}
                            <HistoryEventsCard
                              eventDateTime={valuations.valuationDateTime ?? data.valuationDate}
                              asAtDateTime={data.auditDateTime}
                              events={history?.events ?? []}
                              emptyMessage="No history found for this holding."
                            />
                          {/if}
                        </div>
                      </td>
                    </tr>
                  {/if}
                {/each}
              </tbody>
            </table>
          </div>
        </section>
      {/each}
    {/if}
  </section>
</main>
