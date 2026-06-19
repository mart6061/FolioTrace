<script lang="ts">
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { endOfDayForInput, startOfDayForInput } from '$lib/dates';
  import { holdingDateBasisOptions } from '$lib/valuationPreferences';
  import type { PageData } from './$types';

  let { data }: { data: PageData } = $props();

  type ReportDocument = NonNullable<PageData['reportDocument']>;
  type ReportDocumentSection = ReportDocument['sections'][number];
  type ReportValuationColumn = ReportDocumentSection['valuationColumns'][number];
  type ReportValuationRow = ReportDocumentSection['valuationRows'][number];

  let shareStatus = $state('');
  let reportDocumentElement = $state<HTMLElement | null>(null);
  // svelte-ignore state_referenced_locally
  let valuationStartDate = $state(data.valuationStartDate);
  // svelte-ignore state_referenced_locally
  let valuationEndDate = $state(data.valuationDate);
  // svelte-ignore state_referenced_locally
  let selectedAccountID = $state(data.accountID);
  // svelte-ignore state_referenced_locally
  let selectedReportID = $state(data.reportID);
  // svelte-ignore state_referenced_locally
  let selectedInstrumentPriceBasis = $state(data.instrumentPriceBasis);
  // svelte-ignore state_referenced_locally
  let selectedHoldingDateBasis = $state(data.holdingDateBasis);
  const dateRangeValid = $derived(isValidValuationRange(valuationStartDate, valuationEndDate));
  const selectedAccountExists = $derived(data.accounts.some((account) => account.accountID === selectedAccountID));
  const selectedReportExists = $derived(data.reportConfigs.some((reportConfig) => reportConfig.reportID === selectedReportID));
  const filtersValid = $derived(Boolean(selectedAccountExists && selectedReportExists && selectedInstrumentPriceBasis && selectedHoldingDateBasis && dateRangeValid));

  function captureReportDocument(node: HTMLElement) {
    reportDocumentElement = node;

    return () => {
      if (reportDocumentElement === node)
        reportDocumentElement = null;
    };
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

  function reportFileName(extension: string) {
    const name = data.reportDocument?.name ?? 'report';
    const slug = name
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'report';

    return `${slug}.${extension}`;
  }

  function htmlValue(value: string) {
    return value
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }

  function formatNumber(value: number, maximumFractionDigits = 2) {
    return new Intl.NumberFormat('en-GB', {
      maximumFractionDigits,
      minimumFractionDigits: maximumFractionDigits
    }).format(value);
  }

  function formatQuantity(value: number) {
    return new Intl.NumberFormat('en-GB', {
      maximumFractionDigits: 4
    }).format(value);
  }

  function formatPercent(value: number) {
    return `${formatNumber(value, 2)}%`;
  }

  function formatOptionalPercent(value: number | null) {
    return value === null ? '' : formatPercent(value);
  }

  function formatOptionalMoney(value: number | null, currency: string) {
    return value === null ? '' : formatMoney(value, currency);
  }

  function formatMoney(value: number, currency: string) {
    return new Intl.NumberFormat('en-GB', {
      currency,
      currencyDisplay: 'narrowSymbol',
      maximumFractionDigits: 2,
      minimumFractionDigits: 2,
      style: 'currency'
    }).format(value);
  }

  function formatDateTime(value: string) {
    const date = new Date(value);

    if (Number.isNaN(date.getTime()))
      return '';

    return new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      hour: '2-digit',
      hour12: false,
      minute: '2-digit',
      month: '2-digit',
      second: '2-digit',
      year: '2-digit'
    }).format(date);
  }

  function valuationIndent(level: number) {
    return `${Math.max(0, Math.min(level - 1, 5)) * 0.8}rem`;
  }

  function pieLegendIndent(level: number) {
    return `${Math.max(0, Math.min(level - 1, 5)) * 0.75}rem`;
  }

  function handleValuationStartChange() {
    valuationStartDate = startOfDayForInput(valuationStartDate);

    if (new Date(valuationStartDate).getTime() > new Date(valuationEndDate).getTime())
      valuationEndDate = endOfDayForInput(valuationStartDate);
  }

  function handleValuationEndChange() {
    valuationEndDate = endOfDayForInput(valuationEndDate);

    if (new Date(valuationEndDate).getTime() < new Date(valuationStartDate).getTime())
      valuationStartDate = startOfDayForInput(valuationEndDate);
  }

  function isValidValuationRange(startDateTime: string, endDateTime: string) {
    const startTime = new Date(startDateTime).getTime();
    const endTime = new Date(endDateTime).getTime();

    return Number.isFinite(startTime) && Number.isFinite(endTime) && startTime <= endTime;
  }

  function submitFilterChange(event: Event) {
    const control = event.currentTarget;

    if (control instanceof HTMLSelectElement) {
      if (control.name === 'accountID')
        selectedReportID = '';

      control.form?.requestSubmit();
    }
  }

  function valuationColumnValue(row: ReportValuationRow, column: ReportValuationColumn, currency: string) {
    switch (column.columnKey) {
      case 'InstrumentName':
        return row.instrumentName;
      case 'ISIN':
        return row.isin;
      case 'Sedol':
        return row.sedol;
      case 'QuotePrice':
        return formatOptionalMoney(row.quotePrice, currency);
      case 'Quantity':
        return formatQuantity(row.quantity);
      case 'BookValue':
        return formatMoney(row.bookValue, currency);
      case 'BookCost':
        return formatMoney(row.bookCost, currency);
      case 'Weight':
        return formatPercent(row.weightPercent);
      case 'Target':
        return formatOptionalPercent(row.targetPercent);
      case 'Min':
        return formatOptionalPercent(row.targetMinPercent);
      case 'Max':
        return formatOptionalPercent(row.targetMaxPercent);
      default:
        return '';
    }
  }

  function reportDefaultPageRule() {
    const sections = data.reportDocument?.sections ?? [];
    const orientations = sections.map((section) => section.pageOrientation);

    if (orientations.length && orientations.every((orientation) => orientation === 'Landscape'))
      return '@page { size: A4 landscape; margin: 0; mso-page-orientation: landscape; }';

    if (!orientations.length || orientations.every((orientation) => orientation === 'Portrait'))
      return '@page { size: A4 portrait; margin: 0; mso-page-orientation: portrait; }';

    return '';
  }

  function reportWordDocumentSettings() {
    return `<!--[if gte mso 9]>
      <xml>
        <w:WordDocument>
          <w:View>Print</w:View>
          <w:Zoom>100</w:Zoom>
          <w:DoNotOptimizeForBrowser />
        </w:WordDocument>
      </xml>
    <![endif]-->`;
  }

  function reportHtml() {
    const documentElement = reportDocumentElement;

    if (!(documentElement instanceof HTMLElement))
      return '';

    return `<!doctype html>
      <html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:w="urn:schemas-microsoft-com:office:word" xmlns="http://www.w3.org/TR/REC-html40">
        <head>
          <meta charset="utf-8" />
          <title>${htmlValue(data.reportDocument?.name ?? 'Report')}</title>
          ${reportWordDocumentSettings()}
            <style>
            ${reportDefaultPageRule()}
            @page ReportPortrait { size: A4 portrait; margin: 0; mso-page-orientation: portrait; }
            @page ReportLandscape { size: A4 landscape; margin: 0; mso-page-orientation: landscape; }
            html, body { margin: 0; padding: 0; }
            body { font-family: Arial, sans-serif; color: #0f172a; }
            .report-document-shell { display: block; margin: 0; padding: 0; }
            .report-document-page { box-sizing: border-box; width: 210mm; min-height: 297mm; padding: 22mm 18mm; page: ReportPortrait; break-before: page; break-after: page; page-break-before: always; page-break-after: always; mso-page-orientation: portrait; }
            .report-document-page:first-child { break-before: auto; page-break-before: auto; }
            .report-document-page.portrait-page { page: ReportPortrait; mso-page-orientation: portrait; }
            .report-document-page.landscape-page { width: 297mm; min-height: 210mm; page: ReportLandscape; mso-page-orientation: landscape; }
            .report-document-header { border-bottom: 1px solid #cbd5e1; padding-bottom: 14px; }
            .report-document-header h2 { margin: 0; font-size: 30px; line-height: 1.2; }
            .report-document-header p { margin: 6px 0 0; color: #475569; font-size: 16px; }
            .report-section-content { padding-top: 20px; }
            .report-empty-state { border: 1px solid #e2e8f0; color: #64748b; font-size: 14px; padding: 14px; }
            .report-pie-layout { align-items: center; display: grid; gap: 24px; grid-template-columns: minmax(220px, 0.82fr) minmax(220px, 1fr); }
            .report-pie-chart { display: block; height: auto; width: 100%; }
            .report-pie-chart path { stroke: #ffffff; stroke-width: 0.45; }
            .report-pie-legend { display: grid; gap: 9px; margin: 0; }
            .report-pie-legend-row { align-items: center; display: grid; gap: 10px; grid-template-columns: 14px 1fr auto; }
            .report-pie-swatch { border-radius: 999px; display: block; height: 12px; width: 12px; }
            .report-pie-name { font-size: 14px; font-weight: 700; }
            .report-pie-value { color: #475569; font-family: Consolas, monospace; font-size: 13px; text-align: right; }
            .report-valuation-table { border-collapse: collapse; font-size: 11px; width: 100%; }
            .report-valuation-table th { border-bottom: 1px solid #cbd5e1; color: #475569; font-size: 10px; letter-spacing: 0.04em; padding: 7px 6px; text-align: left; text-transform: uppercase; }
            .report-valuation-table th.numeric, .report-valuation-table td.numeric { text-align: right; }
            .report-valuation-node-row td { border-top: 1px solid #cbd5e1; color: #0f172a; padding: 10px 6px 5px; }
            .report-valuation-node-row.top-level td { font-size: 13px; font-weight: 700; }
            .report-valuation-node-row.child-level td { color: #334155; font-size: 11px; font-weight: 700; }
            .report-valuation-variance.negative { color: #b91c1c; }
            .report-valuation-variance.positive { color: #047857; }
            .report-valuation-group-marker { border-radius: 999px; display: inline-block; height: 10px; margin-right: 7px; width: 10px; }
            .report-valuation-table tbody tr.asset-row td { border-bottom: 1px solid #e2e8f0; padding: 6px; }
            .report-valuation-table td.numeric { font-family: Consolas, monospace; }
            .report-valuation-subtotal td { border-bottom: 1px solid #94a3b8; color: #334155; font-weight: 700; padding: 7px 6px 10px; }
            .report-valuation-subtotal.top-level td { color: #0f172a; }
            .report-valuation-total td { border-bottom: 2px solid #0f172a; border-top: 2px solid #0f172a; color: #0f172a; font-weight: 800; padding: 8px 6px; }
            .report-transaction-table { border-collapse: collapse; font-size: 11px; width: 100%; }
            .report-cash-table { border-collapse: collapse; font-size: 11px; width: 100%; }
            .report-cash-groups { display: grid; gap: 18px; }
            .report-cash-group-title { align-items: baseline; border-bottom: 1px solid #cbd5e1; color: #0f172a; display: flex; font-size: 13px; font-weight: 700; gap: 12px; justify-content: space-between; margin: 0 0 6px; padding-bottom: 6px; }
            .report-cash-group-total { color: #475569; font-family: Consolas, monospace; font-size: 12px; font-weight: 700; }
            .report-transaction-table th, .report-cash-table th { border-bottom: 1px solid #cbd5e1; color: #475569; font-size: 10px; letter-spacing: 0.04em; padding: 7px 6px; text-align: left; text-transform: uppercase; }
            .report-transaction-table th.numeric, .report-transaction-table td.numeric, .report-cash-table th.numeric, .report-cash-table td.numeric { text-align: right; }
            .report-transaction-table td, .report-cash-table td { border-bottom: 1px solid #e2e8f0; color: #0f172a; padding: 6px; }
            .report-transaction-table td.numeric, .report-cash-table td.numeric { font-family: Consolas, monospace; }
          </style>
        </head>
        <body>${documentElement.outerHTML}</body>
      </html>`;
  }

  function exportPdf() {
    const html = reportHtml();
    if (!html)
      return;

    const frame = document.createElement('iframe');
    frame.title = data.reportDocument?.name ?? 'Report';
    frame.setAttribute('aria-hidden', 'true');
    frame.style.position = 'fixed';
    frame.style.left = '-10000px';
    frame.style.top = '0';
    frame.style.width = '297mm';
    frame.style.height = '210mm';
    frame.style.border = '0';
    frame.style.opacity = '0';
    frame.style.pointerEvents = 'none';

    document.body.appendChild(frame);

    const frameDocument = frame.contentDocument;
    const frameWindow = frame.contentWindow;
    if (!frameDocument || !frameWindow) {
      frame.remove();
      return;
    }

    frame.onload = () => {
      frameWindow.focus();
      frameWindow.print();
      setTimeout(() => frame.remove(), 1000);
    };

    frameDocument.open();
    frameDocument.write(html);
    frameDocument.close();
  }

  function exportWord() {
    downloadFile(reportFileName('doc'), reportHtml(), 'application/msword');
  }

  async function shareReport() {
    const shareData = {
      title: data.reportDocument?.name ?? 'Report',
      text: data.reportDocument?.valuationHeading ?? 'Report',
      url: window.location.href
    };

    shareStatus = '';

    try {
      if (navigator.share) {
        await navigator.share(shareData);
        return;
      }

      await navigator.clipboard.writeText(window.location.href);
      shareStatus = 'Link copied';
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError')
        return;

      shareStatus = 'Unable to share';
    }
  }
