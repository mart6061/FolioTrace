<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import SortableHeader from '$lib/components/page/SortableHeader.svelte';
  import TableTools from '$lib/components/page/TableTools.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { TableExportDefinition } from '$lib/export';
  import type { CurrencyReferenceEvent } from '$lib/types';
  import type { ActionData, PageData, SubmitFunction } from './$types';

  type RenderMode = 'full' | 'filter' | 'body';

  let { data: pageData, form: actionForm, formAction = '', renderMode = 'full' as RenderMode, selectedSection = '' } = $props();

  const data = $derived(pageData as PageData);
  const form = $derived(actionForm as ActionData | undefined);

  const showHeader = $derived(renderMode === 'full');
  const showFilter = $derived(renderMode !== 'body');
  const showBody = $derived(renderMode !== 'filter');
  const shellClass = $derived(renderMode === 'full' ? 'min-h-screen' : `data-list-embedded-page data-list-embedded-${renderMode}`);

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  type SortKey = 'currency' | 'alphabeticCode' | 'numericCode' | 'decimalPlace' | 'lastAudit';

  let sortKey = $state<SortKey>('currency');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let debouncedFilterText = $state('');
  let addingCurrency = $state(false);
  let editingCode = $state('');
  let submittingCode = $state('');
  let submittingCreate = $state(false);
  let openHistoryCode = $state('');
  let historyByCode = $state<Record<string, { events: CurrencyReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');

  const currencyCount = $derived(data.currencies?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.currencies ? formatDisplayDateTime(data.currencies.asOfDateTime) : 'now');

  $effect(() => {
    const value = filterText;
    const timeout = setTimeout(() => {
      debouncedFilterText = value;
    }, 200);

    return () => clearTimeout(timeout);
  });

  const filteredCurrencies = $derived(
    (data.currencies?.items ?? []).filter((currency) => {
      const filter = debouncedFilterText.trim().toLocaleLowerCase();

      if (!filter)
        return true;

      return [
        currency.name,
        currency.alphabeticCode,
        currency.numericCode.toString().padStart(3, '0'),
        currency.decimalPlace.toString(),
        currency.lastAuditDateTime
      ].some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  const sortedCurrencies = $derived(
    [...filteredCurrencies].sort((left, right) => {
      const direction = sortDirection;

      switch (sortKey) {
        case 'alphabeticCode':
          return direction * left.alphabeticCode.localeCompare(right.alphabeticCode);
        case 'numericCode':
          return direction * (left.numericCode - right.numericCode);
        case 'decimalPlace':
          return direction * (left.decimalPlace - right.decimalPlace);
        case 'lastAudit':
          return direction * (new Date(left.lastAuditDateTime).getTime() - new Date(right.lastAuditDateTime).getTime());
        case 'currency':
        default:
          return direction * left.name.localeCompare(right.name);
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
    if (openHistoryCode)
      void loadHistory(openHistoryCode);
  });

  function setSort(nextSortKey: SortKey) {
    if (sortKey === nextSortKey) {
      sortDirection = sortDirection === 1 ? -1 : 1;
      return;
    }

    sortKey = nextSortKey;
    sortDirection = 1;
  }


  const currencyExportDefinition = $derived.by((): TableExportDefinition => ({
    fileName: 'currencies',
    sheetName: 'Currencies',
    columns: [
      { key: 'currency', label: 'Currency', kind: 'text' },
      { key: 'alphabeticCode', label: 'Alphabetic code', kind: 'text' },
      { key: 'numericCode', label: 'Numeric code', kind: 'text' },
      { key: 'decimalPlace', label: 'Decimal places', kind: 'number' },
      { key: 'lastAuditDateTime', label: 'Last audit', kind: 'datetime' }
    ],
    rows: sortedCurrencies.map((currency) => ({
      alphabeticCode: currency.alphabeticCode,
      currency: currency.name,
      decimalPlace: currency.decimalPlace,
      lastAuditDateTime: currency.lastAuditDateTime,
      numericCode: currency.numericCode.toString().padStart(3, '0')
    }))
  }));

  function printTable() {
    window.print();
  }

  function startEdit(alphabeticCode: string) {
    addingCurrency = false;
    editingCode = alphabeticCode;
  }

  function cancelEdit() {
    editingCode = '';
  }

  function startAdd() {
    editingCode = '';
    addingCurrency = true;
  }

  function cancelAdd() {
    addingCurrency = false;
  }

  const enhanceCurrencyCreate: SubmitFunction = () => {
    submittingCreate = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;

      if (result.type === 'success')
        addingCurrency = false;
    };
  };

  const enhanceCurrencyEdit: SubmitFunction = ({ formData }) => {
    const alphabeticCode = formData.get('alphabeticCode');

    submittingCode = typeof alphabeticCode === 'string' ? alphabeticCode : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCode = '';

      if (result.type === 'success')
        editingCode = '';
    };
  };

  async function toggleHistory(alphabeticCode: string) {
    if (openHistoryCode === alphabeticCode) {
      openHistoryCode = '';
      delete historyByCode[alphabeticCode];
      return;
    }

    openHistoryCode = alphabeticCode;

    if (historyByCode[alphabeticCode])
      return;

    await loadHistory(alphabeticCode);
  }

  async function loadHistory(alphabeticCode: string) {
    historyByCode[alphabeticCode] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Reference/Currencies/History', window.location.origin);
      historyUrl.searchParams.set('alphabeticCode', alphabeticCode);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByCode[alphabeticCode] = {
        events: await response.json() as CurrencyReferenceEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByCode[alphabeticCode] = {
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
      data.currencies?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
  }

  function currencyEventSummary(event: CurrencyReferenceEvent) {
    return [
      event.name,
      event.alphabeticCode,
      event.numericCode.toString().padStart(3, '0'),
      `${event.decimalPlace} decimals`
    ].filter(Boolean).join(' · ');
  }
</script>

<main class={shellClass}>
  {#if showFilter}
  <section class="page-header">
    <div class="page-container">
      {#if showHeader}
        <div class="page-header-main">
          <p class="page-kicker">Reference Data</p>
          <div class="page-title-row">
            <h1 class="page-title">Currencies</h1>
            <BookmarkButton />
          </div>
        </div>
      {/if}

      <form action={formAction} class="house-form grid gap-4 md:grid-cols-[var(--house-datetime-width)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput
            fullWidth
            name="valuationDate"
            step="1"
            value={data.valuationDate}
          />
        </label>

        {#if selectedSection}
          <input name="section" type="hidden" value={selectedSection} />
        {/if}

        {#if data.auditDateTime}
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
        {/if}

        <button
          class="house-button house-button-primary house-button-md"
          type="submit"
        >
          Apply
        </button>
      </form>
    </div>
  </section>
  {/if}

  {#if showBody}
  <section class="page-container page-section">
    {#if data.error}
      <Card density="compact" intent="error">
        {data.error}
      </Card>
    {:else if data.currencies}
      {#if form?.message}
        <Card class="mb-4" density="compact" intent={form.status === 'success' ? 'success' : 'error'} role="status">
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
        </Card>
      {/if}

      <AggregateUpdateWatcher aggregateKind="Currencies" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.currencies.lastEventID} />

      <div class="data-summary">
        <div>
          <span class="font-semibold text-slate-950">{currencyCount}</span>
          currencies
        </div>
        <div>
          Valuation {formatDisplayDateTime(data.currencies.valuationDateTime)} · As-of {asOfSummary}
        </div>
      </div>

      <div class="data-panel">
        <TableTools bind:filterText filterLabel="Filter currencies" placeholder="Filter currencies..." onadd={startAdd} exportDefinition={currencyExportDefinition} onprint={printTable} />

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <SortableHeader activeKey={sortKey} class="px-3 py-2" direction={sortDirection} onsort={setSort} sortKey="currency">Currency</SortableHeader>
                <SortableHeader activeKey={sortKey} class="px-3 py-2" direction={sortDirection} onsort={setSort} sortKey="alphabeticCode">Alphabetic</SortableHeader>
                <SortableHeader activeKey={sortKey} class="px-3 py-2 text-right" buttonClass="ml-auto" direction={sortDirection} onsort={setSort} sortKey="numericCode">Numeric</SortableHeader>
                <SortableHeader activeKey={sortKey} class="px-3 py-2 text-right" buttonClass="ml-auto" direction={sortDirection} onsort={setSort} sortKey="decimalPlace">Decimals</SortableHeader>
                <SortableHeader activeKey={sortKey} class="px-3 py-2" direction={sortDirection} onsort={setSort} sortKey="lastAudit">Last audit</SortableHeader>
                <th class="w-40 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#if addingCurrency}
                <tr class="bg-teal-50/30 align-top">
                  <td class="px-3 py-2">
                    <form id="currency-create" action="?/createCurrency" method="POST" use:enhance={enhanceCurrencyCreate}>
                      <label class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Currency</span>
                        <input class="house-control house-control-sm house-control-full" name="name" required type="text" value={form?.intent === 'createCurrency' ? (form.values?.name ?? '') : ''} />
                      </label>
                    </form>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="currency-create">
                      <span>Alphabetic</span>
                      <input class="house-control house-control-sm w-24 font-mono uppercase" form="currency-create" maxlength="3" minlength="3" name="alphabeticCode" required type="text" value={form?.intent === 'createCurrency' ? (form.values?.alphabeticCode ?? '') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2 text-right">
                    <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form="currency-create">
                      <span>Numeric</span>
                      <input class="house-control house-control-sm w-24 text-right font-mono" form="currency-create" max="999" min="0" name="numericCode" required type="number" value={form?.intent === 'createCurrency' ? (form.values?.numericCode ?? '') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2 text-right">
                    <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form="currency-create">
                      <span>Decimals</span>
                      <input class="house-control house-control-sm w-20 text-right font-mono" form="currency-create" min="0" name="decimalPlace" required type="number" value={form?.intent === 'createCurrency' ? (form.values?.decimalPlace ?? '') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="currency-create">
                      <span>Event date</span>
                      <DateTimeInput size="sm" form="currency-create" name="eventDateTime" required step="1" value={form?.intent === 'createCurrency' ? (form.values?.eventDateTime ?? eventDateDefault) : eventDateDefault} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={cancelAdd} type="button">Cancel</button>
                        <button class="house-button house-button-primary house-button-sm" disabled={submittingCreate} form="currency-create" type="submit">{submittingCreate ? 'Adding' : 'Add'}</button>
                      </div>
                    </div>
                  </td>
                </tr>
              {/if}

              {#each sortedCurrencies as currency}
                {#if editingCode === currency.alphabeticCode}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      <form id={`currency-edit-${currency.alphabeticCode}`} action="?/modifyCurrency" method="POST" use:enhance={enhanceCurrencyEdit}>
                        <input name="alphabeticCode" type="hidden" value={currency.alphabeticCode} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Currency</span>
                          <input class="house-control house-control-sm house-control-full" name="name" required type="text" value={form?.alphabeticCode === currency.alphabeticCode ? (form.values?.name ?? currency.name) : currency.name} />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Alphabetic</span>
                        <span class="h-8 py-1.5 font-mono text-sm font-normal text-slate-700">{currency.alphabeticCode}</span>
                      </div>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`currency-edit-${currency.alphabeticCode}`}>
                        <span>Numeric</span>
                        <input class="house-control house-control-sm w-24 text-right font-mono" form={`currency-edit-${currency.alphabeticCode}`} max="999" min="0" name="numericCode" required type="number" value={form?.alphabeticCode === currency.alphabeticCode ? (form.values?.numericCode ?? currency.numericCode.toString().padStart(3, '0')) : currency.numericCode.toString().padStart(3, '0')} />
                      </label>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`currency-edit-${currency.alphabeticCode}`}>
                        <span>Decimals</span>
                        <input class="house-control house-control-sm w-20 text-right font-mono" form={`currency-edit-${currency.alphabeticCode}`} min="0" name="decimalPlace" required type="number" value={form?.alphabeticCode === currency.alphabeticCode ? (form.values?.decimalPlace ?? currency.decimalPlace) : currency.decimalPlace} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`currency-edit-${currency.alphabeticCode}`}>
                        <span>Event date</span>
                        <DateTimeInput size="sm" form={`currency-edit-${currency.alphabeticCode}`} name="eventDateTime" required step="1" value={form?.alphabeticCode === currency.alphabeticCode ? (form.values?.eventDateTime ?? eventDateDefault) : eventDateDefault} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                        <span>Actions</span>
                        <div class="flex justify-end gap-2">
                          <button class="house-button house-button-secondary house-button-sm" onclick={cancelEdit} type="button">Cancel</button>
                          <button class="house-button house-button-primary house-button-sm" disabled={submittingCode === currency.alphabeticCode} form={`currency-edit-${currency.alphabeticCode}`} type="submit">{submittingCode === currency.alphabeticCode ? 'Saving' : 'Save'}</button>
                        </div>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="hover:bg-slate-50">
                    <td class="px-3 py-2 font-medium text-slate-950">{currency.name}</td>
                    <td class="px-3 py-2 font-mono text-slate-700">{currency.alphabeticCode}</td>
                    <td class="px-3 py-2 text-right font-mono text-slate-700">{currency.numericCode.toString().padStart(3, '0')}</td>
                    <td class="px-3 py-2 text-right font-mono text-slate-700">{currency.decimalPlace}</td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(currency.lastAuditDateTime)}</td>
                    <td class="px-3 py-2">
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={() => toggleHistory(currency.alphabeticCode)} type="button">
                          {openHistoryCode === currency.alphabeticCode ? 'Hide' : 'History'}
                        </button>
                        <button class="house-button house-button-secondary house-button-sm" onclick={() => startEdit(currency.alphabeticCode)} type="button">
                          Edit
                        </button>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryCode === currency.alphabeticCode}
                    {@const history = historyByCode[currency.alphabeticCode]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="6">
                        <div>
                          {#if history?.loading}
                            <div class="text-sm text-slate-600">Loading history...</div>
                          {:else if history?.error}
                            <Card density="compact" intent="error">{history.error}</Card>
                          {:else}
                            <HistoryEventsCard
                              eventDateTime={data.valuationDate}
                              asAtDateTime={data.auditDateTime}
                              events={history?.events ?? []}
                              emptyMessage="No history found for this currency."
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
  {/if}
</main>
