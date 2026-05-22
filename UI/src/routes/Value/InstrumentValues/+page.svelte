<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import { formatDisplayDateTime, formatTableDateTime } from '$lib/dates';
  import type { InstrumentValue } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  let filterText = $state('');
  let editingInstrumentID = $state('');
  let submittingInstrumentID = $state('');

  const asOfSummary = $derived(data.auditDateTime && data.instrumentValues ? formatDisplayDateTime(data.instrumentValues.asOfDateTime) : 'now');
  const rows = $derived(
    (data.instrumentValues?.items ?? []).filter((instrument) => {
      const filter = filterText.trim().toLocaleLowerCase();
      if (!filter)
        return true;

      return [instrument.name, ticker(instrument), instrument.exchange, instrument.cfi]
        .some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  function ticker(instrument: InstrumentValue) {
    return instrument.identifiers.find((identifier) => String(identifier.type).toLocaleLowerCase() === 'ticker' || identifier.type === 2)?.value ?? '-';
  }

  function currency(instrument: InstrumentValue) {
    return instrument.price?.mid?.currency ?? (instrument.exchange === 'XTKS' ? 'JPY' : instrument.exchange === 'XNAS' ? 'USD' : instrument.exchange === 'XSWX' ? 'CHF' : instrument.exchange === 'XLON' ? 'GBP' : 'EUR');
  }

  function money(value: number | undefined) {
    return typeof value === 'number' ? value.toLocaleString(undefined, { maximumFractionDigits: 8 }) : '-';
  }

  function valueExportRows() {
    return rows.map((instrument) => ({
      ask: money(instrument.price?.ask.amount),
      bid: money(instrument.price?.bid.amount),
      exchange: instrument.exchange,
      instrumentID: instrument.instrumentID,
      lastAuditDateTime: instrument.lastAuditDateTime,
      mid: money(instrument.price?.mid.amount),
      name: instrument.name,
      nav: money(instrument.price?.nav.amount),
      ticker: ticker(instrument)
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
    return value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&#39;');
  }

  function exportJson() {
    downloadFile('instrument-values.json', JSON.stringify(valueExportRows(), null, 2), 'application/json');
  }

  function exportCsv() {
    const header = ['Instrument ID', 'Name', 'Ticker', 'Exchange', 'Bid', 'Mid', 'Ask', 'NAV', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...valueExportRows().map((row) =>
        [row.instrumentID, row.name, row.ticker, row.exchange, row.bid, row.mid, row.ask, row.nav, row.lastAuditDateTime]
          .map(csvValue)
          .join(',')
      )
    ];
    downloadFile('instrument-values.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const rows = valueExportRows();
    const html = `<table><thead><tr><th>Instrument ID</th><th>Name</th><th>Ticker</th><th>Exchange</th><th>Bid</th><th>Mid</th><th>Ask</th><th>NAV</th><th>Last audit</th></tr></thead><tbody>${rows.map((row) => `<tr><td>${htmlValue(row.instrumentID)}</td><td>${htmlValue(row.name)}</td><td>${htmlValue(row.ticker)}</td><td>${htmlValue(row.exchange)}</td><td>${htmlValue(row.bid)}</td><td>${htmlValue(row.mid)}</td><td>${htmlValue(row.ask)}</td><td>${htmlValue(row.nav)}</td><td>${htmlValue(row.lastAuditDateTime)}</td></tr>`).join('')}</tbody></table>`;
    downloadFile('instrument-values.xls', html, 'application/vnd.ms-excel');
  }

  function printTable() {
    window.print();
  }

  const enhancePrice: SubmitFunction = ({ formData }) => {
    const instrumentID = formData.get('instrumentID');
    submittingInstrumentID = typeof instrumentID === 'string' ? instrumentID : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingInstrumentID = '';

      if (result.type === 'success')
        editingInstrumentID = '';
    };
  };
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <p class="page-kicker">Value Data</p>
        <h1 class="page-title">Instrument Values</h1>
      </div>

      <form class="grid gap-4 md:grid-cols-[minmax(220px,280px)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <input class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="valuationDate" type="datetime-local" value={data.valuationDate} />
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
    {:else if data.instrumentValues}
      {#if form?.message}
        <div class={`mb-4 rounded-md border px-4 py-3 text-sm ${form.status === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-red-200 bg-red-50 text-red-800'}`} role="status">{form.message}</div>
      {/if}

      <AggregateUpdateWatcher aggregateKind="InstrumentValues" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.instrumentValues.lastEventID} />

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{data.instrumentValues.items.length}</span> instrument values</div>
        <div>Valuation {formatDisplayDateTime(data.instrumentValues.valuationDateTime)} · As-of {asOfSummary}</div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter instrument values</span>
            <input bind:value={filterText} placeholder="Filter instrument values..." type="search" />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Export instrument values to JSON" onclick={exportJson} title="Export JSON" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" /></svg>
            </button>
            <button aria-label="Export instrument values to CSV" onclick={exportCsv} title="Export CSV" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 4h16v16H4zM4 10h16M10 4v16" /></svg>
            </button>
            <button aria-label="Export instrument values to XLSX" onclick={exportXlsx} title="Export XLSX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" /></svg>
            </button>
            <button aria-label="Print instrument values" onclick={printTable} title="Print" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" /></svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">Instrument</th>
                <th class="px-3 py-2">Exchange</th>
                <th class="px-3 py-2 text-right">Bid</th>
                <th class="px-3 py-2 text-right">Mid</th>
                <th class="px-3 py-2 text-right">Ask</th>
                <th class="px-3 py-2 text-right">NAV</th>
                <th class="px-3 py-2">Last audit</th>
                <th class="w-28 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each rows as instrument}
                {#if editingInstrumentID === instrument.instrumentID}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      <form id={`instrument-price-edit-${instrument.instrumentID}`} action="?/setInstrumentPrice" method="POST" use:enhance={enhancePrice}>
                        <input name="instrumentID" type="hidden" value={instrument.instrumentID} />
                        <input name="currency" type="hidden" value={currency(instrument)} />
                      </form>
                      <div class="font-medium text-slate-950">{instrument.name}</div>
                      <div class="font-mono text-xs text-slate-500">{ticker(instrument)}</div>
                    </td>
                    <td class="px-3 py-2">{instrument.exchange}</td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                        <span>Bid</span>
                        <input class="h-8 w-28 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="bid" required step="0.00000001" type="number" value={instrument.price?.bid.amount ?? ''} />
                      </label>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                        <span>Mid</span>
                        <input class="h-8 w-28 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="mid" required step="0.00000001" type="number" value={instrument.price?.mid.amount ?? ''} />
                      </label>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                        <span>Ask</span>
                        <input class="h-8 w-28 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="ask" required step="0.00000001" type="number" value={instrument.price?.ask.amount ?? ''} />
                      </label>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                        <span>NAV</span>
                        <input class="h-8 w-28 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="nav" required step="0.00000001" type="number" value={instrument.price?.nav.amount ?? ''} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                        <span>Event date</span>
                        <input class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`instrument-price-edit-${instrument.instrumentID}`} name="eventDateTime" required type="datetime-local" value={data.valuationDate} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                        <span>Actions</span>
                        <div class="flex justify-end gap-2">
                          <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400" onclick={() => editingInstrumentID = ''} type="button">Cancel</button>
                          <button class="h-8 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70" disabled={submittingInstrumentID === instrument.instrumentID} form={`instrument-price-edit-${instrument.instrumentID}`} type="submit">{submittingInstrumentID === instrument.instrumentID ? 'Saving' : 'Save'}</button>
                        </div>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="hover:bg-slate-50">
                    <td class="px-3 py-2">
                      <div class="font-medium text-slate-950">{instrument.name}</div>
                      <div class="font-mono text-xs text-slate-500">{ticker(instrument)}</div>
                    </td>
                    <td class="px-3 py-2">{instrument.exchange}</td>
                    <td class="px-3 py-2 text-right font-mono">{money(instrument.price?.bid.amount)}</td>
                    <td class="px-3 py-2 text-right font-mono">{money(instrument.price?.mid.amount)}</td>
                    <td class="px-3 py-2 text-right font-mono">{money(instrument.price?.ask.amount)}</td>
                    <td class="px-3 py-2 text-right font-mono">{money(instrument.price?.nav.amount)}</td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(instrument.lastAuditDateTime)}</td>
                    <td class="px-3 py-2 text-right">
                      <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => editingInstrumentID = instrument.instrumentID} type="button">Edit</button>
                    </td>
                  </tr>
                {/if}
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </section>
</main>
