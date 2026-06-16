<script lang="ts">
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { holdingDateBasisOptions } from '$lib/valuationPreferences';

  let { data } = $props();

  let shareStatus = $state('');
  let reportDocumentElement = $state<HTMLElement | null>(null);

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

  function reportHtml() {
    const documentElement = reportDocumentElement;

    if (!(documentElement instanceof HTMLElement))
      return '';

    return `<!doctype html>
      <html>
        <head>
          <meta charset="utf-8" />
          <title>${htmlValue(data.reportDocument?.name ?? 'Report')}</title>
          <style>
            html, body { margin: 0; padding: 0; }
            body { font-family: Arial, sans-serif; color: #0f172a; }
            .report-document-shell { display: block; margin: 0; padding: 0; }
            .report-document-page { box-sizing: border-box; width: 210mm; min-height: 297mm; padding: 22mm 18mm; page: report-portrait; break-after: page; page-break-after: always; }
            .report-document-page.landscape-page { width: 297mm; min-height: 210mm; page: report-landscape; }
            .report-document-header { border-bottom: 1px solid #cbd5e1; padding-bottom: 14px; }
            .report-document-header h2 { margin: 0; font-size: 30px; line-height: 1.2; }
            .report-document-header p { margin: 6px 0 0; color: #475569; font-size: 16px; }
            @page report-portrait { size: 210mm 297mm; margin: 0; mso-page-orientation: portrait; }
            @page report-landscape { size: 297mm 210mm; margin: 0; mso-page-orientation: landscape; }
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
    frame.style.position = 'fixed';
    frame.style.width = '0';
    frame.style.height = '0';
    frame.style.border = '0';
    frame.style.opacity = '0';

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
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section grid gap-5">
    <form class="grid gap-4 rounded-md border border-slate-200 bg-white p-4 shadow-sm" method="GET">
      {#if data.auditDateTime}
        <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
      {/if}

      <div class="grid gap-3 md:grid-cols-[minmax(240px,24rem)]">
        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="valuationDate" step="1" value={data.valuationDate} />
        </label>
      </div>

      <div class="grid gap-3 border-t border-slate-200 pt-4 md:grid-cols-3">
        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Account
          <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" disabled={!data.accounts.length} name="accountID" required>
            {#each data.accounts as account (account.accountID)}
              <option selected={account.accountID === data.accountID} value={account.accountID}>{account.name}</option>
            {:else}
              <option value="">No active accounts</option>
            {/each}
          </select>
        </label>

        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Price basis
          <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="instrumentPriceBasis">
            {#each data.instrumentPriceBasisOptions as option (option)}
              <option selected={option === data.instrumentPriceBasis} value={option}>{option}</option>
            {/each}
          </select>
        </label>

        <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
          Holding basis
          <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="holdingDateBasis">
            {#each holdingDateBasisOptions as option (option.value)}
              <option selected={option.value === data.holdingDateBasis} value={option.value}>{option.label}</option>
            {/each}
          </select>
        </label>
      </div>

      <div class="grid gap-3 border-t border-slate-200 pt-4 lg:grid-cols-[1fr_auto] lg:items-end">
        <div class="grid gap-2">
          <label class="grid min-w-0 gap-1 text-sm font-medium text-slate-700">
            Report config
            <select class="h-10 w-full min-w-0 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" disabled={!data.reportConfigs.length} name="reportID" required>
              {#each data.reportConfigs as reportConfig (reportConfig.reportID)}
                <option selected={reportConfig.reportID === data.reportID} value={reportConfig.reportID}>{reportConfig.name}</option>
              {:else}
                <option value="">No matching report configs</option>
              {/each}
            </select>
          </label>

          {#if data.accounts.length && !data.reportConfigs.length}
            <div class="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">No active report configs match the selected account and valuation date.</div>
          {/if}
        </div>

        <button class="h-10 rounded-md bg-teal-700 px-5 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-300" disabled={!data.accounts.length || !data.reportConfigs.length} name="create" type="submit" value="true">Create</button>
      </div>
    </form>

    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800" role="status">{data.error}</div>
    {/if}

    {#if data.reportDocument}
      <div class="report-action-bar" aria-label="Report actions">
        <button onclick={exportPdf} title="Export PDF" type="button">Export to PDF</button>
        <button onclick={exportWord} title="Export Word" type="button">Export to Word</button>
        <button onclick={shareReport} title="Share" type="button">Share</button>
        {#if shareStatus}
          <span role="status">{shareStatus}</span>
        {/if}
      </div>

      <section {@attach captureReportDocument} aria-label={data.reportDocument.name} class="report-document-shell">
        {#each data.reportDocument.sections as section (section.reportNodeID)}
          <article class:landscape-page={section.pageOrientation === 'Landscape'} class="report-document-page">
            <header class="report-document-header">
              <h2>{section.name}</h2>
              <p>{data.reportDocument.valuationHeading}</p>
            </header>
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
      page: report-portrait;
      break-after: page;
      page-break-after: always;
      width: 210mm;
    }

    .report-document-page.landscape-page {
      min-height: 210mm;
      page: report-landscape;
      width: 297mm;
    }
  }

  @page report-portrait {
    size: 210mm 297mm;
    margin: 0;
    mso-page-orientation: portrait;
  }

  @page report-landscape {
    size: 297mm 210mm;
    margin: 0;
    mso-page-orientation: landscape;
  }
</style>
