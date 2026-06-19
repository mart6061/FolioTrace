<script lang="ts">
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { MultiSelect, PillGroup } from '$lib/components/forms';
  import { formatTableDateTime, toApiDateTime } from '$lib/dates';
  import { holdingDateBasisOptions } from '$lib/valuationPreferences';
  import { tick } from 'svelte';
  import { SvelteMap } from 'svelte/reactivity';
  import type { Account, AccountValuation, AggregateKind, Currency, HoldingHistoryEvent, TransactionReferenceEvent, ValuationItem } from '$lib/types';

  let { data } = $props();

  const valuationDependencyKinds: AggregateKind[] = [
    'HoldingPositions',
    'Holdings',
    'InstrumentValues',
    'FXs',
    'FXRates'
  ];
  const assetFilterFormID = 'asset-filter-form';

  let openHistoryHoldingID = $state('');
  let accountDropdownOpen = $state(false);
  let accountFilterText = $state('');
  let selectedAccountIDOverrides = $state<string[] | null>(null);
  let displayMode: 'Discrete' | 'Aggregate' = $derived(data.assetViewMode);
  let expandedAggregateAssetID = $state('');
  let historyByHoldingID = $state<Record<string, { events: HoldingHistoryEvent[]; error: string; loading: boolean }>>({});

  const valuations = $derived(data.valuations);
  const accounts = $derived(data.accounts?.items ?? []);
  const currencies = $derived(data.currencies?.items ?? []);
  const filteredAccounts = $derived(accounts.filter((account) => accountMatchesFilter(accountFilterText, account)));
  const selectedAccountIDs = $derived(validSelectedAccountIDs());
  const selectedAccountSummary = $derived(accountSummary());
  const selectedBookCurrencies = $derived(selectedBookCurrencyList());
  const aggregateRows = $derived.by(() => valuations ? aggregateAssetRows(valuations.accounts) : []);
  const aggregateBookCostUnavailable = $derived(displayMode === 'Aggregate' && selectedBookCurrencies.length > 1);
  const displayModeOptions = [
    { label: 'Discrete', value: 'Discrete' },
    { label: 'Aggregate', value: 'Aggregate' }
  ];

  type AggregateAssetRow = {
    assetID: string;
    accountCount: number;
    bookCost: number | null;
    bookCostUnavailable: boolean;
    bookValue: number | null;
    complete: boolean;
    fxDisplayPair?: string | null;
    fxPair?: string | null;
    fxRate?: number | null;
    holdings: ValuationItem[];
    holdingCount: number;
    holdingKind: string;
    incompleteReason?: string | null;
    instrumentName: string;
    localPrice?: number | null;
    name: string;
    priceCurrency: string;
    quantity: number;
    quotePrice?: number | null;
    valuationCurrency: string;
    weightPercent: number | null;
  };

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
        return value
          .replace(/([a-z])([A-Z])/g, '$1 $2')
          .replace(/^Nominal /, '');
    }
  }

  function accountRowClass(item: { complete: boolean }) {
    return item.complete ? 'border-slate-100' : 'border-amber-200 bg-amber-50/70';
  }

  function currencyLabel(currency: Currency) {
    return currency.alphabeticCode;
  }

  function accountMatchesFilter(filterText: string, account: Account) {
    const filter = filterText.trim().toLocaleLowerCase();

    if (!filter)
      return true;

    return [account.name, account.formalName, account.bookCurrency, account.active ? 'active' : 'inactive']
      .some((value) => value.toLocaleLowerCase().includes(filter));
  }

  function accountSummary() {
    if (selectedAccountIDs.length === accounts.length)
      return 'All accounts';

    if (!selectedAccountIDs.length)
      return 'No accounts selected';

    const selectedNames = accounts
      .filter((account) => selectedAccountIDs.includes(account.accountID))
      .map((account) => account.name);

    if (selectedNames.length <= 2)
      return selectedNames.join(', ');

    return `${selectedNames.length} accounts selected`;
  }

  function validSelectedAccountIDs() {
    const accountIDSet = new Set(accounts.map((account) => account.accountID));
    const accountIDs = selectedAccountIDOverrides ?? data.accountIDs ?? [];
    return accountIDs.filter((accountID) => accountIDSet.has(accountID));
  }

  function selectedBookCurrencyList() {
    const selectedAccountIDSet = new Set(selectedAccountIDs);
    const selectedAccounts = selectedAccountIDs.length
      ? accounts.filter((account) => selectedAccountIDSet.has(account.accountID))
      : accounts;

    return [...new Set(selectedAccounts.map((account) => account.bookCurrency).filter(Boolean))];
  }

  function submitFilterChange() {
    const form = document.getElementById(assetFilterFormID);

    if (form instanceof HTMLFormElement)
      form.requestSubmit();
  }

  async function toggleAccountSelection(accountID: string, checked: boolean, event: Event) {
    if (!checked && selectedAccountIDs.length === 1 && selectedAccountIDs.includes(accountID))
      return;

    const nextSelectedAccountIDs = checked
      ? [...new Set([...selectedAccountIDs, accountID])]
      : selectedAccountIDs.filter((selectedAccountID) => selectedAccountID !== accountID);

    selectedAccountIDOverrides = nextSelectedAccountIDs;
    syncCurrencyForSelection(event, nextSelectedAccountIDs);
    await tick();
    submitFilterChange();
  }

  async function selectAllAccounts(event: Event) {
    const nextSelectedAccountIDs = accounts.map((account) => account.accountID);
    selectedAccountIDOverrides = nextSelectedAccountIDs;
    syncCurrencyForSelection(event, nextSelectedAccountIDs);
    await tick();
    submitFilterChange();
  }

  function syncCurrencyForSelection(event: Event, nextSelectedAccountIDs = selectedAccountIDs) {
    if (nextSelectedAccountIDs.length !== 1)
      return;

    const account = accounts.find((item) => item.accountID === nextSelectedAccountIDs[0]);

    if (!account?.bookCurrency)
      return;

    const control = event.currentTarget as HTMLInputElement | HTMLButtonElement;
    const currencySelect = control.form?.elements.namedItem('valuationCurrency') as HTMLSelectElement | null;

    if (currencySelect && [...currencySelect.options].some((option) => option.value === account.bookCurrency))
      currencySelect.value = account.bookCurrency;
  }

  function aggregateAssetRows(accountValuations: AccountValuation[]): AggregateAssetRow[] {
    const groups = new SvelteMap<string, {
      accountIDs: Set<string>;
      bookCost: number;
      bookCurrencies: Set<string>;
      bookValue: number;
      complete: boolean;
      fxDisplayPair?: string | null;
      fxPair?: string | null;
      fxRate?: number | null;
      holdings: ValuationItem[];
      holdingCount: number;
      holdingKind: string;
      incompleteCount: number;
      incompleteReasons: Set<string>;
      instrumentName: string;
      localPrice?: number | null;
      name: string;
      priceCurrency: string;
      quantity: number;
      quotePrice?: number | null;
      valuationCurrency: string;
    }>();

    for (const account of accountValuations) {
      for (const item of account.items) {
        const assetID = item.instrumentID || item.name || item.holdingID;
        const group = groups.get(assetID) ?? {
          accountIDs: new Set<string>(),
          bookCost: 0,
          bookCurrencies: new Set<string>(),
          bookValue: 0,
          complete: true,
          holdings: [],
          holdingCount: 0,
          holdingKind: holdingKindLabel(item.holdingKind),
          incompleteCount: 0,
          incompleteReasons: new Set<string>(),
          instrumentName: item.instrumentName,
          name: item.name,
          priceCurrency: item.priceCurrency,
          quantity: 0,
          valuationCurrency: item.valuationCurrency
        };

        group.accountIDs.add(account.accountID);
        group.bookCurrencies.add(account.bookCurrency);
        group.holdings.push(item);
        group.holdingCount += 1;
        group.quantity += Number.isFinite(item.quantity) ? item.quantity : 0;
        group.bookValue += Number.isFinite(item.bookValue) ? item.bookValue ?? 0 : 0;
        group.bookCost += Number.isFinite(item.bookCost) ? item.bookCost : 0;
        group.complete = group.complete && item.complete;

        if (!item.complete) {
          group.incompleteCount += 1;
          group.incompleteReasons.add(item.incompleteReason ?? 'price or FX data is missing');
        }

        group.localPrice ??= item.localPrice;
        group.quotePrice ??= item.quotePrice;
        group.fxRate ??= item.fxRate;
        group.fxPair ??= item.fxPair;
        group.fxDisplayPair ??= item.fxDisplayPair;
        groups.set(assetID, group);
      }
    }

    const totalBookValue = [...groups.values()].reduce((total, group) => total + group.bookValue, 0);

    return [...groups.entries()]
      .map(([assetID, group]) => ({
        assetID,
        accountCount: group.accountIDs.size,
        bookCost: group.bookCurrencies.size > 1 ? null : group.bookCost,
        bookCostUnavailable: group.bookCurrencies.size > 1,
        bookValue: group.bookValue,
        complete: group.complete,
        fxDisplayPair: group.fxDisplayPair,
        fxPair: group.fxPair,
        fxRate: group.fxRate,
        holdings: [...group.holdings].sort((left, right) =>
          left.accountName.localeCompare(right.accountName) ||
          left.holdingName.localeCompare(right.holdingName)
        ),
        holdingCount: group.holdingCount,
        holdingKind: group.holdingKind,
        incompleteReason: group.incompleteCount
          ? `${group.incompleteCount} incomplete holding${group.incompleteCount === 1 ? '' : 's'}: ${[...group.incompleteReasons].join(', ')}`
          : null,
        instrumentName: group.instrumentName,
        localPrice: group.localPrice,
        name: group.name,
        priceCurrency: group.priceCurrency,
        quantity: group.quantity,
        quotePrice: group.quotePrice,
        valuationCurrency: group.valuationCurrency,
        weightPercent: totalBookValue > 0 ? group.bookValue / totalBookValue * 100 : null
      }))
      .sort((left, right) => left.name.localeCompare(right.name));
  }

  function toggleAggregateDetail(assetID: string) {
    expandedAggregateAssetID = expandedAggregateAssetID === assetID ? '' : assetID;
  }

  function assetDisplayName(item: { holdingName?: string | null; instrumentName: string; name?: string | null }) {
    const ticker = inferredTicker(item);

    return {
      instrumentName: item.instrumentName || item.name || '',
      ticker
    };
  }

  function inferredTicker(item: { holdingName?: string | null; instrumentName: string; name?: string | null }) {
    const instrumentName = (item.instrumentName || '').trim();
    const candidates = [
      item.holdingName ?? '',
      item.name ?? ''
    ];

    for (const candidate of candidates) {
      const normalized = candidate.trim();
      if (!normalized)
        continue;

      const withoutInstrument = instrumentName && normalized.endsWith(instrumentName)
        ? normalized.slice(0, -instrumentName.length).trim()
        : normalized;
      const ticker = withoutInstrument
        .replace(/^(asset|holding|position)\s+/i, '')
        .trim();

      if (ticker && ticker !== instrumentName && /^[A-Z0-9][A-Z0-9.-]{0,14}$/i.test(ticker))
        return ticker.toUpperCase();
    }

    return '';
  }

  function formatBookCost(value: number | null | undefined, currency: string, unavailable = false) {
    return unavailable ? 'n/a' : formatMoney(value, currency);
  }

  function fxPairLabel(item: { fxDisplayPair?: string | null; fxPair?: string | null }) {
    return item.fxDisplayPair ?? item.fxPair ?? '';
  }

  function isSameCurrencyFx(item: { fxDisplayPair?: string | null; fxPair?: string | null }) {
    const label = fxPairLabel(item).trim().toLocaleLowerCase();
    return !label || label === 'same currency';
  }

  function statusDetail(item: { complete: boolean; incompleteReason?: string | null }) {
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
            <BookmarkButton />
          </div>
        </div>
      </div>

      <form id={assetFilterFormID} class="house-form asset-filter-form" method="GET">
        {#if data.auditDateTime}
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
        {/if}

        <label class="asset-filter-field asset-filter-date grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput fullWidth name="valuationDate" onchange={submitFilterChange} step="1" value={data.valuationDate} />
        </label>

        <div class="asset-filter-field asset-filter-account grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Account
          <MultiSelect bind:open={accountDropdownOpen} class="asset-account-select" summary={selectedAccountSummary}>
            <div class="asset-account-filter-row">
              <input
                bind:value={accountFilterText}
                class="house-control house-control-md house-control-full"
                placeholder="Search accounts"
                type="search"
              />
              <button class="house-button house-button-secondary house-button-sm" onclick={selectAllAccounts} type="button">All</button>
            </div>
            <div class="asset-account-options">
              {#each filteredAccounts as account (account.accountID)}
                <label class="house-checkbox-option asset-account-option">
                  <input
                    checked={selectedAccountIDs.includes(account.accountID)}
                    name="accountID"
                    onchange={(event) => toggleAccountSelection(account.accountID, event.currentTarget.checked, event)}
                    type="checkbox"
                    value={account.accountID}
                  />
                  <span>
                    <strong>{account.name}</strong>
                    <small>{account.formalName} - {account.bookCurrency} - {account.active ? 'Active' : 'Inactive'}</small>
                  </span>
                </label>
              {:else}
                <div class="asset-account-empty">No accounts match</div>
              {/each}
            </div>
          </MultiSelect>
        </div>

        <label class="asset-filter-field asset-filter-basis grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Holding basis
          <select class="house-control house-control-md house-control-full" name="holdingDateBasis" onchange={submitFilterChange}>
            {#each holdingDateBasisOptions as option (option.value)}
              <option selected={option.value === data.holdingDateBasis} value={option.value}>{option.label}</option>
            {/each}
          </select>
        </label>

        <label class="asset-filter-field asset-filter-price grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Price basis
          <select class="house-control house-control-md house-control-full" name="instrumentPriceBasis" onchange={submitFilterChange}>
            {#each data.instrumentPriceBasisOptions as option (option)}
              <option selected={option === data.instrumentPriceBasis} value={option}>{option}</option>
            {/each}
          </select>
        </label>

        <label class="asset-filter-field asset-filter-currency grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Currency
          <select class="house-control house-control-md house-control-full" name="valuationCurrency" onchange={submitFilterChange}>
            {#if currencies.length}
              {#each currencies as currency (currency.alphabeticCode)}
                <option selected={currency.alphabeticCode === data.valuationCurrency} value={currency.alphabeticCode}>{currencyLabel(currency)}</option>
              {/each}
            {:else}
              <option value={data.valuationCurrency}>{data.valuationCurrency}</option>
            {/if}
          </select>
        </label>

        <div class="asset-filter-field asset-filter-view grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          View
          <PillGroup
            ariaLabel="Asset view mode"
            bind:value={displayMode}
            class="asset-mode-toggle"
            compact
            name="assetViewMode"
            options={displayModeOptions}
          />
        </div>
      </form>
    </div>
  </section>

  <section class="page-container page-section grid gap-5">

    {#if data.error}
      <div class="status-panel status-panel-error" role="status">{data.error}</div>
    {:else if valuations}
      {#each valuationDependencyKinds as aggregateKind (aggregateKind)}
        <AggregateUpdateWatcher {aggregateKind} valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={valuations.lastEventID} />
      {/each}

      {#if displayMode === 'Aggregate'}
        <section class="house-card grid gap-3">
          <div class="house-card-header">
            <div>
              <h2 class="house-heading">Aggregate assets</h2>
              <p class="house-muted text-sm">{aggregateRows.length} assets across {valuations.accounts.length} accounts</p>
            </div>
            <div class="text-right text-sm">
              <div class="font-semibold text-slate-950">{formatMoney(valuations.totals.bookValue, valuations.valuationCurrency)}</div>
              <div class="house-muted">Cost {formatBookCost(valuations.totals.bookCost, valuations.valuationCurrency, aggregateBookCostUnavailable)}</div>
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="asset-table min-w-full divide-y divide-slate-200 text-sm">
              <thead class="bg-slate-100 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
                <tr>
                  <th class="px-3 py-2">Name</th>
                  <th class="px-3 py-2">Kind</th>
                  <th class="px-3 py-2 text-right">Quantity</th>
                  <th class="px-3 py-2 text-right">Weight %</th>
                  <th class="asset-price-column px-3 py-2 text-right">Local price</th>
                  <th class="asset-currency-column px-3 py-2 text-right">FX</th>
                  <th class="asset-price-column px-3 py-2 text-right">Quote price</th>
                  <th class="px-3 py-2 text-right">Book value</th>
                  <th class="px-3 py-2 text-right">Book cost</th>
                  <th class="px-3 py-2 text-right">Status</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 bg-white">
                {#each aggregateRows as item (item.assetID)}
                  {@const itemDisplay = assetDisplayName(item)}
                  <tr class={accountRowClass(item)}>
                    <td class="px-3 py-2">
                      <div class="asset-name-title">
                        <span>{itemDisplay.instrumentName}</span>
                        {#if itemDisplay.ticker}
                          <span class="asset-name-separator">:</span>
                          <span class="asset-ticker">{itemDisplay.ticker}</span>
                        {/if}
                      </div>
                      <button class="asset-detail-link" onclick={() => toggleAggregateDetail(item.assetID)} type="button">
                        {item.holdingCount} holding{item.holdingCount === 1 ? '' : 's'} | {item.accountCount} account{item.accountCount === 1 ? '' : 's'}
                      </button>
                    </td>
                    <td class="px-3 py-2 text-slate-700">{item.holdingKind}</td>
                    <td class="px-3 py-2 text-right font-mono">{formatNumber(item.quantity)}</td>
                    <td class="px-3 py-2 text-right font-mono">{formatWeightPercent(item.weightPercent)}</td>
                    <td class="asset-price-column px-3 py-2 text-right font-mono">{formatMoney(item.localPrice, item.priceCurrency)}</td>
                    <td class="asset-currency-column px-3 py-2 text-right font-mono">
                      {#if item.fxRate}
                        {#if isSameCurrencyFx(item)}
                          <div class="text-xs text-slate-500">Same currency</div>
                        {:else}
                          <div>{formatNumber(item.fxRate, 8)}</div>
                          <div class="text-xs text-slate-500">{fxPairLabel(item)}</div>
                        {/if}
                      {:else}
                        -
                      {/if}
                    </td>
                    <td class="asset-price-column px-3 py-2 text-right font-mono">{formatMoney(item.quotePrice, item.valuationCurrency)}</td>
                    <td class="px-3 py-2 text-right font-mono font-semibold text-slate-950">{formatMoney(item.bookValue, item.valuationCurrency)}</td>
                    <td class="px-3 py-2 text-right font-mono">{formatBookCost(item.bookCost, item.valuationCurrency, item.bookCostUnavailable)}</td>
                    <td class="px-3 py-2 text-right">
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
                    </td>
                  </tr>
                  {#if expandedAggregateAssetID === item.assetID}
                    {#each item.holdings as holding, index (holding.holdingID)}
                      <tr class="asset-detail-row">
                        <td class="px-3 py-2">
                          <div class="asset-detail-name">
                            <span class="asset-detail-account">{holding.accountName}</span>
                            {#if holding.holdingName && holding.holdingName !== holding.accountName}
                              <span class="asset-detail-holding">{holding.holdingName}</span>
                            {/if}
                          </div>
                        </td>
                        <td class="px-3 py-2 text-slate-600">{holdingKindLabel(holding.holdingKind)}</td>
                        <td class="px-3 py-2 text-right font-mono">{formatNumber(holding.quantity)}</td>
                        <td class="px-3 py-2 text-right font-mono">{formatWeightPercent(holding.weightPercent)}</td>
                        <td class="asset-price-column px-3 py-2 text-right font-mono">{formatMoney(holding.localPrice, holding.priceCurrency)}</td>
                        <td class="asset-currency-column px-3 py-2 text-right font-mono">
                          {#if holding.fxRate}
                            {#if isSameCurrencyFx(holding)}
                              <div class="text-xs text-slate-500">Same currency</div>
                            {:else}
                              <div>{formatNumber(holding.fxRate, 8)}</div>
                              <div class="text-xs text-slate-500">{fxPairLabel(holding)}</div>
                            {/if}
                          {:else}
                            -
                          {/if}
                        </td>
                        <td class="asset-price-column px-3 py-2 text-right font-mono">{formatMoney(holding.quotePrice, holding.valuationCurrency)}</td>
                        <td class="px-3 py-2 text-right font-mono">{formatMoney(holding.bookValue, holding.valuationCurrency)}</td>
                        <td class="px-3 py-2 text-right font-mono">{formatMoney(holding.bookCost, holding.valuationCurrency)}</td>
                        <td class="px-3 py-2 text-right">
                          {#if index === 0}
                            <button class="asset-detail-close" aria-label="Close aggregate detail" onclick={() => toggleAggregateDetail(item.assetID)} type="button">
                              <svg aria-hidden="true" viewBox="0 0 20 20">
                                <path d="m5 5 10 10M15 5 5 15" />
                              </svg>
                            </button>
                          {/if}
                        </td>
                      </tr>
                    {/each}
                  {/if}
                {/each}
              </tbody>
            </table>
          </div>
        </section>
      {:else}
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
              <table class="asset-table min-w-full divide-y divide-slate-200 text-sm">
                <thead class="bg-slate-100 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
                  <tr>
                    <th class="px-3 py-2">Name</th>
                    <th class="px-3 py-2">Kind</th>
                    <th class="px-3 py-2 text-right">Quantity</th>
                    <th class="px-3 py-2 text-right">Weight %</th>
                    <th class="asset-price-column px-3 py-2 text-right">Local price</th>
                    <th class="asset-currency-column px-3 py-2 text-right">FX</th>
                    <th class="asset-price-column px-3 py-2 text-right">Quote price</th>
                    <th class="px-3 py-2 text-right">Book value</th>
                    <th class="px-3 py-2 text-right">Book cost</th>
                    <th class="px-3 py-2 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100 bg-white">
                  {#each account.items as item (item.holdingID)}
                    {@const itemDisplay = assetDisplayName(item)}
                    <tr class={accountRowClass(item)}>
                      <td class="px-3 py-2">
                        <div class="asset-name-title">
                          <span>{itemDisplay.instrumentName}</span>
                          {#if itemDisplay.ticker}
                            <span class="asset-name-separator">:</span>
                            <span class="asset-ticker">{itemDisplay.ticker}</span>
                          {/if}
                        </div>
                      </td>
                      <td class="px-3 py-2 text-slate-700">{holdingKindLabel(item.holdingKind)}</td>
                      <td class="px-3 py-2 text-right font-mono">{formatNumber(item.quantity)}</td>
                      <td class="px-3 py-2 text-right font-mono">{formatWeightPercent(item.weightPercent)}</td>
                      <td class="asset-price-column px-3 py-2 text-right font-mono">{formatMoney(item.localPrice, item.priceCurrency)}</td>
                      <td class="asset-currency-column px-3 py-2 text-right font-mono">
                        {#if item.fxRate}
                          {#if isSameCurrencyFx(item)}
                            <div class="text-xs text-slate-500">Same currency</div>
                          {:else}
                            <div>{formatNumber(item.fxRate, 8)}</div>
                            <div class="text-xs text-slate-500">{fxPairLabel(item)}</div>
                          {/if}
                        {:else}
                          -
                        {/if}
                      </td>
                      <td class="asset-price-column px-3 py-2 text-right font-mono">{formatMoney(item.quotePrice, item.valuationCurrency)}</td>
                      <td class="px-3 py-2 text-right font-mono font-semibold text-slate-950">{formatMoney(item.bookValue, item.valuationCurrency)}</td>
                      <td class="px-3 py-2 text-right font-mono">{formatMoney(item.bookCost, item.valuationCurrency)}</td>
                      <td class="px-3 py-2 text-right">
                        <div class="flex items-center justify-end gap-2">
                          <button class="house-button house-button-secondary house-button-md" onclick={() => toggleHistory(item.holdingID)} type="button">
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
    {/if}
  </section>
</main>

<style>
  .asset-filter-form {
    display: flex;
    flex-wrap: wrap;
    width: 100%;
    column-gap: 1rem;
    row-gap: 0.85rem;
    align-items: end;
  }

  .asset-filter-field {
    flex: 1 1 var(--asset-filter-width, 10rem);
    min-width: min(100%, var(--asset-filter-min, 10rem));
  }

  .asset-filter-date {
    --asset-filter-min: 22rem;
    --asset-filter-width: 24rem;
    flex-grow: 1.15;
  }

  .asset-filter-account {
    --asset-filter-min: 20rem;
    --asset-filter-width: 25rem;
    flex-grow: 1.25;
  }

  .asset-filter-basis {
    --asset-filter-min: 11rem;
    --asset-filter-width: 12rem;
    flex-grow: 0.7;
  }

  .asset-filter-price {
    --asset-filter-min: 10rem;
    --asset-filter-width: 11rem;
    flex-grow: 0.6;
  }

  .asset-filter-currency {
    --asset-filter-min: 8.5rem;
    --asset-filter-width: 9.5rem;
    flex-grow: 0.45;
  }

  .asset-filter-view {
    --asset-filter-min: 14rem;
    --asset-filter-width: 16rem;
    flex-grow: 0.85;
  }

  :global(.page-header:has(.asset-account-select.house-multiselect[open])) {
    z-index: 90;
    overflow: visible;
  }

  :global(.asset-account-select.house-multiselect[open]),
  :global(.asset-account-select.house-multiselect[open] .house-multiselect-options) {
    z-index: 120;
  }

  :global(.asset-account-select.house-multiselect .house-multiselect-options) {
    width: min(32rem, calc(100vw - 2rem));
    max-height: 20rem;
  }

  .asset-account-filter-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 0.4rem;
    align-items: center;
    margin-bottom: 0.25rem;
  }

  .asset-account-options {
    display: grid;
    gap: 0.15rem;
    max-height: 15.5rem;
    overflow-y: auto;
    padding-right: 0.15rem;
  }

  .asset-account-option {
    min-width: 20rem;
  }

  .asset-account-option span {
    display: grid;
    gap: 0.08rem;
  }

  .asset-account-option strong,
  .asset-account-option small {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .asset-account-option small,
  .asset-account-empty {
    color: var(--muted);
    font-size: 0.75rem;
    font-weight: 560;
  }

  .asset-account-empty {
    padding: 0.55rem;
  }

  :global(.asset-mode-toggle.house-pill-group) {
    display: grid;
    width: 100%;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    flex-wrap: nowrap;
  }

  :global(.asset-mode-toggle .house-pill span) {
    width: 100%;
    min-width: 0;
  }

  .asset-name-title {
    display: flex;
    min-width: 0;
    align-items: baseline;
    gap: 0.25rem;
    color: var(--ink);
    font-weight: 650;
    line-height: 1.25;
  }

  .asset-name-title > span:first-child {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .asset-name-separator {
    color: var(--muted);
    font-weight: 560;
  }

  .asset-ticker {
    flex: 0 0 auto;
    color: var(--accent-strong);
    font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
    font-size: 0.78rem;
    font-weight: 760;
    letter-spacing: 0;
  }

  .asset-detail-link {
    display: inline-flex;
    border: 0;
    background: transparent;
    color: var(--accent-strong);
    cursor: pointer;
    font-size: 0.75rem;
    font-weight: 700;
    line-height: 1.25;
    padding: 0;
    text-decoration: underline;
    text-underline-offset: 2px;
  }

  .asset-detail-link:hover,
  .asset-detail-link:focus-visible {
    color: var(--accent);
    outline: none;
  }

  .asset-detail-row {
    background: color-mix(in srgb, var(--panel-muted) 64%, var(--panel));
  }

  .asset-detail-row td {
    border-top: 1px dashed color-mix(in srgb, var(--line) 82%, transparent);
    color: var(--muted);
  }

  .asset-detail-name {
    display: grid;
    min-width: 0;
    gap: 0.05rem;
  }

  .asset-detail-account {
    min-width: 0;
    overflow: hidden;
    color: var(--ink);
    font-size: 0.82rem;
    font-weight: 680;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .asset-detail-holding {
    min-width: 0;
    overflow: hidden;
    color: var(--muted);
    font-size: 0.72rem;
    font-weight: 560;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .asset-detail-close {
    display: inline-flex;
    width: 1.55rem;
    height: 1.55rem;
    align-items: center;
    justify-content: center;
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    background: var(--panel);
    color: var(--muted);
    cursor: pointer;
  }

  .asset-detail-close:hover,
  .asset-detail-close:focus-visible {
    border-color: var(--accent);
    color: var(--accent-strong);
    outline: none;
  }

  .asset-detail-close svg {
    width: 0.8rem;
    height: 0.8rem;
    fill: none;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 2;
  }

  .asset-table {
    min-width: 66rem;
  }

  .asset-price-column {
    width: 6.75rem;
  }

  .asset-currency-column {
    width: 5.75rem;
  }

  @media (max-width: 1180px) {
    .asset-filter-field {
      flex-grow: 1;
    }
  }

  @media (max-width: 720px) {
    .asset-filter-field {
      --asset-filter-min: 100%;
      flex-basis: 100%;
    }

    .asset-account-option {
      min-width: 16rem;
    }
  }
</style>