</script>

<svelte:head>
  <title>Report | FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-content">
        <div class="page-header-main">
          <p class="page-kicker">Report</p>
          <div class="page-title-row">
            <h1 class="page-title">Report</h1>
            <BookmarkButton />
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section grid gap-5">
    <form class="house-form report-filter-form" method="GET">
      {#if data.auditDateTime}
        <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
      {/if}

      <div class="report-filter-section report-filter-section-first report-valuation-date-grid">
        <label class="report-filter-title grid min-w-0 gap-1">
          Valuation period
          <DateTimeInput bind:value={valuationStartDate} fullWidth name="valuationStartDate" onchange={handleValuationStartChange} step="1" />
        </label>
        <span class="report-valuation-date-separator">to</span>
        <label class="report-filter-title grid min-w-0 gap-1">
          <span class="sr-only">End date</span>
          <DateTimeInput bind:value={valuationEndDate} fullWidth name="valuationDate" onchange={handleValuationEndChange} step="1" />
        </label>
      </div>

      <div class="report-filter-section report-filter-basis-grid">
        <label class="report-filter-title grid min-w-0 gap-1">
          Account
          <select bind:value={selectedAccountID} class="house-control house-control-md house-control-full" disabled={!data.accounts.length} name="accountID" onchange={submitFilterChange}>
            <option disabled value="">{data.accounts.length ? 'Select account' : 'No active accounts'}</option>
            {#each data.accounts as account (account.accountID)}
              <option value={account.accountID}>{account.name}</option>
            {/each}
          </select>
        </label>

        <fieldset class="report-filter-field">
          <legend class="report-filter-title">Price basis</legend>
          <div class="report-filter-toggle-group price-basis-toggle">
            {#each data.instrumentPriceBasisOptions as option (option)}
              <label class="report-filter-toggle">
                <input bind:group={selectedInstrumentPriceBasis} name="instrumentPriceBasis" type="radio" value={option} />
                <span>{option}</span>
              </label>
            {/each}
          </div>
        </fieldset>

        <fieldset class="report-filter-field">
          <legend class="report-filter-title">Holding basis</legend>
          <div class="report-filter-toggle-group">
            {#each holdingDateBasisOptions as option (option.value)}
              <label class="report-filter-toggle">
                <input bind:group={selectedHoldingDateBasis} name="holdingDateBasis" type="radio" value={option.value} />
                <span>{option.label}</span>
              </label>
            {/each}
          </div>
        </fieldset>
      </div>

      <div class="report-filter-section report-filter-report-grid">
        <div class="grid gap-2">
          <label class="report-filter-title grid min-w-0 gap-1">
            Report
            <select bind:value={selectedReportID} class="house-control house-control-md house-control-full" disabled={!data.reportConfigs.length} name="reportID">
              <option disabled value="">{data.accountID ? 'Select report' : 'Select account first'}</option>
              {#each data.reportConfigs as reportConfig (reportConfig.reportID)}
                <option value={reportConfig.reportID}>{reportConfig.name}</option>
              {/each}
            </select>
          </label>

          {#if data.accounts.length && !data.accountID}
            <div class="status-panel status-panel-warning">Select an account to choose a report config.</div>
          {:else if data.accountID && !data.reportConfigs.length}
            <div class="status-panel status-panel-warning">No active report configs match the selected account and valuation date.</div>
          {/if}
        </div>

        <button class="house-button house-button-primary house-button-md" disabled={!filtersValid} name="create" type="submit" value="true">Create</button>
      </div>
    </form>

    {#if data.error}
      <div class="status-panel status-panel-error" role="status">{data.error}</div>
    {/if}

    {#if data.reportDocument}
      <div class="report-action-bar" aria-label="Report actions">
        <button class="house-button house-button-secondary house-button-md" onclick={exportPdf} title="Export PDF" type="button">Export to PDF</button>
        <button class="house-button house-button-secondary house-button-md" onclick={exportWord} title="Export Word" type="button">Export to Word</button>
        <button class="house-button house-button-secondary house-button-md" onclick={shareReport} title="Share" type="button">Share</button>
        {#if shareStatus}
          <span role="status">{shareStatus}</span>
        {/if}
      </div>

      <section {@attach captureReportDocument} aria-label={data.reportDocument.name} class="report-document-shell">
        {#each data.reportDocument.sections as section (section.reportNodeID)}
          <article class:landscape-page={section.pageOrientation === 'Landscape'} class:portrait-page={section.pageOrientation !== 'Landscape'} class="report-document-page">
            <header class="report-document-header">
              <h2>{section.name}</h2>
              <p>{data.reportDocument.valuationHeading}</p>
            </header>

            <div class="report-section-content">
              {#if section.sectionType === 'Pie'}
                {#if section.pieSlices.length}
                  <div class="report-pie-layout">
                    <svg aria-label={section.title} class="report-pie-chart" role="img" viewBox="0 0 100 100">
                      {#each section.pieSlices as slice (slice.sliceID)}
                        <path d={slice.path} fill={slice.colour}>
                          <title>{slice.name}: {formatPercent(slice.weightPercent)} (level {slice.level})</title>
                        </path>
                      {/each}
                    </svg>

                    <dl class="report-pie-legend">
                      {#each section.pieSlices as slice (slice.sliceID)}
                        <div class="report-pie-legend-row" style:padding-left={pieLegendIndent(slice.level)}>
                          <span class="report-pie-swatch" style:background-color={slice.colour}></span>
                          <dt class="report-pie-name">{slice.name}</dt>
                          <dd class="report-pie-value">{formatPercent(slice.weightPercent)}</dd>
                        </div>
                      {/each}
                    </dl>
                  </div>
                {:else}
                  <div class="report-empty-state">No valuation weights are available for this chart.</div>
                {/if}
              {:else if section.sectionType === 'Valuation'}
                {#if section.valuationRows.length}
                  <table class="report-valuation-table">
                    <thead>
                      <tr>
                        <th>Name</th>
                        {#each section.valuationColumns as column (column.columnKey)}
                          <th class:numeric={column.numeric}>{column.label}</th>
                        {/each}
                      </tr>
                    </thead>
                    <tbody>
                      {#each section.valuationRows as row (row.rowID)}
                        {#if row.rowType === 'Group'}
                          <tr class={['report-valuation-node-row', row.level === 1 ? 'top-level' : 'child-level']}>
                            <td colspan={section.valuationDisplayHoldings ? section.valuationColumns.length + 1 : undefined} style:color={section.valuationColourText ? row.colour : undefined} style:padding-left={valuationIndent(row.level)}>
                              {#if section.valuationColourBullet}
                                <span class="report-valuation-group-marker" style:background-color={row.colour}></span>
                              {/if}
                              {row.name}
                            </td>
                            {#if !section.valuationDisplayHoldings}
                              {#each section.valuationColumns as column (column.columnKey)}
                                <td class:numeric={column.numeric}>{valuationColumnValue(row, column, section.currency)}</td>
                              {/each}
                            {/if}
                          </tr>
                        {:else if row.rowType === 'Asset' && section.valuationDisplayHoldings}
                          <tr class="asset-row">
                            <td style:padding-left={valuationIndent(row.level)}>{row.name}</td>
                            {#each section.valuationColumns as column (column.columnKey)}
                              <td class:numeric={column.numeric}>{valuationColumnValue(row, column, section.currency)}</td>
                            {/each}
                          </tr>
                        {:else if row.rowType === 'Total'}
                          <tr class="report-valuation-total">
                            <td>{row.name}</td>
                            {#each section.valuationColumns as column (column.columnKey)}
                              <td class:numeric={column.numeric}>{valuationColumnValue(row, column, section.currency)}</td>
                            {/each}
                          </tr>
                        {:else if section.valuationDisplayHoldings}
                          <tr class={['report-valuation-subtotal', row.level === 1 ? 'top-level' : 'child-level']}>
                            <td style:color={section.valuationColourText ? row.colour : undefined} style:padding-left={valuationIndent(row.level)}>{row.name}</td>
                            {#each section.valuationColumns as column (column.columnKey)}
                              <td class:numeric={column.numeric}>{valuationColumnValue(row, column, section.currency)}</td>
                            {/each}
                          </tr>
                        {/if}
                      {/each}
                    </tbody>
                  </table>
                {:else}
                  <div class="report-empty-state">No mapped assets are available for this valuation.</div>
                {/if}
              {:else if section.sectionType === 'Cash'}
                {#if section.cashGroups.length}
                  <div class="report-cash-groups">
                    {#each section.cashGroups as group (group.holdingID)}
                      <section class="report-cash-group" aria-label={group.name}>
                        <h3 class="report-cash-group-title">
                          <span>{group.name}</span>
                          <span class="report-cash-group-total">{formatQuantity(group.totalQuantity)}</span>
                        </h3>
                        <table class="report-cash-table">
                          <thead>
                            <tr>
                              <th>Date</th>
                              <th>Name</th>
                              <th class="numeric">Quantity</th>
                            </tr>
                          </thead>
                          <tbody>
                            {#each group.rows as cash (cash.rowID)}
                              <tr>
                                <td>{formatDateTime(cash.displayDateTime)}</td>
                                <td>{cash.name}</td>
                                <td class="numeric">{formatQuantity(cash.quantity)}</td>
                              </tr>
                            {/each}
                          </tbody>
                        </table>
                      </section>
                    {/each}
                  </div>
                {:else}
                  <div class="report-empty-state">No cash movements are available for this period.</div>
                {/if}
              {:else if section.sectionType === 'Transactions'}
                {#if section.transactionRows.length}
                  <table class="report-transaction-table">
                    <thead>
                      <tr>
                        <th>Date</th>
                        <th>Type</th>
                        <th>Holding</th>
                        <th>Instrument</th>
                        <th class="numeric">Quantity</th>
                        <th class="numeric">Book cost</th>
                      </tr>
                    </thead>
                    <tbody>
                      {#each section.transactionRows as transaction (transaction.rowID)}
                        <tr>
                          <td>{formatDateTime(transaction.displayDateTime)}</td>
                          <td>{transaction.transactionType}</td>
                          <td>{transaction.holdingName}</td>
                          <td>{transaction.instrumentName}</td>
                          <td class="numeric">{formatQuantity(transaction.quantity)}</td>
                          <td class="numeric">{formatMoney(transaction.bookCost, section.currency)}</td>
                        </tr>
                      {/each}
                    </tbody>
                  </table>
                {:else}
                  <div class="report-empty-state">No holding transactions are available for this period.</div>
                {/if}
              {/if}
            </div>
          </article>
        {:else}
          <article class="report-document-page">
            <header class="report-document-header">
              <h2>{data.reportDocument.name}</h2>
              <p>{data.reportDocument.valuationHeading}</p>
            </header>
          </article>
        {/each}
      </section>
    {/if}
  </section>
</main>

<style>
  .report-document-shell {
    display: grid;
    gap: 1.25rem;
    justify-items: center;
  }

  .report-action-bar {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    align-items: center;
    justify-content: flex-end;
  }

  .report-action-bar button {
    min-height: 2.25rem;
    border: 1px solid rgb(13 148 136 / 0.32);
    border-radius: 0.375rem;
    background: white;
    color: rgb(15 118 110);
    cursor: pointer;
    font-size: 0.875rem;
    font-weight: 700;
    padding: 0.45rem 0.75rem;
  }

  .report-action-bar button:hover,
  .report-action-bar button:focus-visible {
    border-color: rgb(15 118 110);
    background: rgb(240 253 250);
    outline: none;
  }

  .report-action-bar span {
    color: rgb(71 85 105);
    font-size: 0.8125rem;
    font-weight: 700;
  }

  .report-filter-form {
    display: grid;
    gap: 0;
  }

  .report-filter-section {
    border-top: 1px solid color-mix(in srgb, var(--line) 82%, transparent);
    margin-top: 1rem;
    padding-top: 1rem;
  }

  .report-filter-section-first {
    border-top: 0;
    margin-top: 0;
    padding-top: 0;
  }

  .report-valuation-date-grid {
    align-items: end;
    display: grid;
    column-gap: 0.75rem;
    row-gap: 0.5rem;
    grid-template-columns: minmax(0, var(--house-datetime-width)) auto minmax(0, var(--house-datetime-width));
    justify-content: start;
  }

  .report-valuation-date-separator {
    align-self: end;
    color: var(--muted);
    font-size: 0.8125rem;
    font-weight: 700;
    padding-bottom: 0.5rem;
  }

  .report-filter-basis-grid {
    display: grid;
    gap: 0.75rem;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .report-filter-report-grid {
    align-items: end;
    display: grid;
    gap: 0.75rem;
    grid-template-columns: minmax(0, 1fr) auto;
  }

  .report-filter-title {
    color: var(--muted);
    font-size: var(--house-label-size);
    font-weight: 700;
    line-height: 1.25rem;
  }

  .report-filter-field {
    border: 0;
    display: grid;
    gap: 0.25rem;
    margin: 0;
    min-width: 0;
    padding: 0;
  }

  .report-filter-field legend {
    padding: 0;
  }

  .report-filter-toggle-group {
    align-items: stretch;
    background: color-mix(in srgb, var(--panel-muted) 90%, transparent);
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    box-sizing: border-box;
    display: grid;
    gap: 0.125rem;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    height: var(--house-control-height);
    min-height: 0;
    padding: 0.125rem;
  }

  .price-basis-toggle {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }

  .report-filter-toggle {
    display: grid;
    min-width: 0;
    position: relative;
  }

  .report-filter-toggle input {
    height: 1px;
    opacity: 0;
    overflow: hidden;
    position: absolute;
    width: 1px;
  }

  .report-filter-toggle span {
    align-items: center;
    border-radius: calc(var(--house-radius-sm) - 2px);
    color: var(--muted);
    display: flex;
    font-size: 0.8125rem;
    font-weight: 700;
    height: 100%;
    justify-content: center;
    min-height: 0;
    min-width: 0;
    padding: 0 0.5rem;
    text-align: center;
    white-space: nowrap;
  }

  .report-filter-toggle input:checked + span {
    background: var(--panel);
    box-shadow: 0 1px 3px var(--surface-shadow);
    color: var(--accent);
  }

  .report-filter-toggle input:focus-visible + span {
    box-shadow: 0 0 0 3px var(--focus-ring);
    outline: none;
  }

  .report-document-page {
    width: min(100%, 210mm);
    min-height: 297mm;
    background: white;
    border: 1px solid rgb(226 232 240);
    border-radius: 0.375rem;
    box-shadow: 0 16px 40px rgb(15 23 42 / 0.08);
    padding: 22mm 18mm;
  }

  .report-document-page.landscape-page {
    width: min(100%, 297mm);
    min-height: 210mm;
  }

  .report-document-header {
    border-bottom: 1px solid rgb(203 213 225);
    padding-bottom: 0.85rem;
  }

  .report-document-header h2 {
    color: rgb(15 23 42);
    font-size: 1.875rem;
    font-weight: 700;
    letter-spacing: 0;
    line-height: 1.2;
    margin: 0;
  }

  .report-document-header p {
    color: rgb(71 85 105);
    font-size: 1rem;
    font-weight: 500;
    margin: 0.35rem 0 0;
  }

  .report-section-content {
    padding-top: 1.25rem;
  }

  .report-empty-state {
    border: 1px solid rgb(226 232 240);
    border-radius: 0.375rem;
    color: rgb(100 116 139);
    font-size: 0.875rem;
    padding: 0.875rem;
  }

  .report-pie-layout {
    align-items: center;
    display: grid;
    gap: 1.5rem;
    grid-template-columns: minmax(13.75rem, 0.82fr) minmax(13.75rem, 1fr);
  }

  .report-pie-chart {
    display: block;
    height: auto;
    width: 100%;
  }

  .report-pie-chart path {
    stroke: white;
    stroke-width: 0.45;
  }

  .report-pie-legend {
    display: grid;
    gap: 0.5625rem;
    margin: 0;
  }

  .report-pie-legend-row {
    align-items: center;
    display: grid;
    gap: 0.625rem;
    grid-template-columns: 0.875rem 1fr auto;
  }

  .report-pie-swatch {
    border-radius: 999px;
    display: block;
    height: 0.75rem;
    width: 0.75rem;
  }

  .report-pie-name {
    color: rgb(15 23 42);
    font-size: 0.875rem;
    font-weight: 700;
  }

  .report-pie-value {
    color: rgb(71 85 105);
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-size: 0.8125rem;
    margin: 0;
    text-align: right;
  }

  .report-valuation-table {
    border-collapse: collapse;
    font-size: 0.6875rem;
    width: 100%;
  }

  .report-valuation-table th {
    border-bottom: 1px solid rgb(203 213 225);
    color: rgb(71 85 105);
    font-size: 0.625rem;
    font-weight: 700;
    letter-spacing: 0.04em;
    padding: 0.4375rem 0.375rem;
    text-align: left;
    text-transform: uppercase;
  }

  .report-valuation-table th.numeric,
  .report-valuation-table td.numeric {
    text-align: right;
  }

  .report-valuation-node-row td {
    border-top: 1px solid rgb(203 213 225);
    color: rgb(15 23 42);
    padding: 0.625rem 0.375rem 0.3125rem;
  }

  .report-valuation-node-row.top-level td {
    font-size: 0.8125rem;
    font-weight: 700;
  }

  .report-valuation-node-row.child-level td {
    color: rgb(51 65 85);
    font-size: 0.6875rem;
    font-weight: 700;
  }

  .report-valuation-group-marker {
    border-radius: 999px;
    display: inline-block;
    height: 0.625rem;
    margin-right: 0.4375rem;
    width: 0.625rem;
  }

  .report-valuation-table tbody tr.asset-row td {
    border-bottom: 1px solid rgb(226 232 240);
    padding: 0.375rem;
  }

  .report-valuation-table td.numeric {
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  }

  .report-valuation-subtotal td {
    border-bottom: 1px solid rgb(148 163 184);
    color: rgb(51 65 85);
    font-weight: 700;
    padding: 0.4375rem 0.375rem 0.625rem;
  }

  .report-valuation-subtotal.top-level td {
    color: rgb(15 23 42);
  }

  .report-valuation-total td {
    border-bottom: 2px solid rgb(15 23 42);
    border-top: 2px solid rgb(15 23 42);
    color: rgb(15 23 42);
    font-weight: 800;
    padding: 0.5rem 0.375rem;
  }

  .report-transaction-table,
  .report-cash-table {
    border-collapse: collapse;
    font-size: 0.6875rem;
    width: 100%;
  }

  .report-cash-groups {
    display: grid;
    gap: 1.125rem;
  }

  .report-cash-group-title {
    align-items: baseline;
    border-bottom: 1px solid rgb(203 213 225);
    color: rgb(15 23 42);
    display: flex;
    font-size: 0.8125rem;
    font-weight: 700;
    gap: 0.75rem;
    justify-content: space-between;
    margin: 0 0 0.375rem;
    padding-bottom: 0.375rem;
  }

  .report-cash-group-total {
    color: rgb(71 85 105);
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-size: 0.75rem;
    font-weight: 700;
  }

  .report-transaction-table th,
  .report-cash-table th {
    border-bottom: 1px solid rgb(203 213 225);
    color: rgb(71 85 105);
    font-size: 0.625rem;
    font-weight: 700;
    letter-spacing: 0.04em;
    padding: 0.4375rem 0.375rem;
    text-align: left;
    text-transform: uppercase;
  }

  .report-transaction-table th.numeric,
  .report-transaction-table td.numeric,
  .report-cash-table th.numeric,
  .report-cash-table td.numeric {
    text-align: right;
  }

  .report-transaction-table td,
  .report-cash-table td {
    border-bottom: 1px solid rgb(226 232 240);
    color: rgb(15 23 42);
    padding: 0.375rem;
  }

  .report-transaction-table td.numeric,
  .report-cash-table td.numeric {
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  }

  @media (max-width: 780px) {
    .report-valuation-date-grid {
      grid-template-columns: minmax(0, 1fr);
    }

    .report-valuation-date-separator {
      padding-bottom: 0;
      justify-self: start;
    }

    .report-filter-basis-grid,
    .report-filter-report-grid {
      grid-template-columns: minmax(0, 1fr);
    }

    .report-pie-layout {
      grid-template-columns: 1fr;
    }

    .price-basis-toggle {
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
  }

  @media print {
    :global(.page-header),
    form,
    .report-action-bar,
    [role='status'] {
      display: none;
    }

    .report-document-shell {
      gap: 0;
    }

    .report-document-page {
      border: 0;
      border-radius: 0;
      box-shadow: none;
      min-height: 297mm;
      page: ReportPortrait;
      break-before: page;
      break-after: page;
      page-break-before: always;
      page-break-after: always;
      width: 210mm;
    }

    .report-document-page:first-child {
      break-before: auto;
      page-break-before: auto;
    }

    .report-document-page.landscape-page {
      min-height: 210mm;
      page: ReportLandscape;
      width: 297mm;
    }
  }

  @page ReportPortrait {
    size: A4 portrait;
    margin: 0;
    mso-page-orientation: portrait;
  }

  @page ReportLandscape {
    size: A4 landscape;
    margin: 0;
    mso-page-orientation: landscape;
  }
</style>
