<script lang="ts">
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import { ComplexSelect, Field, PillGroup, type ComplexSelectOption } from '$lib/components/forms';
  import { formatTableDateTime, toApiDateTime } from '$lib/dates';
  import { holdingDateBasisOptions } from '$lib/valuationPreferences';
  import { tick } from 'svelte';
  import { SvelteMap } from 'svelte/reactivity';
  import type {
    Account,
    AccountValuation,
    Accounts,
    AggregateKind,
    Currencies,
    Currency,
    HoldingDateBasis,
    HoldingHistoryEvent,
    InstrumentPriceBasis,
    ProfitLossMethod,
    TransactionReferenceEvent,
    ValuationItem,
    Valuations
  } from '$lib/types';

  type AssetExperienceData = {
    accountID: string;
    accountIDs: string[];
    accounts: Accounts | null;
    assetViewMode: 'Discrete' | 'Aggregate';
    auditDateTime: string;
    currencies: Currencies | null;
    error: string;
    holdingDateBasis: HoldingDateBasis;
    instrumentPriceBasis: InstrumentPriceBasis;
    instrumentPriceBasisOptions: InstrumentPriceBasis[];
    valuationCurrency: string;
    valuationDate: string;
    valuations: Valuations | null;
  };
  type ExperienceRenderMode = 'full' | 'filter' | 'body';
  type AssetProfitLossRow = {
    rowID: string;
    transactionType: 'Credit' | 'Debit';
    instrumentName: string;
    displayDateTime: string;
    quantity: number;
    bookCost: number;
    realizedPnL: number | null;
  };
  type AssetProfitLossSummary = {
    bookValue: number;
    complete: boolean;
    incompleteReason: string | null;
    realizedPnL: number;
    totalPnL: number | null;
    unrealizedPnL: number | null;
  };
  type AssetProfitLossDetails = {
    accountID: string;
    currency: string;
    holdingID: string;
    holdingName: string;
    instrumentName: string;
    method: ProfitLossMethod;
    methodLabel: string;
    rows: AssetProfitLossRow[];
    summary: AssetProfitLossSummary | null;
  };
  type AssetProfitLossState = {
    details: AssetProfitLossDetails | null;
    error: string;
    loading: boolean;
  };

  let {
    data,
    formAction = '',
    formID = 'asset-filter-form',
    renderMode = 'full',
    showPageHeader = true,
    viewer = ''
  }: {
    data: AssetExperienceData;
    formAction?: string;
    formID?: string;
    renderMode?: ExperienceRenderMode;
    showPageHeader?: boolean;
    viewer?: string;
  } = $props();

  const valuationDependencyKinds: AggregateKind[] = [
    'HoldingPositions',
    'Holdings',
    'InstrumentValues',
    'FXs',
    'FXRates'
  ];
  let openHistoryHoldingID = $state('');
  let openProfitLossHoldingID = $state('');
  let accountDropdownOpen = $state(false);
  let accountSelectionDirty = $state(false);
  let selectedAccountIDOverrides = $state<string[] | null>(null);
  let displayMode: 'Discrete' | 'Aggregate' = $derived(data.assetViewMode);
  let holdingBasis: HoldingDateBasis = $derived(data.holdingDateBasis);
  let priceBasis: InstrumentPriceBasis = $derived(data.instrumentPriceBasis);
  let valuationCurrency = $derived(data.valuationCurrency);
  let expandedAggregateAssetID = $state('');
  let historyByHoldingID = $state<Record<string, { events: HoldingHistoryEvent[]; error: string; loading: boolean }>>({});
  let profitLossByHoldingID = $state<Record<string, AssetProfitLossState>>({});

  const valuations = $derived(data.valuations);
  const accounts = $derived(data.accounts?.items ?? []);
  const currencies = $derived(data.currencies?.items ?? []);
  const selectedAccountIDs = $derived(validSelectedAccountIDs());
  const selectedAccountSummary = $derived(accountSummary());
  const accountOptions = $derived<ComplexSelectOption[]>(accounts.map((account) => ({
    id: account.accountID,
    name: account.name,
    meta: `${account.formalName} - ${account.bookCurrency} - ${account.active ? 'Active' : 'Inactive'}`,
    search: `${account.name} ${account.formalName} ${account.bookCurrency} ${account.active ? 'active' : 'inactive'}`,
    tone: account.active ? undefined : 'alert'
  })));
  const selectedBookCurrencies = $derived(selectedBookCurrencyList());
  const aggregateRows = $derived.by(() => valuations ? aggregateAssetRows(valuations.accounts) : []);
  const aggregateBookCostUnavailable = $derived(displayMode === 'Aggregate' && selectedBookCurrencies.length > 1);
  const displayModeOptions = [
    { label: 'Discrete', value: 'Discrete' },
    { label: 'Aggregate', value: 'Aggregate' }
  ];
  const priceBasisOptions = $derived(data.instrumentPriceBasisOptions.map((option) => ({
    label: option,
    value: option
  })));
  const currencyOptions = $derived<ComplexSelectOption[]>(currencies.length
    ? currencies.map((currency) => ({
        id: currency.alphabeticCode,
        name: currencyLabel(currency),
        meta: currency.name,
        search: `${currency.alphabeticCode} ${currency.name}`
      }))
    : data.valuationCurrency
      ? [{ id: data.valuationCurrency, name: data.valuationCurrency }]
      : []);

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

  function sameAccountIDs(left: string[], right: string[]) {
    if (left.length !== right.length)
      return false;

    const rightIDs = new Set(right);
    return left.every((accountID) => rightIDs.has(accountID));
  }

  function selectedBookCurrencyList() {
    const selectedAccountIDSet = new Set(selectedAccountIDs);
    const selectedAccounts = selectedAccountIDs.length
      ? accounts.filter((account) => selectedAccountIDSet.has(account.accountID))
      : accounts;

    return [...new Set(selectedAccounts.map((account) => account.bookCurrency).filter(Boolean))];
  }

  async function submitFilterChange() {
    await tick();
    const form = document.getElementById(formID);

    if (form instanceof HTMLFormElement)
      form.requestSubmit();
  }

  function commitAccountSelection() {
    if (!accountSelectionDirty)
      return;

    accountSelectionDirty = false;
    submitFilterChange();
  }

  function updateAccountSelection(nextSelectedAccountIDs: string[]) {
    selectedAccountIDOverrides = nextSelectedAccountIDs;
    accountSelectionDirty = !sameAccountIDs(nextSelectedAccountIDs, data.accountIDs ?? []);
    syncCurrencyForSelection(nextSelectedAccountIDs);
  }

  function changeAccountSelection(selection: string | string[] | undefined) {
    if (Array.isArray(selection))
      updateAccountSelection(selection);
  }

  function syncCurrencyForSelection(nextSelectedAccountIDs = selectedAccountIDs) {
    if (nextSelectedAccountIDs.length !== 1)
      return;

    const account = accounts.find((item) => item.accountID === nextSelectedAccountIDs[0]);

    if (!account?.bookCurrency)
      return;

    if (currencyOptions.some((option) => option.id === account.bookCurrency))
      valuationCurrency = account.bookCurrency;
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
    openProfitLossHoldingID = '';

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

  async function toggleProfitLoss(item: ValuationItem) {
    if (openProfitLossHoldingID === item.holdingID) {
      openProfitLossHoldingID = '';
      delete profitLossByHoldingID[item.holdingID];
      return;
    }

    openProfitLossHoldingID = item.holdingID;
    openHistoryHoldingID = '';

    if (profitLossByHoldingID[item.holdingID])
      return;

    await loadProfitLoss(item);
  }

  async function loadProfitLoss(item: ValuationItem) {
    profitLossByHoldingID[item.holdingID] = { details: null, error: '', loading: true };

    try {
      const profitLossUrl = new URL('/Asset/ProfitLoss', window.location.origin);
      profitLossUrl.searchParams.set('accountID', item.accountID);
      profitLossUrl.searchParams.set('holdingID', item.holdingID);
      profitLossUrl.searchParams.set('holdingDateBasis', data.holdingDateBasis);
      profitLossUrl.searchParams.set('instrumentPriceBasis', data.instrumentPriceBasis);
      profitLossUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        profitLossUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${profitLossUrl.pathname}${profitLossUrl.search}`);

      if (!response.ok)
        throw new Error(`Profit/Loss request returned ${response.status} ${response.statusText}`);

      profitLossByHoldingID[item.holdingID] = {
        details: await response.json() as AssetProfitLossDetails,
        error: '',
        loading: false
      };
    } catch (error) {
      profitLossByHoldingID[item.holdingID] = {
        details: null,
        error: error instanceof Error ? error.message : 'Unable to load Profit/Loss.',
        loading: false
      };
    }
  }

  function moneyTone(value: number | null | undefined) {
    if (value === null || value === undefined || !Number.isFinite(value) || value === 0)
      return 'text-slate-700';

    return value > 0 ? 'text-emerald-700' : 'text-rose-700';
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


<div
  class={[
    showPageHeader ? 'min-h-screen' : 'viewer-embedded-page',
    renderMode === 'filter' && 'experience-filter-only',
    renderMode === 'body' && 'experience-body-only'
  ]}
>
  {#if renderMode !== 'body'}
  <section class="page-header">
    <div class="page-container">
      {#if showPageHeader}
      <div class="page-header-content">
        <div class="page-header-main">
          <p class="page-kicker">Value</p>
          <div class="page-title-row">
            <h1 class="page-title">Asset</h1>
            <BookmarkButton />
          </div>
        </div>
      </div>
      {/if}

      <form
        id={formID}
        action={formAction}
        class={[
          'house-form asset-filter-form',
          viewer === 'Asset' && renderMode === 'filter' && 'asset-filter-viewer-form'
        ]}
        method="GET"
      >
        {#if viewer}
          <input name="viewer" type="hidden" value={viewer} />
        {/if}
        {#if data.auditDateTime}
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
        {/if}

        <label class="asset-filter-field asset-filter-date grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput fullWidth name="valuationDate" onchange={submitFilterChange} step="1" value={data.valuationDate} />
        </label>

        <div class="asset-filter-field asset-filter-account grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Account
          <ComplexSelect
            ariaLabel="Accounts"
            bind:open={accountDropdownOpen}
            class="asset-account-select"
            confirmSelection
            emptyText="No accounts match"
            minimumSelections={1}
            multiple
            name="accountID"
            onchange={changeAccountSelection}
            onclose={commitAccountSelection}
            options={accountOptions}
            placeholder="No accounts selected"
            searchPlaceholder="Search accounts"
            showClear={false}
            summary={selectedAccountSummary}
            values={selectedAccountIDs}
          />
        </div>

        <div class="asset-filter-field asset-filter-view grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          View
          <PillGroup
            ariaLabel="Asset view mode"
            bind:value={displayMode}
            class="asset-mode-toggle"
            compact
            name="assetViewMode"
            onchange={submitFilterChange}
            options={displayModeOptions}
          />
        </div>

        <div class="asset-filter-secondary-row">
          <Field class="asset-filter-field asset-filter-basis" controlId="asset-holding-basis" label="Holding basis">
            <PillGroup
              ariaLabel="Holding basis"
              bind:value={holdingBasis}
              class="asset-basis-toggle"
              compact
              id="asset-holding-basis"
              name="holdingDateBasis"
              onchange={submitFilterChange}
              options={holdingDateBasisOptions}
            />
          </Field>

          <Field class="asset-filter-field asset-filter-price" controlId="asset-price-basis" label="Price basis">
            <PillGroup
              ariaLabel="Price basis"
              bind:value={priceBasis}
              class="asset-basis-toggle"
              compact
              id="asset-price-basis"
              name="instrumentPriceBasis"
              onchange={submitFilterChange}
              options={priceBasisOptions}
            />
          </Field>

          <Field class="asset-filter-field asset-filter-currency" controlId="asset-valuation-currency" label="Currency">
            <ComplexSelect
              ariaLabel="Currencies"
              bind:value={valuationCurrency}
              class="asset-currency-select"
              compactBrand
              disabled={!currencyOptions.length}
              emptyText="No currencies match"
              id="asset-valuation-currency"
              name="valuationCurrency"
              onchange={submitFilterChange}
              options={currencyOptions}
              placeholder="Select currency"
              searchPlaceholder="Search currencies"
            />
          </Field>
        </div>
      </form>
    </div>
  </section>
  {/if}

  {#if renderMode !== 'filter'}
  <section class="page-container page-section grid gap-5">

    {#if data.error}
      <Card density="compact" intent="error" role="status">{data.error}</Card>
    {:else if valuations}
      {#each valuationDependencyKinds as aggregateKind (aggregateKind)}
        <AggregateUpdateWatcher {aggregateKind} valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={valuations.lastEventID} />
      {/each}

      {#if displayMode === 'Aggregate'}
        <section class="house-card grid gap-3" style="border-top: 3px solid var(--brand-green)">
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
          <section class="house-card grid gap-3" style="border-top: 3px solid var(--brand-green)">
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
                        <div class="asset-row-actions">
                          <button
                            aria-label={`${openHistoryHoldingID === item.holdingID ? 'Hide' : 'Show'} history for ${itemDisplay.instrumentName}`}
                            aria-pressed={openHistoryHoldingID === item.holdingID}
                            class:asset-action-button-active={openHistoryHoldingID === item.holdingID}
                            class="asset-action-button asset-action-button-history"
                            onclick={() => toggleHistory(item.holdingID)}
                            title="History"
                            type="button"
                          >
                            <svg aria-hidden="true" viewBox="0 0 24 24">
                              <path d="M3 12a9 9 0 1 0 3-6.7" />
                              <path d="M3 4v5h5" />
                              <path d="M12 7v5l3 2" />
                            </svg>
                          </button>
                          <button
                            aria-label={`${openProfitLossHoldingID === item.holdingID ? 'Hide' : 'Show'} Profit/Loss for ${itemDisplay.instrumentName}`}
                            aria-pressed={openProfitLossHoldingID === item.holdingID}
                            class:asset-action-button-active={openProfitLossHoldingID === item.holdingID}
                            class="asset-action-button asset-action-button-pnl"
                            onclick={() => toggleProfitLoss(item)}
                            title="Profit/Loss"
                            type="button"
                          >
                            <svg aria-hidden="true" viewBox="0 0 24 24">
                              <path d="M4 18h16" />
                              <path d="m7 15 4-4 3 3 4-7" />
                              <path d="M17 7h4v4" />
                            </svg>
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
                              <Card density="compact" intent="error">{history.error}</Card>
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
                    {#if openProfitLossHoldingID === item.holdingID}
                      {@const profitLoss = profitLossByHoldingID[item.holdingID]}
                      <tr class="bg-slate-50/80">
                        <td class="px-3 py-3" colspan="10">
                          {#if profitLoss?.loading}
                            <div class="text-sm text-slate-600">Loading Profit/Loss...</div>
                          {:else if profitLoss?.error}
                            <Card density="compact" intent="error">{profitLoss.error}</Card>
                          {:else if profitLoss?.details}
                            <section class="asset-pnl-card" aria-label={`Profit/Loss for ${itemDisplay.instrumentName}`}>
                              <header class="asset-pnl-header">
                                <div>
                                  <h3>Profit/Loss</h3>
                                  <p>{profitLoss.details.methodLabel}</p>
                                </div>
                                {#if profitLoss.details.summary}
                                  <div class={`asset-pnl-total ${moneyTone(profitLoss.details.summary.totalPnL)}`}>
                                    {formatMoney(profitLoss.details.summary.totalPnL, profitLoss.details.currency)}
                                  </div>
                                {/if}
                              </header>

                              {#if profitLoss.details.summary && !profitLoss.details.summary.complete && profitLoss.details.summary.incompleteReason}
                                <Card density="compact" intent="warning">{profitLoss.details.summary.incompleteReason}</Card>
                              {/if}

                              <div class="asset-pnl-table-wrap">
                                <table class="asset-pnl-table">
                                  <thead>
                                    <tr>
                                      <th scope="col">Instrument</th>
                                      <th scope="col">Date</th>
                                      <th scope="col" class="text-right">Qty</th>
                                      <th scope="col" class="text-right">Book cost</th>
                                      <th scope="col" class="text-right">PnL</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {#if profitLoss.details.rows.length}
                                      {#each profitLoss.details.rows as row (row.rowID)}
                                        <tr>
                                          <td>
                                            <div class="asset-pnl-instrument">
                                              <span>{row.instrumentName || profitLoss.details.instrumentName || itemDisplay.instrumentName}</span>
                                              <small>{row.transactionType}</small>
                                            </div>
                                          </td>
                                          <td>{formatTableDateTime(row.displayDateTime)}</td>
                                          <td class="text-right font-mono">{formatNumber(row.quantity)}</td>
                                          <td class="text-right font-mono">{formatMoney(row.bookCost, profitLoss.details.currency)}</td>
                                          <td class={`text-right font-mono font-semibold ${moneyTone(row.realizedPnL)}`}>
                                            {formatMoney(row.realizedPnL, profitLoss.details.currency)}
                                          </td>
                                        </tr>
                                      {/each}
                                    {:else}
                                      <tr>
                                        <td class="asset-pnl-empty" colspan="5">No transactions found.</td>
                                      </tr>
                                    {/if}

                                    {#if profitLoss.details.summary}
                                      <tr class="asset-pnl-summary-row">
                                        <th scope="row" colspan="4">Realised PnL</th>
                                        <td class={`text-right font-mono font-semibold ${moneyTone(profitLoss.details.summary.realizedPnL)}`}>
                                          {formatMoney(profitLoss.details.summary.realizedPnL, profitLoss.details.currency)}
                                        </td>
                                      </tr>
                                      <tr class="asset-pnl-summary-row">
                                        <th scope="row" colspan="4">Unrealised PnL</th>
                                        <td class={`text-right font-mono font-semibold ${moneyTone(profitLoss.details.summary.unrealizedPnL)}`}>
                                          {formatMoney(profitLoss.details.summary.unrealizedPnL, profitLoss.details.currency)}
                                        </td>
                                      </tr>
                                      <tr class="asset-pnl-summary-row asset-pnl-summary-total">
                                        <th scope="row" colspan="4">Total PnL</th>
                                        <td class={`text-right font-mono font-semibold ${moneyTone(profitLoss.details.summary.totalPnL)}`}>
                                          {formatMoney(profitLoss.details.summary.totalPnL, profitLoss.details.currency)}
                                        </td>
                                      </tr>
                                    {/if}
                                  </tbody>
                                </table>
                              </div>
                            </section>
                          {/if}
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
  {/if}
</div>

<style>
  .experience-filter-only .page-header {
    overflow: visible;
    background: transparent;
    padding: 0;
  }

  .experience-filter-only .page-header::before {
    display: none;
  }

  .experience-filter-only .page-container {
    max-width: none;
    padding: 0;
  }

  .experience-filter-only .asset-filter-form {
    border: 0;
    background: transparent;
    box-shadow: none;
    padding: 0;
  }

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
    color: var(--muted);
    font-size: var(--house-label-size);
    font-weight: 700;
    line-height: 1.25rem;
  }

  .asset-filter-date {
    --asset-filter-min: 24rem;
    --asset-filter-width: 26.5rem;
    flex-grow: 1.3;
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
    --asset-filter-min: 10.75rem;
    --asset-filter-width: 11.25rem;
    flex-grow: 0.25;
  }

  .asset-filter-secondary-row {
    display: contents;
  }

  .asset-filter-viewer-form {
    display: grid;
    grid-template-columns: var(--house-datetime-width) minmax(20rem, 1fr) minmax(10.75rem, 11.25rem);
    gap: 1rem 1.15rem;
    align-items: end;
  }

  .asset-filter-viewer-form .asset-filter-field {
    min-width: 0;
    flex: none;
  }

  .asset-filter-viewer-form .asset-filter-date,
  .asset-filter-viewer-form .asset-filter-account,
  .asset-filter-viewer-form .asset-filter-view,
  .asset-filter-viewer-form .asset-filter-basis,
  .asset-filter-viewer-form .asset-filter-price,
  .asset-filter-viewer-form .asset-filter-currency {
    --asset-filter-min: 0;
    --asset-filter-width: auto;
  }

  .asset-filter-viewer-form .asset-filter-secondary-row {
    display: grid;
    grid-column: 1 / -1;
    grid-template-columns: minmax(13rem, 1fr) minmax(13rem, 1fr) minmax(10rem, 14rem);
    gap: 0.85rem;
    align-items: end;
  }

  :global(.page-header:has(.asset-account-select .complex-select-menu)) {
    z-index: 90;
    overflow: visible;
  }

  :global(.asset-account-select:has(.complex-select-menu)),
  :global(.asset-account-select .complex-select-menu) {
    z-index: 120;
  }

  :global(.asset-account-select .complex-select-menu) {
    width: min(32rem, calc(100vw - 2rem));
    max-height: clamp(18rem, calc(100vh - 8rem), 26rem);
    overflow: hidden;
  }

  :global(.asset-account-select .complex-select-option) {
    min-width: 20rem;
  }

  :global(.asset-mode-toggle.house-pill-group) {
    display: grid;
    width: 100%;
    max-width: 11.25rem;
    border-color: color-mix(in srgb, var(--brand-green) 54%, var(--line));
    background: color-mix(in srgb, var(--brand-green) 16%, var(--panel));
    gap: 0.125rem;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    flex-wrap: nowrap;
  }

  :global(.asset-mode-toggle .house-pill input:checked + span) {
    box-shadow: 0 0 0 1px color-mix(in srgb, var(--accent) 18%, transparent);
  }

  :global(.asset-mode-toggle .house-pill span) {
    width: 100%;
    min-width: 0;
    padding-inline: 0.45rem;
  }
  :global(.asset-basis-toggle.house-pill-group) {
    display: grid;
    width: fit-content;
    border-color: color-mix(in srgb, var(--brand-green) 54%, var(--line));
    background: color-mix(in srgb, var(--brand-green) 16%, var(--panel));
    gap: 0.125rem;
    grid-auto-columns: minmax(0, 1fr);
    grid-auto-flow: column;
    flex-wrap: nowrap;
  }

  :global(.asset-basis-toggle .house-pill span) {
    width: 100%;
    min-width: 0;
    padding-inline: 0.45rem;
  }

  :global(.asset-basis-toggle .house-pill input:checked + span) {
    box-shadow: 0 0 0 1px color-mix(in srgb, var(--accent) 18%, transparent);
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

  .asset-row-actions {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 0.35rem;
  }

  .asset-action-button {
    display: inline-grid;
    width: 2rem;
    height: 1.75rem;
    place-items: center;
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    background: var(--panel);
    color: var(--muted);
    cursor: pointer;
    transition:
      background-color 0.14s ease,
      border-color 0.14s ease,
      color 0.14s ease,
      box-shadow 0.14s ease;
  }

  .asset-action-button:hover,
  .asset-action-button:focus-visible {
    border-color: var(--accent);
    color: var(--accent-strong);
    outline: none;
  }

  .asset-action-button svg {
    width: 1rem;
    height: 1rem;
    fill: none;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 2;
  }

  .asset-action-button-history {
    background: color-mix(in srgb, var(--accent-soft) 56%, var(--panel));
    color: var(--accent-strong);
  }

  .asset-action-button-pnl {
    background: color-mix(in srgb, #ecfdf5 68%, var(--panel));
    color: #047857;
  }

  .asset-action-button-active {
    border-color: var(--accent);
    box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--accent) 18%, transparent);
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

  .asset-pnl-card {
    display: grid;
    gap: 0.75rem;
    border: 1px solid color-mix(in srgb, var(--line) 88%, transparent);
    border-radius: var(--house-radius);
    background: var(--panel);
    padding: 0.9rem;
    box-shadow: 0 10px 24px rgb(15 23 42 / 0.06);
  }

  .asset-pnl-header {
    display: flex;
    align-items: start;
    justify-content: space-between;
    gap: 1rem;
  }

  .asset-pnl-header h3 {
    margin: 0;
    color: var(--ink);
    font-size: 0.95rem;
    font-weight: 760;
    letter-spacing: 0;
    line-height: 1.2;
  }

  .asset-pnl-header p {
    margin: 0.15rem 0 0;
    color: var(--muted);
    font-size: 0.75rem;
    font-weight: 620;
    line-height: 1.25;
  }

  .asset-pnl-total {
    flex: 0 0 auto;
    font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
    font-size: 0.95rem;
    font-weight: 760;
    line-height: 1.2;
  }

  .asset-pnl-table-wrap {
    overflow-x: auto;
  }

  .asset-pnl-table {
    width: 100%;
    min-width: 44rem;
    border-collapse: collapse;
    color: var(--ink);
    font-size: 0.82rem;
  }

  .asset-pnl-table th,
  .asset-pnl-table td {
    border-bottom: 1px solid color-mix(in srgb, var(--line) 76%, transparent);
    padding: 0.45rem 0.55rem;
    text-align: left;
    vertical-align: middle;
  }

  .asset-pnl-table thead th {
    color: var(--muted);
    font-size: 0.7rem;
    font-weight: 760;
    letter-spacing: 0;
    text-transform: uppercase;
  }

  .asset-pnl-table .text-right {
    text-align: right;
  }

  .asset-pnl-instrument {
    display: grid;
    min-width: 0;
    gap: 0.05rem;
  }

  .asset-pnl-instrument span {
    min-width: 0;
    overflow: hidden;
    font-weight: 650;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .asset-pnl-instrument small {
    color: var(--muted);
    font-size: 0.68rem;
    font-weight: 640;
    line-height: 1.1;
  }

  .asset-pnl-empty {
    color: var(--muted);
    font-weight: 560;
    text-align: center;
  }

  .asset-pnl-summary-row th,
  .asset-pnl-summary-row td {
    background: color-mix(in srgb, var(--panel-muted) 58%, var(--panel));
  }

  .asset-pnl-summary-row th {
    color: var(--muted);
    font-weight: 740;
    text-align: right;
  }

  .asset-pnl-summary-total th,
  .asset-pnl-summary-total td {
    border-bottom: 0;
    background: color-mix(in srgb, var(--accent-soft) 40%, var(--panel));
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

  @media (max-width: 980px) {
    .asset-filter-viewer-form,
    .asset-filter-viewer-form .asset-filter-secondary-row {
      grid-template-columns: minmax(0, 1fr);
    }
  }

  @media (max-width: 720px) {
    .asset-filter-field {
      --asset-filter-min: 100%;
      flex-basis: 100%;
    }

    :global(.asset-account-select .complex-select-option) {
      min-width: 16rem;
    }
  }
</style>
