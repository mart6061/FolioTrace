<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import TableTools from '$lib/components/page/TableTools.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput } from '$lib/dates';
  import type { TableExportDefinition } from '$lib/export';
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

  const fxExportDefinition = $derived.by((): TableExportDefinition => ({
    fileName: 'fxs',
    sheetName: 'FXs',
    columns: [
      { key: 'pair', label: 'Pair', kind: 'text' },
      { key: 'displayPair', label: 'Display pair', kind: 'text' },
      { key: 'baseCurrency', label: 'Base', kind: 'text' },
      { key: 'quoteCurrency', label: 'Quote', kind: 'text' },
      { key: 'active', label: 'Active', kind: 'boolean' },
      { key: 'lastAuditDateTime', label: 'Last audit', kind: 'datetime' }
    ],
    rows: filteredFXs.map((fx) => ({
      active: fx.active,
      baseCurrency: fx.baseCurrency,
      displayPair: fx.displayPair,
      lastAuditDateTime: fx.lastAuditDateTime,
      pair: fx.pair,
      quoteCurrency: fx.quoteCurrency
    }))
  }));

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
        <TableTools bind:filterText filterLabel="Filter FXs" placeholder="Filter FXs..." onadd={startAdd} exportDefinition={fxExportDefinition} onprint={printTable} />

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
