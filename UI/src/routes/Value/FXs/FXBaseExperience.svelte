<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput } from '$lib/dates';
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
  let filterText = $state('');
  let addingFX = $state(false);
  let submittingCreate = $state(false);
  let submittingPair = $state('');

  const fxCount = $derived(data.fxs?.items.length ?? 0);
  const countryOptions = $derived(data.countryOptions ?? []);
  const asOfSummary = $derived(data.auditDateTime && data.fxs ? formatDisplayDateTime(data.fxs.asOfDateTime) : 'now');
  const filteredFXs = $derived(
    (data.fxs?.items ?? []).filter((fx) => {
      const filter = filterText.trim().toLocaleLowerCase();
      if (!filter)
        return true;

      return [fx.pair, fx.displayPair, fx.baseCurrency, fx.quoteCurrency, fx.active ? 'active' : 'inactive']
        .some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  function fxExportRows() {
    return filteredFXs.map((fx) => ({
      active: fx.active ? 'Active' : 'Inactive',
      baseCurrency: fx.baseCurrency,
      displayPair: fx.displayPair,
      lastAuditDateTime: fx.lastAuditDateTime,
      pair: fx.pair,
      quoteCurrency: fx.quoteCurrency
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
    downloadFile('fxs.json', JSON.stringify(fxExportRows(), null, 2), 'application/json');
  }

  function exportCsv() {
    const rows = fxExportRows();
    const header = ['Pair', 'Display pair', 'Base', 'Quote', 'Active', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...rows.map((row) =>
        [row.pair, row.displayPair, row.baseCurrency, row.quoteCurrency, row.active, row.lastAuditDateTime]
          .map(csvValue)
          .join(',')
      )
    ];

    downloadFile('fxs.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const rows = fxExportRows();
    const html = `
      <table>
        <thead>
          <tr>
            <th>Pair</th>
            <th>Display pair</th>
            <th>Base</th>
            <th>Quote</th>
            <th>Active</th>
            <th>Last audit</th>
          </tr>
        </thead>
        <tbody>
          ${rows.map((row) => `
            <tr>
              <td>${htmlValue(row.pair)}</td>
              <td>${htmlValue(row.displayPair)}</td>
              <td>${htmlValue(row.baseCurrency)}</td>
              <td>${htmlValue(row.quoteCurrency)}</td>
              <td>${htmlValue(row.active)}</td>
              <td>${htmlValue(row.lastAuditDateTime)}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    `;

    downloadFile('fxs.xls', html, 'application/vnd.ms-excel');
  }

  function printTable() {
    window.print();
  }

  function startAdd() {
    addingFX = true;
  }

  function cancelAdd() {
    addingFX = false;
  }

  const enhanceFXCreate: SubmitFunction = () => {
    submittingCreate = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;

      if (result.type === 'success')
        addingFX = false;
    };
  };

  const enhanceActive: SubmitFunction = ({ formData }) => {
    const pair = formData.get('pair');
    submittingPair = typeof pair === 'string' ? pair : '';

    return async ({ update }) => {
      await update({ reset: false });
      submittingPair = '';
    };
  };
</script>

<main class={shellClass}>
  {#if showFilter}
  <section class="page-header">
    <div class="page-container">
      {#if showHeader}
        <div class="page-header-main">
          <p class="page-kicker">Value Data</p>
          <div class="page-title-row">
            <h1 class="page-title">FXs</h1>
            <BookmarkButton />
          </div>
        </div>
      {/if}

      <form action={formAction} class="house-form grid gap-4 md:grid-cols-[var(--house-datetime-width)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput fullWidth name="valuationDate" step="1" value={data.valuationDate} />
        </label>

        {#if selectedSection}
          <input name="section" type="hidden" value={selectedSection} />
        {/if}

        {#if data.auditDateTime}
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
        {/if}

        <button class="house-button house-button-primary house-button-md" type="submit">Apply</button>
      </form>
    </div>
  </section>
  {/if}

  {#if showBody}
  <section class="page-container page-section">
    <datalist id="fx-country-alpha3-options">
      {#each countryOptions as country}
        <option value={country.alpha3}>{country.name}</option>
      {/each}
    </datalist>

    {#if data.error}
      <Card density="compact" intent="error">{data.error}</Card>
    {:else if data.fxs}
      {#if form?.message}
        <Card class="mb-4" density="compact" intent={form.status === 'success' ? 'success' : 'error'} role="status">
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
        </Card>
      {/if}

      <AggregateUpdateWatcher aggregateKind="FXs" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.fxs.lastEventID} />

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{fxCount}</span> FXs</div>
        <div>Valuation {formatDisplayDateTime(data.fxs.valuationDateTime)} · As-of {asOfSummary}</div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter FXs</span>
            <input bind:value={filterText} placeholder="Filter FXs..." type="search" />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add FX" onclick={startAdd} title="Add FX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
            </button>
            <button aria-label="Export FXs to JSON" onclick={exportJson} title="Export JSON" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" /></svg>
            </button>
            <button aria-label="Export FXs to CSV" onclick={exportCsv} title="Export CSV" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 4h16v16H4zM4 10h16M10 4v16" /></svg>
            </button>
            <button aria-label="Export FXs to XLSX" onclick={exportXlsx} title="Export XLSX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" /></svg>
            </button>
            <button aria-label="Print FXs" onclick={printTable} title="Print" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" /></svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">Pair</th>
                <th class="px-3 py-2">Base</th>
                <th class="px-3 py-2">Quote</th>
                <th class="px-3 py-2">Active</th>
                <th class="px-3 py-2">Last audit</th>
                <th class="w-32 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#if addingFX}
                <tr class="bg-teal-50/30 align-top">
                  <td class="px-3 py-2">
                    <form id="fx-create" action="?/createFX" method="POST" use:enhance={enhanceFXCreate}></form>
                    <div class="grid gap-1 text-xs font-medium text-slate-600">
                      <span>Pair</span>
                      <span class="h-8 py-1.5 text-sm font-normal text-slate-700">New FX</span>
                    </div>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="fx-create">
                      Base
                      <input class="house-control house-control-sm w-36 font-mono uppercase" form="fx-create" list="fx-country-alpha3-options" maxlength="3" minlength="3" name="baseCurrency" placeholder="Alpha-3" required value={form?.intent === 'createFX' ? (form.values?.baseCurrency ?? '') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="fx-create">
                      Quote
                      <input class="house-control house-control-sm w-36 font-mono uppercase" form="fx-create" list="fx-country-alpha3-options" maxlength="3" minlength="3" name="quoteCurrency" placeholder="Alpha-3" required value={form?.intent === 'createFX' ? (form.values?.quoteCurrency ?? '') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="flex h-full items-center gap-2 pt-5 text-xs font-medium text-slate-600" form="fx-create">
                      <input checked form="fx-create" name="active" type="checkbox" />
                      Active
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="fx-create">
                      Event date
                      <DateTimeInput size="sm" form="fx-create" name="eventDateTime" required step="1" value={form?.intent === 'createFX' ? (form.values?.eventDateTime ?? eventDateDefault) : eventDateDefault} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={cancelAdd} type="button">Cancel</button>
                        <button class="house-button house-button-primary house-button-sm" disabled={submittingCreate} form="fx-create" type="submit">{submittingCreate ? 'Adding' : 'Add'}</button>
                      </div>
                    </div>
                  </td>
                </tr>
              {/if}

              {#each filteredFXs as fx}
                <tr class="hover:bg-slate-50">
                  <td class="px-3 py-2">
                    <div class="font-medium text-slate-950">{fx.displayPair}</div>
                    <div class="font-mono text-xs text-slate-500">{fx.pair}</div>
                  </td>
                  <td class="px-3 py-2 font-mono text-slate-700">{fx.baseCurrency}</td>
                  <td class="px-3 py-2 font-mono text-slate-700">{fx.quoteCurrency}</td>
                  <td class="px-3 py-2">
                    <span class={`rounded px-2 py-1 text-xs font-semibold ${fx.active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'}`}>{fx.active ? 'Active' : 'Inactive'}</span>
                  </td>
                  <td class="px-3 py-2 text-slate-600">{formatTableDateTime(fx.lastAuditDateTime)}</td>
                  <td class="px-3 py-2 text-right">
                    <form action="?/modifyActive" method="POST" use:enhance={enhanceActive}>
                      <input name="pair" type="hidden" value={fx.pair} />
                      <input name="active" type="hidden" value={String(!fx.active)} />
                      <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                      <button class="house-button house-button-secondary house-button-sm" disabled={submittingPair === fx.pair} type="submit">
                        {fx.active ? 'Deactivate' : 'Activate'}
                      </button>
                    </form>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </section>
  {/if}
</main>
