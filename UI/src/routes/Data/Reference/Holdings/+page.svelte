<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { EventPropertyDetail, Holding, HoldingHistoryEvent, HoldingKind, TransactionReferenceEvent } from '$lib/types';
  import type { SubmitFunction } from '@sveltejs/kit';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  type SortKey = 'name' | 'type' | 'account' | 'instrument' | 'status' | 'lastAudit';

  const holdingKinds: HoldingKind[] = ['PositionMemo', 'PositionCash', 'PositionAsset', 'CashDebt', 'CashInvestable', 'CashNonInvestable', 'NominalInflow', 'NominalOutflow', 'NominalInSpecieIn', 'NominalInSpecieOut', 'NominalFeesCustodian', 'NominalFeesAdministrator', 'NominalFeesBank', 'NominalIncome', 'NominalInterest'];
  const historyDatePropertyNames = new Set(['ValuationDateTime', 'EventDateTime', 'SettlementDateTime', 'CancelledDateTime', 'CancellationDateTime', 'AuditDateTime']);
  const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  const holdingCount = $derived(data.holdings?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.holdings ? formatDisplayDateTime(data.holdings.asOfDateTime) : 'now');
  const accountsByID = $derived(new Map((data.accounts?.items ?? []).map((account) => [account.accountID, account.name])));
  const instrumentsByID = $derived(new Map((data.instruments?.items ?? []).map((instrument) => [instrument.instrumentID, instrument.name])));

  let sortKey = $state<SortKey>('name');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let addingHolding = $state(false);
  let editingHoldingID = $state('');
  let submittingHoldingID = $state('');
  let submittingCreate = $state(false);
  let openHistoryHoldingID = $state('');
  let historyByHoldingID = $state<Record<string, { events: HoldingHistoryEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');

  const filteredHoldings = $derived(
    (data.holdings?.items ?? []).filter((holding: Holding) => {
      const filter = filterText.trim().toLocaleLowerCase();

      if (!filter)
        return true;

      return [
        holding.name,
        holding.holdingKind,
        accountsByID.get(holding.accountID) ?? '',
        instrumentsByID.get(holding.instrumentID) ?? '',
        holding.active ? 'active' : 'inactive',
        holding.includeInValuation ? 'valuation' : 'excluded'
      ].some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  const sortedHoldings = $derived(
    [...filteredHoldings].sort((left, right) => {
      const direction = sortDirection;

      switch (sortKey) {
        case 'type':
          return direction * left.holdingKind.localeCompare(right.holdingKind);
        case 'account':
          return direction * (accountsByID.get(left.accountID) ?? '').localeCompare(accountsByID.get(right.accountID) ?? '');
        case 'instrument':
          return direction * (instrumentsByID.get(left.instrumentID) ?? '').localeCompare(instrumentsByID.get(right.instrumentID) ?? '');
        case 'status':
          return direction * Number(left.active === right.active ? 0 : left.active ? -1 : 1);
        case 'lastAudit':
          return direction * (new Date(left.lastAuditDateTime).getTime() - new Date(right.lastAuditDateTime).getTime());
        case 'name':
        default:
          return direction * displayName(left).localeCompare(displayName(right));
      }
    })
  );

  $effect(() => {
    const nextHistoryContextKey = createHistoryContextKey();
    if (!loadedHistoryContextKey) {
      loadedHistoryContextKey = nextHistoryContextKey;
      return;
    }

    if (nextHistoryContextKey === loadedHistoryContextKey)
      return;

    loadedHistoryContextKey = nextHistoryContextKey;
    if (openHistoryHoldingID)
      void loadHistory(openHistoryHoldingID);
  });

  function setSort(nextSortKey: SortKey) {
    if (sortKey === nextSortKey) {
      sortDirection = sortDirection === 1 ? -1 : 1;
      return;
    }

    sortKey = nextSortKey;
    sortDirection = 1;
  }

  function sortLabel(nextSortKey: SortKey) {
    if (sortKey !== nextSortKey)
      return '';

    return sortDirection === 1 ? ' ↑' : ' ↓';
  }

  function displayName(holding: Holding) {
    return holding.name || holdingKindLabel(holding.holdingKind);
  }

  function holdingKindLabel(holdingKind: HoldingKind) {
    return holdingKind
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/^Nominal /, '');
  }

  function isBankHolding(holding: Holding) {
    return isBankHoldingKind(holding.holdingKind);
  }

  function isBankHoldingKind(holdingKind: HoldingKind) {
    return holdingKind === 'CashDebt' || holdingKind === 'CashInvestable' || holdingKind === 'CashNonInvestable';
  }

  function bankSummary(holding: Holding) {
    return [holding.bankName, holding.accountName, holding.sortCode, holding.accountNumber]
      .filter(Boolean)
      .join(' / ') || '-';
  }

  function startEdit(holdingID: string) {
    addingHolding = false;
    editingHoldingID = holdingID;
  }

  function cancelEdit() {
    editingHoldingID = '';
  }

  function startAdd() {
    editingHoldingID = '';
    addingHolding = true;
  }

  function cancelAdd() {
    addingHolding = false;
  }

  const enhanceHoldingCreate: SubmitFunction = () => {
    submittingCreate = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;

      if (result.type === 'success')
        addingHolding = false;
    };
  };

  const enhanceHoldingEdit: SubmitFunction = ({ formData }) => {
    const holdingID = formData.get('holdingID');
    submittingHoldingID = typeof holdingID === 'string' ? holdingID : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingHoldingID = '';

      if (result.type === 'success')
        editingHoldingID = '';
    };
  };

  const enhanceHoldingActive: SubmitFunction = ({ formData }) => {
    const holdingID = formData.get('holdingID');
    submittingHoldingID = typeof holdingID === 'string' ? holdingID : '';

    return async ({ update }) => {
      await update({ reset: false });
      submittingHoldingID = '';
    };
  };

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

  function createHistoryContextKey() {
    return [
      data.valuationDate,
      data.auditDateTime ?? '',
      data.holdings?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
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
    ].filter(Boolean).join(' · ');
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
      `Book cost ${formatNumber(event.bookCost)}`
    ].join(' - ');
  }

  function transactionEventDetail(event: TransactionReferenceEvent) {
    if (event.$type === 'TransactionCancellationEvent')
      return event.cancelledIDGroup?.length
        ? `Cancelled group ${event.cancelledIDGroup.join(', ')}`
        : '';

    return `Event set ${event.eventSetID}`;
  }

  function formatNumber(value: number | null | undefined) {
    if (value === null || value === undefined || !Number.isFinite(value))
      return '-';

    return new Intl.NumberFormat('en-GB', {
      maximumFractionDigits: 8,
      minimumFractionDigits: 0
    }).format(value);
  }

  function historyDateRows(event: HoldingHistoryEvent) {
    const valuationDateTime = detailString(event, 'ValuationDateTime');
    const eventDateTime = event.eventDateTime || detailString(event, 'EventDateTime');
    const settlementDateTime = isTransactionHistoryEvent(event)
      ? event.settlementDateTime
      : detailString(event, 'SettlementDateTime');
    const cancelledDateTime = detailString(event, 'CancelledDateTime') ||
      detailString(event, 'CancellationDateTime') ||
      (event.$type === 'TransactionCancellationEvent' ? event.auditDateTime : '');
    const rows: { label: string; value: string }[] = [];

    if (valuationDateTime)
      rows.push({ label: 'Valuation', value: valuationDateTime });

    if (eventDateTime && (!valuationDateTime || !isSameDateTime(eventDateTime, valuationDateTime)))
      rows.push({ label: valuationDateTime ? 'Event' : firstDateLabel(eventDateTime), value: eventDateTime });

    if (settlementDateTime)
      rows.push({ label: 'Settlement', value: settlementDateTime });

    if (cancelledDateTime)
      rows.push({ label: 'Cancelled', value: cancelledDateTime });

    if (event.auditDateTime)
      rows.push({ label: 'Audit', value: event.auditDateTime });

    return rows;
  }

  function firstDateLabel(value: string) {
    return isStartOfDay(value) ? 'Valuation' : 'Event';
  }

  function isStartOfDay(value: string) {
    const date = new Date(value);

    return Number.isFinite(date.getTime()) &&
      date.getHours() === 0 &&
      date.getMinutes() === 0 &&
      date.getSeconds() === 0 &&
      date.getMilliseconds() === 0;
  }

  function isSameDateTime(left: string, right: string) {
    const leftTime = new Date(left).getTime();
    const rightTime = new Date(right).getTime();

    return Number.isFinite(leftTime) && Number.isFinite(rightTime)
      ? leftTime === rightTime
      : left === right;
  }

  function historyGuidDetails(event: HoldingHistoryEvent) {
    const details = (event.propertyDetails ?? []).filter((detail) => !historyDatePropertyNames.has(detail.name) && isGuidDetail(detail));
    const seen = new Set<string>();

    return details.filter((detail) => {
      const key = `${detail.name}|${formatHistoryDetailValue(detail.value)}`;

      if (seen.has(key))
        return false;

      seen.add(key);
      return true;
    });
  }

  function historyPropertyExclusionNames(guidDetails: EventPropertyDetail[]) {
    return [
      ...historyDatePropertyNames,
      ...guidDetails.map((detail) => detail.name)
    ];
  }

  function isGuidDetail(detail: EventPropertyDetail) {
    return valuesFromDetail(detail.value).some((value) => guidPattern.test(value));
  }

  function valuesFromDetail(value: unknown): string[] {
    if (typeof value === 'string')
      return [value.trim()];

    if (Array.isArray(value))
      return value.flatMap(valuesFromDetail);

    if (value && typeof value === 'object') {
      const record = value as Record<string, unknown>;
      const primitive = record.value ?? record.Value;

      return primitive === undefined || primitive === value
        ? []
        : valuesFromDetail(primitive);
    }

    return [];
  }

  function detailString(event: HoldingHistoryEvent, name: string) {
    const value = event.propertyDetails?.find((detail) => detail.name === name)?.value;
    const formatted = formatHistoryDetailValue(value);

    return formatted === '-' ? '' : formatted;
  }

  function formatHistoryDetailValue(value: unknown): string {
    if (value === null || value === undefined)
      return '-';

    if (typeof value === 'string')
      return value.trim().length ? value : '-';

    if (typeof value === 'number' || typeof value === 'boolean')
      return String(value);

    if (Array.isArray(value))
      return value.length ? value.map(formatHistoryDetailValue).join(', ') : '-';

    if (value && typeof value === 'object') {
      const record = value as Record<string, unknown>;
      const primitive = record.value ?? record.Value ?? record.amount ?? record.Amount;

      if (primitive !== undefined && primitive !== value)
        return formatHistoryDetailValue(primitive);

      return JSON.stringify(value);
    }

    return '-';
  }
