<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import { csvValue, downloadFile, htmlValue } from '$lib/export';
  import type { InstrumentReferenceEvent } from '$lib/types';
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
  let addingInstrument = $state(false);
  let editingInstrumentID = $state('');
  let submittingIdentifierKey = $state('');
  let submittingInstrumentID = $state('');
  let submittingCreate = $state(false);
  let openHistoryInstrumentID = $state('');
  let historyByInstrumentID = $state<Record<string, { events: InstrumentReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');
  const identifierTypeOptions = ['Ticker', 'Sedol', 'ISIN', 'CUSIP', 'FIGI', 'RIC'];

  const asOfSummary = $derived(data.auditDateTime && data.instruments ? formatDisplayDateTime(data.instruments.asOfDateTime) : 'now');
  const rows = $derived(
    (data.instruments?.items ?? []).filter((instrument) => {
      const filter = filterText.trim().toLocaleLowerCase();
      if (!filter)
        return true;

      return [
        instrument.name,
        instrument.formalName,
        instrument.exchange,
        instrument.cfi,
        instrument.identifiers.map((identifier) => `${identifier.type} ${identifier.value}`).join(' ')
      ].some((value) => value.toLocaleLowerCase().includes(filter));
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
    if (openHistoryInstrumentID)
      void loadHistory(openHistoryInstrumentID);
  });

  function ticker(instrument: { identifiers: { type: string | number; value: string }[] }) {
    return instrument.identifiers.find((identifier) => String(identifier.type).toLocaleLowerCase() === 'ticker' || identifier.type === 2)?.value ?? '-';
  }

  function identifierTypeName(type: string | number) {
    const value = String(type);
    const lookup: Record<string, string> = {
      '0': 'SEDOL',
      '1': 'ISIN',
      '2': 'Ticker',
      '3': 'CUSIP',
      '4': 'FIGI',
      '5': 'RIC',
      isin: 'ISIN',
      sedol: 'SEDOL',
      ticker: 'Ticker',
      cusip: 'CUSIP',
      figi: 'FIGI',
      ric: 'RIC'
    };

    return lookup[value.toLocaleLowerCase()] ?? value;
  }

  function identifierTypeValue(type: string | number) {
    const value = String(type);
    const lookup: Record<string, string> = {
      '0': 'Sedol',
      '1': 'ISIN',
      '2': 'Ticker',
      '3': 'CUSIP',
      '4': 'FIGI',
      '5': 'RIC',
      isin: 'ISIN',
      sedol: 'Sedol',
      ticker: 'Ticker',
      cusip: 'CUSIP',
      figi: 'FIGI',
      ric: 'RIC'
    };

    return lookup[value.toLocaleLowerCase()] ?? value;
  }

  function identifiersSummary(instrument: { identifiers: { type: string | number; value: string }[] }) {
    return instrument.identifiers.map((identifier) => `${identifierTypeName(identifier.type)}: ${identifier.value}`).join(' | ');
  }

  function svgDataUrl(svg: string) {
    return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
  }

  function createValue(key: 'cfi' | 'eventDateTime' | 'exchange' | 'formalName' | 'incomeCountry' | 'name' | 'priceCountry' | 'priceCurrency' | 'ticker') {
    const values = form?.intent === 'createInstrument' ? form.values as Record<string, string> | undefined : undefined;
    return values?.[key] ?? '';
  }

  function editValue(instrumentID: string, key: 'cfi' | 'eventDateTime' | 'exchange' | 'formalName' | 'incomeCountry' | 'name' | 'priceCountry' | 'priceCurrency') {
    const values = form?.intent === 'modifyInstrument' && form.instrumentID === instrumentID ? form.values as Record<string, string> | undefined : undefined;
    return values?.[key];
  }

  function instrumentExportRows() {
    return rows.map((instrument) => ({
      active: instrument.active ? 'Active' : 'Inactive',
      cfi: instrument.cfi,
      exchange: instrument.exchange,
      formalName: instrument.formalName,
      incomeCountry: instrument.incomeCountry,
      instrumentID: instrument.instrumentID,
      identifiers: identifiersSummary(instrument),
      lastAuditDateTime: instrument.lastAuditDateTime,
      name: instrument.name,
      priceCountry: instrument.priceCountry,
      priceCurrency: instrument.priceCurrency,
      ticker: ticker(instrument)
    }));
  }

  function exportJson() {
    downloadFile('instruments.json', JSON.stringify(instrumentExportRows(), null, 2), 'application/json');
  }

  function exportCsv() {
    const header = ['Instrument ID', 'Name', 'Formal name', 'Ticker', 'Identifiers', 'Exchange', 'CFI', 'Price country', 'Price currency', 'Income country', 'Active', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...instrumentExportRows().map((row) =>
        [row.instrumentID, row.name, row.formalName, row.ticker, row.identifiers, row.exchange, row.cfi, row.priceCountry, row.priceCurrency, row.incomeCountry, row.active, row.lastAuditDateTime]
          .map(csvValue)
          .join(',')
      )
    ];
    downloadFile('instruments.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const rows = instrumentExportRows();
    const html = `<table><thead><tr><th>Instrument ID</th><th>Name</th><th>Formal name</th><th>Ticker</th><th>Identifiers</th><th>Exchange</th><th>CFI</th><th>Price country</th><th>Price currency</th><th>Income country</th><th>Active</th><th>Last audit</th></tr></thead><tbody>${rows.map((row) => `<tr><td>${htmlValue(row.instrumentID)}</td><td>${htmlValue(row.name)}</td><td>${htmlValue(row.formalName)}</td><td>${htmlValue(row.ticker)}</td><td>${htmlValue(row.identifiers)}</td><td>${htmlValue(row.exchange)}</td><td>${htmlValue(row.cfi)}</td><td>${htmlValue(row.priceCountry)}</td><td>${htmlValue(row.priceCurrency)}</td><td>${htmlValue(row.incomeCountry)}</td><td>${htmlValue(row.active)}</td><td>${htmlValue(row.lastAuditDateTime)}</td></tr>`).join('')}</tbody></table>`;
    downloadFile('instruments.xls', html, 'application/vnd.ms-excel');
  }

  function printTable() {
    window.print();
  }

  function startEdit(instrumentID: string) {
    addingInstrument = false;
    editingInstrumentID = instrumentID;
  }

  function cancelEdit() {
    editingInstrumentID = '';
  }

  function startAdd() {
    editingInstrumentID = '';
    addingInstrument = true;
  }

  function cancelAdd() {
    addingInstrument = false;
  }

  const enhanceInstrumentCreate: SubmitFunction = () => {
    submittingCreate = true;
    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;
      if (result.type === 'success')
        addingInstrument = false;
    };
  };

  const enhanceInstrumentEdit: SubmitFunction = ({ formData }) => {
    const instrumentID = formData.get('instrumentID');
    submittingInstrumentID = typeof instrumentID === 'string' ? instrumentID : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingInstrumentID = '';

      if (result.type === 'success')
        editingInstrumentID = '';
    };
  };

  const enhanceIdentifier: SubmitFunction = ({ formData }) => {
    const instrumentID = formData.get('instrumentID');
    const identifierType = formData.get('identifierType');
    const formAction = formData.get('formAction');

    submittingIdentifierKey = [
      typeof instrumentID === 'string' ? instrumentID : '',
      typeof identifierType === 'string' ? identifierType : '',
      typeof formAction === 'string' ? formAction : ''
    ].join('|');

    return async ({ update }) => {
      await update({ reset: false });
      submittingIdentifierKey = '';
    };
  };

  async function toggleHistory(instrumentID: string) {
    if (openHistoryInstrumentID === instrumentID) {
      openHistoryInstrumentID = '';
      delete historyByInstrumentID[instrumentID];
      return;
    }

    openHistoryInstrumentID = instrumentID;

    if (historyByInstrumentID[instrumentID])
      return;

    await loadHistory(instrumentID);
  }

  async function loadHistory(instrumentID: string) {
    historyByInstrumentID[instrumentID] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Reference/Instruments/History', window.location.origin);
      historyUrl.searchParams.set('instrumentID', instrumentID);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByInstrumentID[instrumentID] = {
        events: await response.json() as InstrumentReferenceEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByInstrumentID[instrumentID] = {
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
      data.instruments?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
  }

  function instrumentEventSummary(event: InstrumentReferenceEvent) {
    if (event.$type === 'InstrumentActiveModifiedEvent')
      return event.active ? 'Activated' : 'Deactivated';

    if (event.$type === 'InstrumentIdentifierSetEvent' && event.identifier)
      return `Set ${identifierTypeName(event.identifier.type)}: ${event.identifier.value}`;

    if (event.$type === 'InstrumentIdentifierUnsetEvent')
      return `Unset ${identifierTypeName(event.identifierType ?? '')}`;

    if (event.$type === 'InstrumentTermsSetEvent')
      return 'Terms updated';

    return [
      event.name,
      event.formalName,
      event.exchange,
      event.cfi,
      event.priceCountry,
      event.incomeCountry,
      typeof event.active === 'boolean' ? event.active ? 'Active' : 'Inactive' : ''
    ].filter(Boolean).join(' | ');
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
            <h1 class="page-title">Instruments</h1>
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
    {#if data.error}
      <div class="status-panel status-panel-error">{data.error}</div>
    {:else if data.instruments}
      {#if form?.message}
        <div class={['status-panel mb-4', form.status === 'success' ? 'status-panel-success' : 'status-panel-error']} role="status">
          {form.message}
        </div>
      {/if}

      <AggregateUpdateWatcher aggregateKind="Instruments" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.instruments.lastEventID} />

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{data.instruments.items.length}</span> instruments</div>
        <div>Valuation {formatDisplayDateTime(data.instruments.valuationDateTime)} · As-of {asOfSummary}</div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter instruments</span>
            <input bind:value={filterText} placeholder="Filter instruments..." type="search" />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add instrument" onclick={startAdd} title="Add instrument" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
            </button>
            <button aria-label="Export instruments to JSON" onclick={exportJson} title="Export JSON" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" /></svg>
            </button>
            <button aria-label="Export instruments to CSV" onclick={exportCsv} title="Export CSV" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 4h16v16H4zM4 10h16M10 4v16" /></svg>
            </button>
            <button aria-label="Export instruments to XLSX" onclick={exportXlsx} title="Export XLSX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" /></svg>
            </button>
            <button aria-label="Print instruments" onclick={printTable} title="Print" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" /></svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="w-14 px-3 py-2">Logo</th>
                <th class="px-3 py-2">Name</th>
                <th class="px-3 py-2">Ticker</th>
                <th class="px-3 py-2">Exchange</th>
                <th class="px-3 py-2">CFI</th>
                <th class="px-3 py-2">Country</th>
                <th class="px-3 py-2">Active</th>
                <th class="px-3 py-2">Last audit</th>
                <th class="sticky right-0 z-10 w-40 bg-slate-50 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#if addingInstrument}
                <tr class="bg-teal-50/30 align-top">
                  <td class="px-3 py-2"></td>
                  <td class="px-3 py-2">
                    <form id="instrument-create" action="?/createInstrument" method="POST" use:enhance={enhanceInstrumentCreate}></form>
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>Name</span>
                      <input class="house-control house-control-sm house-control-full" form="instrument-create" name="name" required type="text" value={createValue('name')} />
                    </label>
                    <label class="mt-2 grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>Formal name</span>
                      <input class="house-control house-control-sm house-control-full" form="instrument-create" name="formalName" type="text" value={createValue('formalName')} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>Ticker</span>
                      <input class="house-control house-control-sm w-28 font-mono uppercase" form="instrument-create" name="ticker" type="text" value={createValue('ticker')} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>Exchange</span>
                      <input class="house-control house-control-sm w-28 font-mono uppercase" form="instrument-create" name="exchange" required type="text" value={createValue('exchange')} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>CFI</span>
                      <input class="house-control house-control-sm w-24 font-mono uppercase" form="instrument-create" maxlength="6" minlength="6" name="cfi" required type="text" value={createValue('cfi') || 'ESVUFR'} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>Price country</span>
                      <input class="house-control house-control-sm w-24 font-mono uppercase" form="instrument-create" maxlength="2" minlength="2" name="priceCountry" required type="text" value={createValue('priceCountry')} />
                    </label>
                    <label class="mt-2 grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>Price currency</span>
                      <input class="house-control house-control-sm w-24 font-mono uppercase" form="instrument-create" maxlength="3" minlength="3" name="priceCurrency" required type="text" value={createValue('priceCurrency')} />
                    </label>
                    <label class="mt-2 grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>Income country</span>
                      <input class="house-control house-control-sm w-24 font-mono uppercase" form="instrument-create" maxlength="2" minlength="2" name="incomeCountry" type="text" value={createValue('incomeCountry')} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="flex h-full items-center gap-2 pt-5 text-xs font-medium text-slate-600" form="instrument-create">
                      <input checked form="instrument-create" name="active" type="checkbox" />
                      Active
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="instrument-create">
                      <span>Event date</span>
                      <DateTimeInput size="sm" form="instrument-create" name="eventDateTime" required step="1" value={createValue('eventDateTime') || eventDateDefault} />
                    </label>
                  </td>
                  <td class="sticky right-0 bg-teal-50 px-3 py-2 shadow-[-8px_0_12px_-12px_rgba(15,23,42,0.6)]">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={cancelAdd} type="button">Cancel</button>
                        <button class="house-button house-button-primary house-button-sm" disabled={submittingCreate} form="instrument-create" type="submit">{submittingCreate ? 'Adding' : 'Add'}</button>
                      </div>
                    </div>
                  </td>
                </tr>
              {/if}

              {#each rows as instrument (instrument.instrumentID)}
                {#if editingInstrumentID === instrument.instrumentID}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      {#if instrument.logo?.svg}
                        <span class="flag"><img src={svgDataUrl(instrument.logo.svg)} alt={`${instrument.name} logo`} /></span>
                      {/if}
                    </td>
                    <td class="px-3 py-2">
                      <form id={`instrument-edit-${instrument.instrumentID}`} action="?/modifyInstrument" method="POST" use:enhance={enhanceInstrumentEdit}>
                        <input name="instrumentID" type="hidden" value={instrument.instrumentID} />
                        <input name="logoSvg" type="hidden" value={instrument.logo?.svg ?? ''} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Name</span>
                          <input class="house-control house-control-sm house-control-full" name="name" required type="text" value={editValue(instrument.instrumentID, 'name') ?? instrument.name} />
                        </label>
                      </form>
                      <label class="mt-2 grid gap-1 text-xs font-medium text-slate-600" form={`instrument-edit-${instrument.instrumentID}`}>
                        <span>Formal name</span>
                        <input class="house-control house-control-sm house-control-full" form={`instrument-edit-${instrument.instrumentID}`} name="formalName" type="text" value={editValue(instrument.instrumentID, 'formalName') ?? instrument.formalName} />
                      </label>
                      {#if instrument.identifiers.length}
                        <div class="mt-2 flex max-w-xl flex-wrap gap-1.5">
                          {#each instrument.identifiers as identifier (`${identifier.type}-${identifier.value}`)}
                            {@const identifierType = identifierTypeValue(identifier.type)}
                            {@const unsetKey = `${instrument.instrumentID}|${identifierType}|unsetIdentifier`}
                            <form action="?/unsetIdentifier" class="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-1.5 py-0.5 text-[11px] leading-4 text-slate-700" method="POST" use:enhance={enhanceIdentifier}>
                              <input name="formAction" type="hidden" value="unsetIdentifier" />
                              <input name="instrumentID" type="hidden" value={instrument.instrumentID} />
                              <input name="identifierType" type="hidden" value={identifierType} />
                              <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                              <span class="font-semibold text-slate-500">{identifierTypeName(identifier.type)}</span>
                              <span class="font-mono">{identifier.value}</span>
                              <button aria-label={`Remove ${identifierTypeName(identifier.type)} ${identifier.value}`} class="ml-0.5 rounded px-1 font-semibold text-slate-500 hover:bg-red-50 hover:text-red-700 disabled:cursor-wait disabled:opacity-60" disabled={submittingIdentifierKey === unsetKey} title="Remove identifier" type="submit">x</button>
                            </form>
                          {/each}
                        </div>
                      {/if}
                      <form action="?/setIdentifier" class="mt-2 flex max-w-xl flex-wrap items-end gap-1.5" method="POST" use:enhance={enhanceIdentifier}>
                        <input name="formAction" type="hidden" value="setIdentifier" />
                        <input name="instrumentID" type="hidden" value={instrument.instrumentID} />
                        <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                        <label class="grid gap-0.5 text-[11px] font-medium text-slate-500">
                          <span>Identifier</span>
                          <select class="house-control house-control-compact" name="identifierType">
                            {#each identifierTypeOptions as identifierType (identifierType)}
                              <option value={identifierType}>{identifierType}</option>
                            {/each}
                          </select>
                        </label>
                        <label class="grid gap-0.5 text-[11px] font-medium text-slate-500">
                          <span>Value</span>
                          <input class="house-control house-control-compact w-32 font-mono uppercase" name="identifierValue" required type="text" />
                        </label>
                        <button class="house-button house-button-secondary house-button-compact" disabled={submittingIdentifierKey.endsWith('|setIdentifier') && submittingIdentifierKey.startsWith(`${instrument.instrumentID}|`)} type="submit">Set</button>
                      </form>
                    </td>
                    <td class="px-3 py-2 font-mono">
                      {ticker(instrument)}
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-edit-${instrument.instrumentID}`}>
                        <span>Exchange</span>
                        <input class="house-control house-control-sm w-28 font-mono uppercase" form={`instrument-edit-${instrument.instrumentID}`} name="exchange" required type="text" value={editValue(instrument.instrumentID, 'exchange') ?? instrument.exchange} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-edit-${instrument.instrumentID}`}>
                        <span>CFI</span>
                        <input class="house-control house-control-sm w-24 font-mono uppercase" form={`instrument-edit-${instrument.instrumentID}`} maxlength="6" minlength="6" name="cfi" required type="text" value={editValue(instrument.instrumentID, 'cfi') ?? instrument.cfi} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-edit-${instrument.instrumentID}`}>
                        <span>Price country</span>
                        <input class="house-control house-control-sm w-24 font-mono uppercase" form={`instrument-edit-${instrument.instrumentID}`} maxlength="2" minlength="2" name="priceCountry" required type="text" value={editValue(instrument.instrumentID, 'priceCountry') ?? instrument.priceCountry} />
                      </label>
                      <label class="mt-2 grid gap-1 text-xs font-medium text-slate-600" form={`instrument-edit-${instrument.instrumentID}`}>
                        <span>Price currency</span>
                        <input class="house-control house-control-sm w-24 font-mono uppercase" form={`instrument-edit-${instrument.instrumentID}`} maxlength="3" minlength="3" name="priceCurrency" required type="text" value={editValue(instrument.instrumentID, 'priceCurrency') ?? instrument.priceCurrency} />
                      </label>
                      <label class="mt-2 grid gap-1 text-xs font-medium text-slate-600" form={`instrument-edit-${instrument.instrumentID}`}>
                        <span>Income country</span>
                        <input class="house-control house-control-sm w-24 font-mono uppercase" form={`instrument-edit-${instrument.instrumentID}`} maxlength="2" minlength="2" name="incomeCountry" type="text" value={editValue(instrument.instrumentID, 'incomeCountry') ?? instrument.incomeCountry} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <span class={`rounded px-2 py-1 text-xs font-semibold ${instrument.active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'}`}>{instrument.active ? 'Active' : 'Inactive'}</span>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-edit-${instrument.instrumentID}`}>
                        <span>Event date</span>
                        <DateTimeInput size="sm" form={`instrument-edit-${instrument.instrumentID}`} name="eventDateTime" required step="1" value={editValue(instrument.instrumentID, 'eventDateTime') ?? eventDateDefault} />
                      </label>
                    </td>
                    <td class="sticky right-0 bg-teal-50 px-3 py-2 shadow-[-8px_0_12px_-12px_rgba(15,23,42,0.6)]">
                      <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                        <span>Actions</span>
                        <div class="flex justify-end gap-2">
                          <button class="house-button house-button-secondary house-button-sm" onclick={cancelEdit} type="button">Cancel</button>
                          <button class="house-button house-button-primary house-button-sm" disabled={submittingInstrumentID === instrument.instrumentID} form={`instrument-edit-${instrument.instrumentID}`} type="submit">{submittingInstrumentID === instrument.instrumentID ? 'Saving' : 'Save'}</button>
                        </div>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="group hover:bg-slate-50">
                    <td class="px-3 py-2">
                      {#if instrument.logo?.svg}
                        <span class="flag"><img src={svgDataUrl(instrument.logo.svg)} alt={`${instrument.name} logo`} /></span>
                      {/if}
                    </td>
                    <td class="px-3 py-2">
                      <div class="font-medium text-slate-950">{instrument.name}</div>
                      <div class="text-xs text-slate-500">{instrument.formalName}</div>
                      {#if instrument.identifiers.length}
                        <div class="mt-2 flex max-w-xl flex-wrap gap-1.5">
                          {#each instrument.identifiers as identifier (`${identifier.type}-${identifier.value}`)}
                            <span class="inline-flex items-center gap-1 rounded border border-slate-200 bg-slate-50 px-1.5 py-0.5 text-[11px] leading-4 text-slate-700">
                              <span class="font-semibold text-slate-500">{identifierTypeName(identifier.type)}</span>
                              <span class="font-mono">{identifier.value}</span>
                            </span>
                          {/each}
                        </div>
                      {/if}
                    </td>
                    <td class="px-3 py-2 font-mono">{ticker(instrument)}</td>
                    <td class="px-3 py-2">{instrument.exchange}</td>
                    <td class="px-3 py-2 font-mono">{instrument.cfi}</td>
                    <td class="px-3 py-2">{instrument.priceCountry} / {instrument.priceCurrency}</td>
                    <td class="px-3 py-2">
                      <span class={`rounded px-2 py-1 text-xs font-semibold ${instrument.active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'}`}>{instrument.active ? 'Active' : 'Inactive'}</span>
                    </td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(instrument.lastAuditDateTime)}</td>
                    <td class="sticky right-0 bg-white px-3 py-2 shadow-[-8px_0_12px_-12px_rgba(15,23,42,0.6)] group-hover:bg-slate-50">
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={() => toggleHistory(instrument.instrumentID)} type="button">
                          {openHistoryInstrumentID === instrument.instrumentID ? 'Hide' : 'History'}
                        </button>
                        <button class="house-button house-button-secondary house-button-sm" onclick={() => startEdit(instrument.instrumentID)} type="button">
                          Edit
                        </button>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryInstrumentID === instrument.instrumentID}
                    {@const history = historyByInstrumentID[instrument.instrumentID]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="9">
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
                              emptyMessage="No history found for this instrument."
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
