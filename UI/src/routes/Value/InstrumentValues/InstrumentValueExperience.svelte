<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import { formatDisplayDateTime, formatShortDate, formatTableDateTime, isSameInputDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type {
    InstrumentIncomeCash,
    InstrumentIncomeEquity,
    InstrumentIncomeFixedIncome,
    InstrumentPriceCash,
    InstrumentPriceEquity,
    InstrumentPriceFixedIncome,
    InstrumentValueHistoryEvent,
    InstrumentValue
  } from '$lib/types';
  import type { ActionData, PageData, SubmitFunction } from './$types';

  type SectionKey = 'cash' | 'equity' | 'fixedIncome' | 'unknown';

  type InstrumentValueSection = {
    key: SectionKey;
    title: string;
    priceType: string;
    incomeType: string;
    rows: InstrumentValue[];
  };

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
  let editingInstrumentID = $state('');
  let submittingInstrumentID = $state('');
  let openHistoryInstrumentID = $state('');
  let historyByInstrumentID = $state<Record<string, { events: InstrumentValueHistoryEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');
  let sectionOpen = $state<Record<SectionKey, boolean>>({
    cash: false,
    equity: false,
    fixedIncome: false,
    unknown: false
  });

  const asOfSummary = $derived(data.auditDateTime && data.instrumentValues ? formatDisplayDateTime(data.instrumentValues.asOfDateTime) : 'now');
  const rows = $derived(
    (data.instrumentValues?.items ?? []).filter((instrument) => {
      const filter = filterText.trim().toLocaleLowerCase();
      if (!filter)
        return true;

      return [instrument.name, ticker(instrument), instrument.exchange, instrument.cfi, priceType(instrument.price), incomeType(instrument.income)]
        .some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );
  const sections = $derived(buildSections(rows));

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

  function ticker(instrument: InstrumentValue) {
    return instrument.identifiers.find((identifier) => String(identifier.type).toLocaleLowerCase() === 'ticker' || identifier.type === 2)?.value ?? '-';
  }

  function currency(instrument: InstrumentValue) {
    return instrument.exchange === 'XTKS' ? 'JPY' : instrument.exchange === 'XNAS' ? 'USD' : instrument.exchange === 'XSWX' ? 'CHF' : instrument.exchange === 'XLON' || instrument.exchange === 'CASH' ? 'GBP' : 'EUR';
  }

  function equityPrice(price: InstrumentValue['price']): InstrumentPriceEquity | null {
    return price && 'bid' in price ? price : null;
  }

  function fixedIncomePrice(price: InstrumentValue['price']): InstrumentPriceFixedIncome | null {
    return price && 'cleanPrice' in price ? price : null;
  }

  function cashPrice(price: InstrumentValue['price']): InstrumentPriceCash | null {
    return price && 'price' in price ? price : null;
  }

  function equityIncome(income: InstrumentValue['income']): InstrumentIncomeEquity | null {
    return income && 'dividendAmount' in income ? income : null;
  }

  function fixedIncomeIncome(income: InstrumentValue['income']): InstrumentIncomeFixedIncome | null {
    return income && 'accruedInterest' in income ? income : null;
  }

  function cashIncome(income: InstrumentValue['income']): InstrumentIncomeCash | null {
    return income && 'income' in income ? income : null;
  }

  function priceType(price: InstrumentValue['price']) {
    if (!price)
      return 'No price';

    if (price.priceType)
      return price.priceType;

    if (price.$type)
      return price.$type;

    if (equityPrice(price))
      return 'InstrumentPriceEquity';

    if (fixedIncomePrice(price))
      return 'InstrumentPriceFixedIncome';

    if (cashPrice(price))
      return 'InstrumentPriceCash';

    return 'Unknown price';
  }

  function incomeType(income: InstrumentValue['income']) {
    if (!income)
      return 'No income';

    if (income.incomeType)
      return income.incomeType;

    if (income.$type)
      return income.$type;

    if (equityIncome(income))
      return 'InstrumentIncomeEquity';

    if (fixedIncomeIncome(income))
      return 'InstrumentIncomeFixedIncome';

    if (cashIncome(income))
      return 'InstrumentIncomeCash';

    return 'Unknown income';
  }

  function sectionKey(instrument: InstrumentValue): SectionKey {
    const pair = `${priceType(instrument.price)}|${incomeType(instrument.income)}`;

    if (pair === 'InstrumentPriceCash|InstrumentIncomeCash')
      return 'cash';

    if (pair === 'InstrumentPriceFixedIncome|InstrumentIncomeFixedIncome')
      return 'fixedIncome';

    if (pair === 'InstrumentPriceEquity|InstrumentIncomeEquity')
      return 'equity';

    return 'unknown';
  }

  function buildSections(items: InstrumentValue[]): InstrumentValueSection[] {
    const grouped: Record<SectionKey, InstrumentValue[]> = {
      cash: [],
      equity: [],
      fixedIncome: [],
      unknown: []
    };

    for (const item of items)
      grouped[sectionKey(item)].push(item);

    const sectionDefinitions: InstrumentValueSection[] = [
      {
        key: 'cash',
        title: 'Cash',
        priceType: 'InstrumentPriceCash',
        incomeType: 'InstrumentIncomeCash',
        rows: grouped.cash
      },
      {
        key: 'fixedIncome',
        title: 'Fixed Income',
        priceType: 'InstrumentPriceFixedIncome',
        incomeType: 'InstrumentIncomeFixedIncome',
        rows: grouped.fixedIncome
      },
      {
        key: 'equity',
        title: 'Equity',
        priceType: 'InstrumentPriceEquity',
        incomeType: 'InstrumentIncomeEquity',
        rows: grouped.equity
      },
      {
        key: 'unknown',
        title: 'Unpaired or unavailable values',
        priceType: 'Other',
        incomeType: 'Other',
        rows: grouped.unknown
      }
    ];

    return sectionDefinitions.filter((section) => section.rows.length > 0);
  }

  function toggleSection(key: SectionKey) {
    sectionOpen[key] = !sectionOpen[key];
  }

  function money(value: number | null | undefined) {
    return typeof value === 'number' ? value.toLocaleString(undefined, { maximumFractionDigits: 8 }) : '-';
  }

  function priceSummary(instrument: InstrumentValue) {
    const equity = equityPrice(instrument.price);
    if (equity)
      return `Bid ${money(equity.bid.amount)} | Mid ${money(equity.mid.amount)} | Ask ${money(equity.ask.amount)} | NAV ${money(equity.nav.amount)}`;

    const fixedIncome = fixedIncomePrice(instrument.price);
    if (fixedIncome)
      return `Clean ${money(fixedIncome.cleanPrice.amount)}`;

    const cash = cashPrice(instrument.price);
    if (cash)
      return `Price ${money(cash.price.amount)}`;

    return '-';
  }

  function incomeSummary(instrument: InstrumentValue) {
    const equity = equityIncome(instrument.income);
    if (equity)
      return `Dividend ${money(equity.dividendAmount.amount)}${equity.dividendType ? ` ${equity.dividendType}` : ''}`;

    const fixedIncome = fixedIncomeIncome(instrument.income);
    if (fixedIncome)
      return `Accrued ${money(fixedIncome.accruedInterest.amount)}`;

    const cash = cashIncome(instrument.income);
    if (cash)
      return `Income ${money(cash.income.value)}`;

    return '-';
  }

  function staleDataLine(valueDateTime: string | undefined) {
    return valueDateTime && !isSameInputDateTime(valueDateTime, data.valuationDate)
      ? `most recent available: ${formatShortDate(valueDateTime)}`
      : '';
  }

  function valueExportRows() {
    return rows.map((instrument) => ({
      exchange: instrument.exchange,
      income: incomeSummary(instrument),
      incomeType: incomeType(instrument.income),
      instrumentID: instrument.instrumentID,
      lastAuditDateTime: instrument.lastAuditDateTime,
      name: instrument.name,
      price: priceSummary(instrument),
      priceType: priceType(instrument.price),
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
    const header = ['Instrument ID', 'Name', 'Ticker', 'Exchange', 'Price type', 'Income type', 'Price', 'Income', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...valueExportRows().map((row) =>
        [row.instrumentID, row.name, row.ticker, row.exchange, row.priceType, row.incomeType, row.price, row.income, row.lastAuditDateTime]
          .map(csvValue)
          .join(',')
      )
    ];
    downloadFile('instrument-values.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const exportRows = valueExportRows();
    const html = `<table><thead><tr><th>Instrument ID</th><th>Name</th><th>Ticker</th><th>Exchange</th><th>Price type</th><th>Income type</th><th>Price</th><th>Income</th><th>Last audit</th></tr></thead><tbody>${exportRows.map((row) => `<tr><td>${htmlValue(row.instrumentID)}</td><td>${htmlValue(row.name)}</td><td>${htmlValue(row.ticker)}</td><td>${htmlValue(row.exchange)}</td><td>${htmlValue(row.priceType)}</td><td>${htmlValue(row.incomeType)}</td><td>${htmlValue(row.price)}</td><td>${htmlValue(row.income)}</td><td>${htmlValue(row.lastAuditDateTime)}</td></tr>`).join('')}</tbody></table>`;
    downloadFile('instrument-values.xls', html, 'application/vnd.ms-excel');
  }

  function printTable() {
    window.print();
  }

  async function toggleHistory(instrument: InstrumentValue) {
    const instrumentID = instrument.instrumentID;

    if (openHistoryInstrumentID === instrumentID) {
      openHistoryInstrumentID = '';
      delete historyByInstrumentID[instrumentID];
      return;
    }

    openHistoryInstrumentID = instrumentID;

    if (historyByInstrumentID[instrumentID])
      return;

    await loadHistory(instrumentID, instrument.priceValuationDateTime ?? null);
  }

  async function loadHistory(instrumentID: string, priceValuationDateTime: string | null = null) {
    historyByInstrumentID[instrumentID] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Value/InstrumentValues/History', window.location.origin);
      historyUrl.searchParams.set('instrumentID', instrumentID);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      if (priceValuationDateTime)
        historyUrl.searchParams.set('priceValuationDateTime', priceValuationDateTime);

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByInstrumentID[instrumentID] = {
        events: await response.json() as InstrumentValueHistoryEvent[],
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
      data.instrumentValues?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
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

<main class={shellClass}>
  {#if showFilter}
  <section class="page-header">
    <div class="page-container">
      {#if showHeader}
        <div class="page-header-main">
          <p class="page-kicker">Value Data</p>
          <div class="page-title-row">
            <h1 class="page-title">Instrument Values</h1>
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
      <Card density="compact" intent="error">{data.error}</Card>
    {:else if data.instrumentValues}
      {#if form?.message}
        <Card class="mb-4" density="compact" intent={form.status === 'success' ? 'success' : 'error'} role="status">{form.message}</Card>
      {/if}

      <AggregateUpdateWatcher aggregateKind="InstrumentValues" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.instrumentValues.lastEventID} />

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{data.instrumentValues.items.length}</span> instrument values</div>
        <div>Valuation {formatDisplayDateTime(data.instrumentValues.valuationDateTime)} | As-of {asOfSummary}</div>
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

        <div class="instrument-value-sections">
          {#each sections as section}
            <section class="instrument-value-section" aria-labelledby={`instrument-value-${section.key}`}>
              <button
                aria-controls={`instrument-value-panel-${section.key}`}
                aria-expanded={sectionOpen[section.key]}
                class="instrument-value-section-toggle"
                onclick={() => toggleSection(section.key)}
                type="button"
              >
                <span aria-hidden="true" class={`section-chevron ${sectionOpen[section.key] ? 'section-chevron-open' : ''}`}>&gt;</span>
                <span class="instrument-value-section-title" id={`instrument-value-${section.key}`}>{section.title}</span>
                <span class="instrument-value-section-meta">{section.rows.length.toLocaleString()} values</span>
              </button>

              {#if sectionOpen[section.key]}
                <div class="overflow-x-auto" id={`instrument-value-panel-${section.key}`}>
                  <table class="min-w-full divide-y divide-slate-200 text-sm">
                    <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
                      <tr>
                        <th class="px-3 py-2">Instrument</th>
                        <th class="px-3 py-2">Exchange</th>
                        <th class="px-3 py-2">Price</th>
                        <th class="px-3 py-2">Income</th>
                        <th class="px-3 py-2">Last audit</th>
                        <th class="w-28 px-3 py-2 text-right">Actions</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      {#each section.rows as instrument}
                        {#if editingInstrumentID === instrument.instrumentID && (equityPrice(instrument.price) || fixedIncomePrice(instrument.price))}
                          {@const currentPrice = equityPrice(instrument.price)}
                          {@const currentFixedIncomePrice = fixedIncomePrice(instrument.price)}
                          <tr class="bg-teal-50/30 align-top">
                            <td class="px-3 py-2">
                              <form id={`instrument-price-edit-${instrument.instrumentID}`} action="?/setInstrumentPrice" method="POST" use:enhance={enhancePrice}>
                                <input name="instrumentID" type="hidden" value={instrument.instrumentID} />
                                <input name="currency" type="hidden" value={currency(instrument)} />
                                <input name="priceType" type="hidden" value={currentFixedIncomePrice ? 'InstrumentPriceFixedIncome' : 'InstrumentPriceEquity'} />
                              </form>
                              <div class="font-medium text-slate-950">{instrument.name}</div>
                              <div class="font-mono text-xs text-slate-500">{ticker(instrument)}</div>
                            </td>
                            <td class="px-3 py-2">{instrument.exchange}</td>
                            <td class="px-3 py-2">
                              {#if currentFixedIncomePrice}
                                <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                                  <span>Clean price</span>
                                  <input class="house-control house-control-sm w-32 text-right font-mono" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="cleanPrice" required step="0.00000001" type="number" value={currentFixedIncomePrice.cleanPrice.amount ?? ''} />
                                </label>
                              {:else}
                                <div class="grid gap-2 sm:grid-cols-2">
                                  <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                                    <span>Bid</span>
                                    <input class="house-control house-control-sm w-28 text-right font-mono" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="bid" required step="0.00000001" type="number" value={currentPrice?.bid.amount ?? ''} />
                                  </label>
                                  <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                                    <span>Mid</span>
                                    <input class="house-control house-control-sm w-28 text-right font-mono" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="mid" required step="0.00000001" type="number" value={currentPrice?.mid.amount ?? ''} />
                                  </label>
                                  <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                                    <span>Ask</span>
                                    <input class="house-control house-control-sm w-28 text-right font-mono" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="ask" required step="0.00000001" type="number" value={currentPrice?.ask.amount ?? ''} />
                                  </label>
                                  <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                                    <span>NAV</span>
                                    <input class="house-control house-control-sm w-28 text-right font-mono" form={`instrument-price-edit-${instrument.instrumentID}`} min="0" name="nav" required step="0.00000001" type="number" value={currentPrice?.nav.amount ?? ''} />
                                  </label>
                                </div>
                              {/if}
                            </td>
                            <td class="px-3 py-2">
                              <label class="grid gap-1 text-xs font-medium text-slate-600" form={`instrument-price-edit-${instrument.instrumentID}`}>
                                <span>Event date</span>
                                <DateTimeInput size="sm" form={`instrument-price-edit-${instrument.instrumentID}`} name="eventDateTime" required step="1" value={eventDateDefault} />
                              </label>
                            </td>
                            <td class="px-3 py-2 text-slate-600">{formatTableDateTime(instrument.lastAuditDateTime)}</td>
                            <td class="px-3 py-2 text-right">
                              <div class="flex justify-end gap-2">
                                <button class="house-button house-button-secondary house-button-sm" onclick={() => editingInstrumentID = ''} type="button">Cancel</button>
                                <button class="house-button house-button-primary house-button-sm" disabled={submittingInstrumentID === instrument.instrumentID} form={`instrument-price-edit-${instrument.instrumentID}`} type="submit">{submittingInstrumentID === instrument.instrumentID ? 'Saving' : 'Save'}</button>
                              </div>
                            </td>
                          </tr>
                        {:else}
                          <tr class="hover:bg-slate-50">
                            <td class="px-3 py-2">
                              <div class="font-medium text-slate-950">{instrument.name}</div>
                              <div class="font-mono text-xs text-slate-500">{ticker(instrument)}</div>
                              {#if staleDataLine(instrument.priceValuationDateTime ?? undefined)}
                                <div class="mt-1 text-xs text-slate-400">{staleDataLine(instrument.priceValuationDateTime ?? undefined)}</div>
                              {/if}
                            </td>
                            <td class="px-3 py-2">{instrument.exchange}</td>
                            <td class="px-3 py-2 font-mono">{priceSummary(instrument)}</td>
                            <td class="px-3 py-2 font-mono">{incomeSummary(instrument)}</td>
                            <td class="px-3 py-2 text-slate-600">{formatTableDateTime(instrument.lastAuditDateTime)}</td>
                            <td class="px-3 py-2 text-right">
                              <div class="flex justify-end gap-2">
                                <button class="house-button house-button-secondary house-button-sm" onclick={() => toggleHistory(instrument)} type="button">
                                  {openHistoryInstrumentID === instrument.instrumentID ? 'Hide' : 'History'}
                                </button>
                                {#if equityPrice(instrument.price) || fixedIncomePrice(instrument.price)}
                                  <button class="house-button house-button-secondary house-button-sm" onclick={() => editingInstrumentID = instrument.instrumentID} type="button">Edit</button>
                                {/if}
                              </div>
                            </td>
                          </tr>
                          {#if openHistoryInstrumentID === instrument.instrumentID}
                            {@const history = historyByInstrumentID[instrument.instrumentID]}
                            <tr class="bg-slate-50/80">
                              <td class="px-3 py-3" colspan="6">
                                <div>
                                  {#if history?.loading}
                                    <div class="text-sm text-slate-600">Loading history...</div>
                                  {:else if history?.error}
                                    <Card density="compact" intent="error">{history.error}</Card>
                                  {:else}
                                    <HistoryEventsCard
                                      eventDateTime={data.instrumentValues.valuationDateTime ?? data.valuationDate}
                                      asAtDateTime={data.auditDateTime}
                                      events={history?.events ?? []}
                                      emptyMessage="No history found for this instrument value."
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
              {/if}
            </section>
          {/each}
        </div>
      </div>
    {/if}
  </section>
  {/if}
</main>