</script>

<svelte:head>
  <title>Holdings - Foleo</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-main">
        <p class="page-kicker">Reference Data</p>
        <div class="page-title-row">
          <h1 class="page-title">Holdings</h1>
          <BookmarkButton />
        </div>
      </div>

      <form class="house-form grid gap-4 md:grid-cols-[var(--house-datetime-width)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput fullWidth name="valuationDate" step="1" value={data.valuationDate} />
        </label>

        {#if data.auditDateTime}
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
        {/if}

        <button class="house-button house-button-primary house-button-md" type="submit">Apply</button>
      </form>
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <div class="status-panel status-panel-error">{data.error}</div>
    {:else if data.holdings}
      {#if form?.message}
        <div class={['status-panel mb-4', form.status === 'success' ? 'status-panel-success' : 'status-panel-error']} role="status">
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
        </div>
      {/if}

      <AggregateUpdateWatcher aggregateKind="Holdings" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.holdings.lastEventID} />

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{holdingCount}</span> holdings</div>
        <div>Valuation {formatDisplayDateTime(data.holdings.valuationDateTime)} · As-of {asOfSummary}</div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter holdings</span>
            <input bind:value={filterText} placeholder="Filter holdings..." type="search" />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add holding" onclick={startAdd} title="Add holding" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('name')} type="button">Name{sortLabel('name')}</button></th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('type')} type="button">Type{sortLabel('type')}</button></th>
                <th class="px-3 py-2">Bank account</th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('account')} type="button">Account{sortLabel('account')}</button></th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('instrument')} type="button">Instrument{sortLabel('instrument')}</button></th>
                <th class="px-3 py-2">Default</th>
                <th class="px-3 py-2">Valuation</th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('status')} type="button">Status{sortLabel('status')}</button></th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('lastAudit')} type="button">Last audit{sortLabel('lastAudit')}</button></th>
                <th class="w-56 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#if addingHolding}
                <tr class="bg-teal-50/30 align-top">
                  <td class="px-3 py-2">
                    <form id="holding-create" action="?/createHolding" method="POST" use:enhance={enhanceHoldingCreate}>
                      <label class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Name</span>
                        <input class="house-control house-control-sm w-40" name="name" type="text" value={form?.intent === 'createHolding' ? (form.values?.name ?? '') : ''} />
                      </label>
                    </form>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Type</span>
                      <select class="house-control house-control-sm" form="holding-create" name="holdingKind">
                        {#each holdingKinds as holdingKind (holdingKind)}
                          <option value={holdingKind}>{holdingKindLabel(holdingKind)}</option>
                        {/each}
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid gap-1 text-xs font-medium text-slate-600">
                      <span>Bank account</span>
                      <input class="house-control house-control-sm w-36" form="holding-create" name="bankName" placeholder="Bank" type="text" value={form?.intent === 'createHolding' ? (form.values?.bankName ?? '') : ''} />
                      <input class="house-control house-control-sm w-36" form="holding-create" name="accountName" placeholder="Account" type="text" value={form?.intent === 'createHolding' ? (form.values?.accountName ?? '') : ''} />
                      <div class="flex gap-1">
                        <input class="house-control house-control-sm w-20" form="holding-create" name="sortCode" placeholder="Sort" type="text" value={form?.intent === 'createHolding' ? (form.values?.sortCode ?? '') : ''} />
                        <input class="house-control house-control-sm w-24" form="holding-create" name="accountNumber" placeholder="Number" type="text" value={form?.intent === 'createHolding' ? (form.values?.accountNumber ?? '') : ''} />
                      </div>
                    </div>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Account</span>
                      <select class="house-control house-control-sm w-44" form="holding-create" name="accountID" required>
                        {#each data.accounts?.items ?? [] as account (account.accountID)}
                          <option value={account.accountID}>{account.name}</option>
                        {/each}
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Instrument</span>
                      <select class="house-control house-control-sm w-44" form="holding-create" name="instrumentID" required>
                        {#each data.instruments?.items ?? [] as instrument (instrument.instrumentID)}
                          <option value={instrument.instrumentID}>{instrument.name}</option>
                        {/each}
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Default</span>
                      <select class="house-control house-control-sm" form="holding-create" name="default">
                        <option value="false">No</option>
                        <option value="true">Yes</option>
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2"></td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Status</span>
                      <select class="house-control house-control-sm" form="holding-create" name="active">
                        <option value="true">Active</option>
                        <option value="false">Inactive</option>
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Event date</span>
                      <DateTimeInput size="sm" form="holding-create" name="eventDateTime" required step="1" value={eventDateDefault} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={cancelAdd} type="button">Cancel</button>
                        <button class="house-button house-button-primary house-button-sm" disabled={submittingCreate} form="holding-create" type="submit">{submittingCreate ? 'Adding' : 'Add'}</button>
                      </div>
                    </div>
                  </td>
                </tr>
              {/if}

              {#each sortedHoldings as holding (holding.holdingID)}
                {#if editingHoldingID === holding.holdingID}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      <form id={`holding-edit-${holding.holdingID}`} action="?/modifyHolding" method="POST" use:enhance={enhanceHoldingEdit}>
                        <input name="holdingID" type="hidden" value={holding.holdingID} />
                        <input name="holdingKind" type="hidden" value={holding.holdingKind} />
                        <input name="bic" type="hidden" value={holding.bic ?? ''} />
                        <input name="iban" type="hidden" value={holding.iban ?? ''} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Name</span>
                          <input class="house-control house-control-sm w-40" name="name" type="text" value={form?.holdingID === holding.holdingID ? (form.values?.name ?? holding.name) : holding.name} />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2 text-slate-700">{holdingKindLabel(holding.holdingKind)}</td>
                    <td class="px-3 py-2">
                      {#if isBankHolding(holding)}
                        <div class="grid gap-1">
                          <input class="house-control house-control-sm w-36" form={`holding-edit-${holding.holdingID}`} name="bankName" type="text" value={form?.holdingID === holding.holdingID ? (form.values?.bankName ?? holding.bankName ?? '') : (holding.bankName ?? '')} />
                          <input class="house-control house-control-sm w-36" form={`holding-edit-${holding.holdingID}`} name="accountName" type="text" value={form?.holdingID === holding.holdingID ? (form.values?.accountName ?? holding.accountName ?? '') : (holding.accountName ?? '')} />
                          <div class="flex gap-1">
                            <input class="house-control house-control-sm w-20" form={`holding-edit-${holding.holdingID}`} name="sortCode" type="text" value={form?.holdingID === holding.holdingID ? (form.values?.sortCode ?? holding.sortCode ?? '') : (holding.sortCode ?? '')} />
                            <input class="house-control house-control-sm w-24" form={`holding-edit-${holding.holdingID}`} name="accountNumber" type="text" value={form?.holdingID === holding.holdingID ? (form.values?.accountNumber ?? holding.accountNumber ?? '') : (holding.accountNumber ?? '')} />
                          </div>
                        </div>
                      {:else}
                        <span class="text-slate-500">-</span>
                      {/if}
                    </td>
                    <td class="px-3 py-2 text-slate-700">{accountsByID.get(holding.accountID) ?? holding.accountID}</td>
                    <td class="px-3 py-2 text-slate-700">{instrumentsByID.get(holding.instrumentID) ?? holding.instrumentID}</td>
                    <td class="px-3 py-2">
                      <select class="house-control house-control-sm" form={`holding-edit-${holding.holdingID}`} name="default">
                        <option selected={!holding.default} value="false">No</option>
                        <option selected={holding.default} value="true">Yes</option>
                      </select>
                    </td>
                    <td class="px-3 py-2">{holding.includeInValuation ? 'Included' : 'Excluded'}</td>
                    <td class="px-3 py-2">{holding.active ? 'Active' : 'Inactive'}</td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`holding-edit-${holding.holdingID}`}>
                        <span>Event date</span>
                        <DateTimeInput size="sm" form={`holding-edit-${holding.holdingID}`} name="eventDateTime" required step="1" value={eventDateDefault} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={cancelEdit} type="button">Cancel</button>
                        <button class="house-button house-button-primary house-button-sm" disabled={submittingHoldingID === holding.holdingID} form={`holding-edit-${holding.holdingID}`} type="submit">{submittingHoldingID === holding.holdingID ? 'Saving' : 'Save'}</button>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="hover:bg-slate-50">
                    <td class="px-3 py-2 font-medium text-slate-950">{displayName(holding)}</td>
                    <td class="px-3 py-2 text-slate-700">{holdingKindLabel(holding.holdingKind)}</td>
                    <td class="px-3 py-2 text-slate-700">{bankSummary(holding)}</td>
                    <td class="px-3 py-2 text-slate-700">{accountsByID.get(holding.accountID) ?? holding.accountID}</td>
                    <td class="px-3 py-2 text-slate-700">{instrumentsByID.get(holding.instrumentID) ?? holding.instrumentID}</td>
                    <td class="px-3 py-2">{holding.default ? 'Default' : '-'}</td>
                    <td class="px-3 py-2">{holding.includeInValuation ? 'Included' : 'Excluded'}</td>
                    <td class="px-3 py-2">
                      <span class={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${holding.active ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'}`}>{holding.active ? 'Active' : 'Inactive'}</span>
                    </td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(holding.lastAuditDateTime)}</td>
                    <td class="px-3 py-2">
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={() => toggleHistory(holding.holdingID)} type="button">{openHistoryHoldingID === holding.holdingID ? 'Hide' : 'History'}</button>
                        <button class="house-button house-button-secondary house-button-sm" onclick={() => startEdit(holding.holdingID)} type="button">Edit</button>
                        <form action="?/modifyHoldingActive" method="POST" use:enhance={enhanceHoldingActive}>
                          <input name="holdingID" type="hidden" value={holding.holdingID} />
                          <input name="name" type="hidden" value={displayName(holding)} />
                          <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                          <input name="active" type="hidden" value={holding.active ? 'false' : 'true'} />
                          <button class="house-button house-button-secondary house-button-sm" disabled={submittingHoldingID === holding.holdingID} type="submit">{holding.active ? 'Deactivate' : 'Activate'}</button>
                        </form>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryHoldingID === holding.holdingID}
                    {@const history = historyByHoldingID[holding.holdingID]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="10">
                        <div>
                          {#if history?.loading}
                            <div class="text-sm text-slate-600">Loading history...</div>
                          {:else if history?.error}
                            <div class="status-panel status-panel-error">{history.error}</div>
                          {:else}
                            <HistoryEventsCard
                              eventDateTime={data.valuationDate}
                              asAtDateTime={data.auditDateTime}
                              events={history?.events ?? []}
                              emptyMessage="No history found for this holding."
                            />
                          {/if}
                        </div>
                      </td>
                    </tr>
                  {/if}
                {/if}
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </section>
</main>
