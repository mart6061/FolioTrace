<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import EventPropertyDetails from '$lib/components/EventPropertyDetails.svelte';
  import { formatDisplayDateTime, formatShortDate, formatTableDateTime, isSameInputDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { FXRate, FXRateHistoryEvent } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  let filterText = $state('');
  let addingPair = $state('');
  let editingPair = $state('');
  let openHistoryPair = $state('');
  let submittingPair = $state('');
  let historyByPair = $state<Record<string, { events: FXRateHistoryEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');

  const asOfSummary = $derived(data.auditDateTime && data.fxRates ? formatDisplayDateTime(data.fxRates.asOfDateTime) : 'now');
  const ratesByPair = $derived(new Map((data.fxRates?.items ?? []).map((rate) => [rate.pair, rate])));
  const rows = $derived(
    (data.fxs?.items ?? []).filter((fx) => {
      const filter = filterText.trim().toLocaleLowerCase();
      if (!filter)
        return true;

      return [fx.pair, fx.displayPair, fx.baseCurrency, fx.quoteCurrency, fx.active ? 'active' : 'inactive']
        .some((value) => value.toLocaleLowerCase().includes(filter));
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
    if (openHistoryPair)
      void loadHistory(openHistoryPair, ratesByPair.get(openHistoryPair)?.valuationDateTime ?? null);
  });

  function priceValue(rate: FXRate | undefined, key: 'bid' | 'mid' | 'ask') {
    return rate ? rate.price[key].toString() : '';
  }

  function formatPrice(value: number) {
    return value.toLocaleString(undefined, { maximumFractionDigits: 8, minimumFractionDigits: 0 });
  }

  function staleDataLine(valueDateTime: string | undefined) {
    return valueDateTime && !isSameInputDateTime(valueDateTime, data.valuationDate)
      ? `most recent available: ${formatShortDate(valueDateTime)}`
      : '';
  }

  function rateExportRows() {
    return rows.map((fx) => {
      const rate = ratesByPair.get(fx.pair);

      return {
        active: fx.active ? 'Active' : 'Inactive',
        ask: rate ? formatPrice(rate.price.ask) : '',
        bid: rate ? formatPrice(rate.price.bid) : '',
        displayPair: fx.displayPair,
        lastAuditDateTime: rate?.lastAuditDateTime ?? '',
        mid: rate ? formatPrice(rate.price.mid) : '',
        pair: fx.pair
      };
    });
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
    downloadFile('fx-rates.json', JSON.stringify(rateExportRows(), null, 2), 'application/json');
  }

  function exportCsv() {
    const rows = rateExportRows();
    const header = ['Pair', 'Display pair', 'Active', 'Bid', 'Mid', 'Ask', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...rows.map((row) =>
        [row.pair, row.displayPair, row.active, row.bid, row.mid, row.ask, row.lastAuditDateTime]
          .map(csvValue)
          .join(',')
      )
    ];

    downloadFile('fx-rates.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const rows = rateExportRows();
    const html = `
      <table>
        <thead>
          <tr>
            <th>Pair</th>
            <th>Display pair</th>
            <th>Active</th>
            <th>Bid</th>
            <th>Mid</th>
            <th>Ask</th>
            <th>Last audit</th>
          </tr>
        </thead>
        <tbody>
          ${rows.map((row) => `
            <tr>
              <td>${htmlValue(row.pair)}</td>
              <td>${htmlValue(row.displayPair)}</td>
              <td>${htmlValue(row.active)}</td>
              <td>${htmlValue(row.bid)}</td>
              <td>${htmlValue(row.mid)}</td>
              <td>${htmlValue(row.ask)}</td>
              <td>${htmlValue(row.lastAuditDateTime)}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    `;

    downloadFile('fx-rates.xls', html, 'application/vnd.ms-excel');
  }

  function printTable() {
    window.print();
  }

  async function toggleHistory(pair: string, rate: FXRate | undefined) {
    if (openHistoryPair === pair) {
      openHistoryPair = '';
      delete historyByPair[pair];
      return;
    }

    openHistoryPair = pair;

    if (historyByPair[pair])
      return;

    await loadHistory(pair, rate?.valuationDateTime ?? null);
  }

  async function loadHistory(pair: string, rateValuationDateTime: string | null = null) {
    historyByPair[pair] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Value/FXRates/History', window.location.origin);
      historyUrl.searchParams.set('pair', pair);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      if (rateValuationDateTime)
        historyUrl.searchParams.set('rateValuationDateTime', rateValuationDateTime);

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByPair[pair] = {
        events: await response.json() as FXRateHistoryEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByPair[pair] = {
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
      data.fxRates?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
  }

  function closeEditor() {
    addingPair = '';
    editingPair = '';
  }

  const enhanceRate: SubmitFunction = ({ formData }) => {
    const pair = formData.get('pair');
    submittingPair = typeof pair === 'string' ? pair : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingPair = '';

      if (result.type === 'success')
        closeEditor();
    };
  };
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <p class="page-kicker">Value Data</p>
        <div class="page-title-row">
          <h1 class="page-title">FX Rates</h1>
          <BookmarkButton />
        </div>
      </div>

      <form class="grid gap-4 md:grid-cols-[minmax(220px,280px)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="valuationDate" step="1" value={data.valuationDate} />
        </label>

        {#if data.auditDateTime}
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
        {/if}

        <button class="h-10 rounded-md bg-teal-700 px-4 text-sm font-semibold text-white shadow-sm hover:bg-teal-800" type="submit">Apply</button>
      </form>
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">{data.error}</div>
    {:else if data.fxRates && data.fxs}
      {#if form?.message}
        <div class={`mb-4 rounded-md border px-4 py-3 text-sm ${form.status === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-red-200 bg-red-50 text-red-800'}`} role="status">
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
        </div>
      {/if}

      <AggregateUpdateWatcher aggregateKind="FXRates" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.fxRates.lastEventID} />

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{data.fxRates.items.length}</span> FX rates</div>
        <div>Valuation {formatDisplayDateTime(data.fxRates.valuationDateTime)} · As-of {asOfSummary}</div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter FX rates</span>
            <input bind:value={filterText} placeholder="Filter FX rates..." type="search" />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Export FX rates to JSON" onclick={exportJson} title="Export JSON" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" /></svg>
            </button>
            <button aria-label="Export FX rates to CSV" onclick={exportCsv} title="Export CSV" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 4h16v16H4zM4 10h16M10 4v16" /></svg>
            </button>
            <button aria-label="Export FX rates to XLSX" onclick={exportXlsx} title="Export XLSX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" /></svg>
            </button>
            <button aria-label="Print FX rates" onclick={printTable} title="Print" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" /></svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">Pair</th>
                <th class="px-3 py-2">Active</th>
                <th class="px-3 py-2 text-right">Bid</th>
                <th class="px-3 py-2 text-right">Mid</th>
                <th class="px-3 py-2 text-right">Ask</th>
                <th class="px-3 py-2">Last audit</th>
                <th class="w-28 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each rows as fx}
                {@const rate = ratesByPair.get(fx.pair)}
                {#if addingPair === fx.pair || editingPair === fx.pair}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      <form id={`fx-rate-edit-${fx.pair}`} action="?/setFXRate" method="POST" use:enhance={enhanceRate}>
                        <input name="pair" type="hidden" value={fx.pair} />
                      </form>
                      <div class="font-medium text-slate-950">{fx.displayPair}</div>
                      <div class="font-mono text-xs text-slate-500">{fx.pair}</div>
                    </td>
                    <td class="px-3 py-2">
                      <span class={`rounded px-2 py-1 text-xs font-semibold ${fx.active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'}`}>{fx.active ? 'Active' : 'Inactive'}</span>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`fx-rate-edit-${fx.pair}`}>
                        <span>Bid</span>
                        <input class="h-8 w-28 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-slate-950" form={`fx-rate-edit-${fx.pair}`} min="0" name="bid" required step="0.00000001" type="number" value={form?.pair === fx.pair ? (form.values?.bid ?? priceValue(rate, 'bid')) : priceValue(rate, 'bid')} />
                      </label>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`fx-rate-edit-${fx.pair}`}>
                        <span>Mid</span>
                        <input class="h-8 w-28 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-slate-950" form={`fx-rate-edit-${fx.pair}`} min="0" name="mid" required step="0.00000001" type="number" value={form?.pair === fx.pair ? (form.values?.mid ?? priceValue(rate, 'mid')) : priceValue(rate, 'mid')} />
                      </label>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`fx-rate-edit-${fx.pair}`}>
                        <span>Ask</span>
                        <input class="h-8 w-28 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-slate-950" form={`fx-rate-edit-${fx.pair}`} min="0" name="ask" required step="0.00000001" type="number" value={form?.pair === fx.pair ? (form.values?.ask ?? priceValue(rate, 'ask')) : priceValue(rate, 'ask')} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`fx-rate-edit-${fx.pair}`}>
                        <span>Event date</span>
                        <DateTimeInput class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-slate-950" form={`fx-rate-edit-${fx.pair}`} name="eventDateTime" required step="1" value={form?.pair === fx.pair ? (form.values?.eventDateTime ?? eventDateDefault) : eventDateDefault} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                        <span>Actions</span>
                        <div class="flex justify-end gap-2">
                          <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700" onclick={closeEditor} type="button">Cancel</button>
                          <button class="h-8 rounded-md bg-teal-700 px-3 text-sm font-medium text-white disabled:opacity-70" disabled={submittingPair === fx.pair} form={`fx-rate-edit-${fx.pair}`} type="submit">{submittingPair === fx.pair ? 'Saving' : 'Save'}</button>
                        </div>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="hover:bg-slate-50">
                    <td class="px-3 py-2">
                      <div class="font-medium text-slate-950">{fx.displayPair}</div>
                      <div class="font-mono text-xs text-slate-500">{fx.pair}</div>
                      {#if staleDataLine(rate?.valuationDateTime)}
                        <div class="mt-1 text-xs text-slate-400">{staleDataLine(rate?.valuationDateTime)}</div>
                      {/if}
                    </td>
                    <td class="px-3 py-2">
                      <span class={`rounded px-2 py-1 text-xs font-semibold ${fx.active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'}`}>{fx.active ? 'Active' : 'Inactive'}</span>
                    </td>
                    <td class="px-3 py-2 text-right font-mono text-slate-700">{rate ? formatPrice(rate.price.bid) : '-'}</td>
                    <td class="px-3 py-2 text-right font-mono text-slate-700">{rate ? formatPrice(rate.price.mid) : '-'}</td>
                    <td class="px-3 py-2 text-right font-mono text-slate-700">{rate ? formatPrice(rate.price.ask) : '-'}</td>
                    <td class="px-3 py-2 text-slate-600">{rate ? formatTableDateTime(rate.lastAuditDateTime) : '-'}</td>
                    <td class="px-3 py-2 text-right">
                      <div class="flex justify-end gap-2">
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => toggleHistory(fx.pair, rate)} type="button">
                          {openHistoryPair === fx.pair ? 'Hide' : 'History'}
                        </button>
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => rate ? editingPair = fx.pair : addingPair = fx.pair} type="button">
                          {rate ? 'Edit' : 'Add'}
                        </button>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryPair === fx.pair}
                    {@const history = historyByPair[fx.pair]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="7">
                        <div class="grid gap-3 rounded-md border border-slate-200 bg-white p-3">
                          <div class="flex items-center justify-between gap-3">
                            <h2 class="text-sm font-semibold text-slate-950">{fx.displayPair} history</h2>
                            <span class="text-xs text-slate-500">
                              {history?.events.length ?? 0} events
                              {#if data.auditDateTime && history?.events.length}
                                | {history.events.filter((event) => event.applicationStatus === 'omitted').length} omitted
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
                                    <div class="text-sm text-slate-700">{event.summary}</div>
                                    <EventPropertyDetails details={event.propertyDetails} />
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
                            <div class="text-sm text-slate-600">No history found for this FX rate.</div>
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
