<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { CurrencyReferenceEvent } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  type SortKey = 'currency' | 'alphabeticCode' | 'numericCode' | 'decimalPlace' | 'lastAudit';

  let sortKey = $state<SortKey>('currency');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let addingCurrency = $state(false);
  let editingCode = $state('');
  let submittingCode = $state('');
  let submittingCreate = $state(false);
  let openHistoryCode = $state('');
  let historyByCode = $state<Record<string, { events: CurrencyReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');

  const currencyCount = $derived(data.currencies?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.currencies ? formatDisplayDateTime(data.currencies.asOfDateTime) : 'now');

  const filteredCurrencies = $derived(
    (data.currencies?.items ?? []).filter((currency) => {
      const filter = filterText.trim().toLocaleLowerCase();

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

  function sortLabel(nextSortKey: SortKey) {
    if (sortKey !== nextSortKey)
      return '';

    return sortDirection === 1 ? ' ↑' : ' ↓';
  }

  function currencyExportRows() {
    return sortedCurrencies.map((currency) => ({
      alphabeticCode: currency.alphabeticCode,
      currency: currency.name,
      decimalPlace: currency.decimalPlace.toString(),
      lastAuditDateTime: currency.lastAuditDateTime,
      numericCode: currency.numericCode.toString().padStart(3, '0')
    }));
  }

  function downloadFile(fileName: string, content: string, mimeType: string) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = fileName;
    link.click();

    URL.revokeObjectURL(url);
  }

  function csvValue(value: string) {
    return `"${value.replaceAll('"', '""')}"`;
  }

  function htmlValue(value: string) {
    return value
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }

  function exportJson() {
    downloadFile('currencies.json', JSON.stringify(currencyExportRows(), null, 2), 'application/json');
  }

  function exportCsv() {
    const rows = currencyExportRows();
    const header = ['Currency', 'Alphabetic code', 'Numeric code', 'Decimal places', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...rows.map((row) =>
        [row.currency, row.alphabeticCode, row.numericCode, row.decimalPlace, row.lastAuditDateTime].map(csvValue).join(',')
      )
    ];

    downloadFile('currencies.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const rows = currencyExportRows();
    const html = `
      <table>
        <thead>
          <tr>
            <th>Currency</th>
            <th>Alphabetic code</th>
            <th>Numeric code</th>
            <th>Decimal places</th>
            <th>Last audit</th>
          </tr>
        </thead>
        <tbody>
          ${rows.map((row) => `
            <tr>
              <td>${htmlValue(row.currency)}</td>
              <td>${htmlValue(row.alphabeticCode)}</td>
              <td>${htmlValue(row.numericCode)}</td>
              <td>${htmlValue(row.decimalPlace)}</td>
              <td>${htmlValue(row.lastAuditDateTime)}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    `;

    downloadFile('currencies.xls', html, 'application/vnd.ms-excel');
  }

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

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <p class="page-kicker">Reference Data</p>
        <div class="page-title-row">
          <h1 class="page-title">Currencies</h1>
          <BookmarkButton />
        </div>
      </div>

      <form class="grid gap-4 md:grid-cols-[minmax(220px,280px)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput
            class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
            name="valuationDate"
            step="1"
            value={data.valuationDate}
          />
        </label>

        {#if data.auditDateTime}
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
        {/if}

        <button
          class="h-10 rounded-md bg-teal-700 px-4 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 focus:outline-none focus:ring-2 focus:ring-teal-600/30"
          type="submit"
        >
          Apply
        </button>
      </form>
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
        {data.error}
      </div>
    {:else if data.currencies}
      {#if form?.message}
        <div
          class={`mb-4 rounded-md border px-4 py-3 text-sm ${
            form.status === 'success'
              ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
              : 'border-red-200 bg-red-50 text-red-800'
          }`}
          role="status"
        >
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
        </div>
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
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter currencies</span>
            <input bind:value={filterText} placeholder="Filter currencies..." type="search" />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add currency" onclick={startAdd} title="Add currency" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
            </button>
            <button aria-label="Export currencies to JSON" onclick={exportJson} title="Export JSON" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" /></svg>
            </button>
            <button aria-label="Export currencies to CSV" onclick={exportCsv} title="Export CSV" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 4h16v16H4zM4 10h16M10 4v16" /></svg>
            </button>
            <button aria-label="Export currencies to XLSX" onclick={exportXlsx} title="Export XLSX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" /></svg>
            </button>
            <button aria-label="Print currencies" onclick={printTable} title="Print" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" /></svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('currency')} type="button">Currency{sortLabel('currency')}</button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('alphabeticCode')} type="button">Alphabetic{sortLabel('alphabeticCode')}</button>
                </th>
                <th class="px-3 py-2 text-right">
                  <button class="table-sort-button ml-auto" onclick={() => setSort('numericCode')} type="button">Numeric{sortLabel('numericCode')}</button>
                </th>
                <th class="px-3 py-2 text-right">
                  <button class="table-sort-button ml-auto" onclick={() => setSort('decimalPlace')} type="button">Decimals{sortLabel('decimalPlace')}</button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('lastAudit')} type="button">Last audit{sortLabel('lastAudit')}</button>
                </th>
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
                        <input class="h-8 w-full rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="name" required type="text" value={form?.intent === 'createCurrency' ? (form.values?.name ?? '') : ''} />
                      </label>
                    </form>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="currency-create">
                      <span>Alphabetic</span>
                      <input class="h-8 w-24 rounded-md border border-slate-300 bg-white px-2 font-mono text-sm uppercase text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form="currency-create" maxlength="3" minlength="3" name="alphabeticCode" required type="text" value={form?.intent === 'createCurrency' ? (form.values?.alphabeticCode ?? '') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2 text-right">
                    <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form="currency-create">
                      <span>Numeric</span>
                      <input class="h-8 w-24 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form="currency-create" max="999" min="0" name="numericCode" required type="number" value={form?.intent === 'createCurrency' ? (form.values?.numericCode ?? '') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2 text-right">
                    <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form="currency-create">
                      <span>Decimals</span>
                      <input class="h-8 w-20 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form="currency-create" min="0" name="decimalPlace" required type="number" value={form?.intent === 'createCurrency' ? (form.values?.decimalPlace ?? '') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="currency-create">
                      <span>Event date</span>
                      <DateTimeInput class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form="currency-create" name="eventDateTime" required step="1" value={form?.intent === 'createCurrency' ? (form.values?.eventDateTime ?? eventDateDefault) : eventDateDefault} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400" onclick={cancelAdd} type="button">Cancel</button>
                        <button class="h-8 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70" disabled={submittingCreate} form="currency-create" type="submit">{submittingCreate ? 'Adding' : 'Add'}</button>
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
                          <input class="h-8 w-full rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="name" required type="text" value={form?.alphabeticCode === currency.alphabeticCode ? (form.values?.name ?? currency.name) : currency.name} />
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
                        <input class="h-8 w-24 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`currency-edit-${currency.alphabeticCode}`} max="999" min="0" name="numericCode" required type="number" value={form?.alphabeticCode === currency.alphabeticCode ? (form.values?.numericCode ?? currency.numericCode.toString().padStart(3, '0')) : currency.numericCode.toString().padStart(3, '0')} />
                      </label>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`currency-edit-${currency.alphabeticCode}`}>
                        <span>Decimals</span>
                        <input class="h-8 w-20 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`currency-edit-${currency.alphabeticCode}`} min="0" name="decimalPlace" required type="number" value={form?.alphabeticCode === currency.alphabeticCode ? (form.values?.decimalPlace ?? currency.decimalPlace) : currency.decimalPlace} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`currency-edit-${currency.alphabeticCode}`}>
                        <span>Event date</span>
                        <DateTimeInput class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`currency-edit-${currency.alphabeticCode}`} name="eventDateTime" required step="1" value={form?.alphabeticCode === currency.alphabeticCode ? (form.values?.eventDateTime ?? eventDateDefault) : eventDateDefault} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                        <span>Actions</span>
                        <div class="flex justify-end gap-2">
                          <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400" onclick={cancelEdit} type="button">Cancel</button>
                          <button class="h-8 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70" disabled={submittingCode === currency.alphabeticCode} form={`currency-edit-${currency.alphabeticCode}`} type="submit">{submittingCode === currency.alphabeticCode ? 'Saving' : 'Save'}</button>
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
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => toggleHistory(currency.alphabeticCode)} type="button">
                          {openHistoryCode === currency.alphabeticCode ? 'Hide' : 'History'}
                        </button>
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => startEdit(currency.alphabeticCode)} type="button">
                          Edit
                        </button>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryCode === currency.alphabeticCode}
                    {@const history = historyByCode[currency.alphabeticCode]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="6">
                        <div class="grid gap-3 rounded-md border border-slate-200 bg-white p-3">
                          <div class="flex items-center justify-between gap-3">
                            <h2 class="text-sm font-semibold text-slate-950">{currency.alphabeticCode} history</h2>
                            <span class="text-xs text-slate-500">
                              {history?.events.length ?? 0} events
                              {#if data.auditDateTime && history?.events.length}
                                Â· {history.events.filter((event) => event.applicationStatus === 'omitted').length} omitted
                              {/if}
                            </span>
                          </div>

                          {#if history?.loading}
                            <div class="text-sm text-slate-600">Loading history...</div>
                          {:else if history?.error}
                            <div class="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">{history.error}</div>
                          {:else if history?.events.length}
                            <ol class="grid gap-2">
                              {#each history.events as event}
                                <li class={`grid gap-2 rounded-md border px-3 py-2 md:grid-cols-[180px_1fr] ${
                                  event.applicationStatus === 'omitted'
                                    ? 'border-amber-200 bg-amber-50/70'
                                    : 'border-slate-200'
                                }`}>
                                  <div class="text-xs text-slate-500">
                                    <div>{formatTableDateTime(event.eventDateTime)}</div>
                                    <div>Audit {formatTableDateTime(event.auditDateTime)}</div>
                                  </div>
                                  <div class="grid gap-1">
                                    <div class="flex flex-wrap items-center gap-2">
                                      <span class="font-medium text-slate-950">{event.$type}</span>
                                      {#if event.applicationStatus === 'omitted'}
                                        <span class="rounded-full border border-amber-300 bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-900">Not applied</span>
                                      {:else if data.auditDateTime}
                                        <span class="rounded-full border border-emerald-200 bg-emerald-50 px-2 py-0.5 text-xs font-semibold text-emerald-800">Applied</span>
                                      {/if}
                                      <span class="font-mono text-xs text-slate-500">{event.eventID}</span>
                                    </div>
                                    <div class="text-sm text-slate-700">{currencyEventSummary(event)}</div>
                                    {#if event.applicationStatus === 'omitted'}
                                      <div class="text-xs font-medium text-amber-900">
                                        Omitted from this view because its audit time is after the selected as-at date.
                                      </div>
                                    {/if}
                                    <div class="text-xs text-slate-500">{event.reason}</div>
                                  </div>
                                </li>
                              {/each}
                            </ol>
                          {:else}
                            <div class="text-sm text-slate-600">No history found for this currency.</div>
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
