<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { BrokerReferenceEvent } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  type SortKey = 'broker' | 'lei' | 'commission' | 'active' | 'approved' | 'nextReview' | 'lastAudit';
  type BrokerAction =
    | 'modifyBroker'
    | 'setBrokerActive'
    | 'setBrokerApprovedDateTime'
    | 'setBrokerNextReview'
    | 'setBrokerNotes';

  let sortKey = $state<SortKey>('broker');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let addingBroker = $state(false);
  let editingLei = $state('');
  let submittingKey = $state('');
  let submittingCreate = $state(false);
  let openHistoryLei = $state('');
  let historyByLei = $state<Record<string, { events: BrokerReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');

  const brokerCount = $derived(data.brokers?.items.length ?? 0);
  const activeBrokerCount = $derived((data.brokers?.items ?? []).filter((broker) => broker.active).length);
  const asOfSummary = $derived(data.auditDateTime && data.brokers ? formatDisplayDateTime(data.brokers.asOfDateTime) : 'now');

  const filteredBrokers = $derived(
    (data.brokers?.items ?? []).filter((broker) => {
      const filter = filterText.trim().toLocaleLowerCase();

      if (!filter)
        return true;

      return [
        broker.name,
        broker.lei,
        broker.commission.toString(),
        broker.active ? 'active' : 'inactive',
        broker.approvedDateTime,
        broker.nextReview,
        broker.notes,
        broker.lastAuditDateTime
      ].some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  const sortedBrokers = $derived(
    [...filteredBrokers].sort((left, right) => {
      const direction = sortDirection;

      switch (sortKey) {
        case 'lei':
          return direction * left.lei.localeCompare(right.lei);
        case 'commission':
          return direction * (left.commission - right.commission);
        case 'active':
          return direction * (Number(left.active) - Number(right.active));
        case 'approved':
          return direction * (new Date(left.approvedDateTime).getTime() - new Date(right.approvedDateTime).getTime());
        case 'nextReview':
          return direction * (new Date(left.nextReview).getTime() - new Date(right.nextReview).getTime());
        case 'lastAudit':
          return direction * (new Date(left.lastAuditDateTime).getTime() - new Date(right.lastAuditDateTime).getTime());
        case 'broker':
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
    if (openHistoryLei)
      void loadHistory(openHistoryLei);
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

    return sortDirection === 1 ? ' ^' : ' v';
  }

  function formText(key: string, fallback = '') {
    const values = (form?.values ?? {}) as Record<string, unknown>;
    const value = values[key];

    if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean')
      return String(value);

    return fallback;
  }

  function formBoolean(key: string, fallback = false) {
    const values = (form?.values ?? {}) as Record<string, unknown>;
    const value = values[key];

    return typeof value === 'boolean' ? value : fallback;
  }

  function brokerExportRows() {
    return sortedBrokers.map((broker) => ({
      active: broker.active ? 'Active' : 'Inactive',
      approvedDateTime: broker.approvedDateTime,
      broker: broker.name,
      commission: formatFeeRate(broker.commission),
      lastAuditDateTime: broker.lastAuditDateTime,
      lei: broker.lei,
      nextReview: broker.nextReview,
      notes: broker.notes
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
    downloadFile('brokers.json', JSON.stringify(brokerExportRows(), null, 2), 'application/json');
  }

  function exportCsv() {
    const rows = brokerExportRows();
    const header = ['Broker', 'LEI', 'Commission', 'Status', 'Approved', 'Next review', 'Notes', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...rows.map((row) =>
        [row.broker, row.lei, row.commission, row.active, row.approvedDateTime, row.nextReview, row.notes, row.lastAuditDateTime].map(csvValue).join(',')
      )
    ];

    downloadFile('brokers.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const rows = brokerExportRows();
    const html = `
      <table>
        <thead>
          <tr>
            <th>Broker</th>
            <th>LEI</th>
            <th>Commission</th>
            <th>Status</th>
            <th>Approved</th>
            <th>Next review</th>
            <th>Notes</th>
            <th>Last audit</th>
          </tr>
        </thead>
        <tbody>
          ${rows.map((row) => `
            <tr>
              <td>${htmlValue(row.broker)}</td>
              <td>${htmlValue(row.lei)}</td>
              <td>${htmlValue(row.commission)}</td>
              <td>${htmlValue(row.active)}</td>
              <td>${htmlValue(row.approvedDateTime)}</td>
              <td>${htmlValue(row.nextReview)}</td>
              <td>${htmlValue(row.notes)}</td>
              <td>${htmlValue(row.lastAuditDateTime)}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    `;

    downloadFile('brokers.xls', html, 'application/vnd.ms-excel');
  }

  function printTable() {
    window.print();
  }

  function startEdit(lei: string) {
    addingBroker = false;
    editingLei = lei;
  }

  function cancelEdit() {
    editingLei = '';
  }

  function startAdd() {
    editingLei = '';
    addingBroker = true;
  }

  function cancelAdd() {
    addingBroker = false;
  }

  const enhanceBrokerCreate: SubmitFunction = () => {
    submittingCreate = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;

      if (result.type === 'success')
        addingBroker = false;
    };
  };

  function enhanceBrokerAction(intent: BrokerAction): SubmitFunction {
    return ({ formData }) => {
      const lei = formData.get('lei');
      submittingKey = `${intent}:${typeof lei === 'string' ? lei : ''}`;

      return async ({ result, update }) => {
        await update({ reset: false });
        submittingKey = '';

        if (result.type === 'success' && intent === 'modifyBroker')
          editingLei = '';
      };
    };
  }

  async function toggleHistory(lei: string) {
    if (openHistoryLei === lei) {
      openHistoryLei = '';
      delete historyByLei[lei];
      return;
    }

    openHistoryLei = lei;

    if (historyByLei[lei])
      return;

    await loadHistory(lei);
  }

  async function loadHistory(lei: string) {
    historyByLei[lei] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Reference/Brokers/History', window.location.origin);
      historyUrl.searchParams.set('lei', lei);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByLei[lei] = {
        events: await response.json() as BrokerReferenceEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByLei[lei] = {
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
      data.brokers?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
  }

  function brokerEventSummary(event: BrokerReferenceEvent) {
    return [
      event.name,
      event.lei,
      event.commission !== undefined ? `commission ${formatFeeRate(event.commission)}` : '',
      event.active !== undefined ? (event.active ? 'active' : 'inactive') : '',
      event.approvedDateTime ? `approved ${formatTableDateTime(event.approvedDateTime)}` : '',
      event.nextReview ? `review ${formatTableDateTime(event.nextReview)}` : '',
      event.notes
    ].filter(Boolean).join(' | ');
  }

  function formatFeeRate(value: number) {
    return value.toLocaleString(undefined, { maximumFractionDigits: 8, minimumFractionDigits: 0 });
  }

  function inputDateTime(value: string) {
    const date = new Date(value);

    if (Number.isNaN(date.getTime()))
      return eventDateDefault;

    const pad = (part: number) => part.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
  }

  function isSubmitting(intent: BrokerAction, lei: string) {
    return submittingKey === `${intent}:${lei}`;
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-main">
        <p class="page-kicker">Reference Data</p>
        <div class="page-title-row">
          <h1 class="page-title">Brokers</h1>
          <BookmarkButton />
        </div>
      </div>

      <form class="house-form grid gap-4 md:grid-cols-[minmax(0,var(--house-datetime-width))_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput
            fullWidth
            name="valuationDate"
            step="1"
            value={data.valuationDate}
          />
        </label>

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

  <section class="page-container page-section">
    {#if data.error}
      <div class="status-panel status-panel-error">
        {data.error}
      </div>
    {:else if data.brokers}
      {#if form?.message}
        <div class={['status-panel mb-4', form.status === 'success' ? 'status-panel-success' : 'status-panel-error']} role="status">
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
        </div>
      {/if}

      <AggregateUpdateWatcher aggregateKind="Brokers" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.brokers.lastEventID} />

      <div class="data-summary">
        <div>
          <span class="font-semibold text-slate-950">{brokerCount}</span>
          brokers
          <span class="mx-2 text-slate-300">|</span>
          <span class="font-semibold text-slate-950">{activeBrokerCount}</span>
          active
        </div>
        <div>
          Valuation {formatDisplayDateTime(data.brokers.valuationDateTime)} | As-of {asOfSummary}
        </div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter brokers</span>
            <input bind:value={filterText} placeholder="Filter brokers..." type="search" />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add broker" onclick={startAdd} title="Add broker" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
            </button>
            <button aria-label="Export brokers to JSON" onclick={exportJson} title="Export JSON" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" /></svg>
            </button>
            <button aria-label="Export brokers to CSV" onclick={exportCsv} title="Export CSV" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 4h16v16H4zM4 10h16M10 4v16" /></svg>
            </button>
            <button aria-label="Export brokers to XLSX" onclick={exportXlsx} title="Export XLSX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" /></svg>
            </button>
            <button aria-label="Print brokers" onclick={printTable} title="Print" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" /></svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="min-w-56 px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('broker')} type="button">Broker{sortLabel('broker')}</button>
                </th>
                <th class="min-w-48 px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('lei')} type="button">LEI{sortLabel('lei')}</button>
                </th>
                <th class="min-w-28 px-3 py-2 text-right">
                  <button class="table-sort-button ml-auto" onclick={() => setSort('commission')} type="button">Commission{sortLabel('commission')}</button>
                </th>
                <th class="min-w-28 px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('active')} type="button">Status{sortLabel('active')}</button>
                </th>
                <th class="min-w-44 px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('approved')} type="button">Approved{sortLabel('approved')}</button>
                </th>
                <th class="min-w-44 px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('nextReview')} type="button">Next review{sortLabel('nextReview')}</button>
                </th>
                <th class="min-w-56 px-3 py-2">Notes</th>
                <th class="min-w-36 px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('lastAudit')} type="button">Last audit{sortLabel('lastAudit')}</button>
                </th>
                <th class="w-48 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#if addingBroker}
                <tr class="bg-teal-50/30 align-top">
                  <td class="px-3 py-2">
                    <form id="broker-create" action="?/createBroker" method="POST" use:enhance={enhanceBrokerCreate}>
                      <label class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Broker</span>
                        <input class="house-control house-control-sm house-control-full" name="name" required type="text" value={form?.intent === 'createBroker' ? formText('name') : ''} />
                      </label>
                    </form>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="broker-create">
                      <span>LEI</span>
                      <input class="house-control house-control-sm w-48 font-mono uppercase" form="broker-create" maxlength="20" minlength="20" name="lei" required type="text" value={form?.intent === 'createBroker' ? formText('lei') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2 text-right">
                    <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form="broker-create">
                      <span>Commission</span>
                      <input class="house-control house-control-sm w-28 text-right font-mono" form="broker-create" min="0" name="commission" required step="0.00000001" type="number" value={form?.intent === 'createBroker' ? formText('commission') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="broker-create">
                      <span>Status</span>
                      <select class="house-control house-control-sm w-28" form="broker-create" name="active" value={form?.intent === 'createBroker' && !formBoolean('active', true) ? 'false' : 'true'}>
                        <option value="true">Active</option>
                        <option value="false">Inactive</option>
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="broker-create">
                      <span>Approved</span>
                      <DateTimeInput size="sm" form="broker-create" name="approvedDateTime" required step="1" value={form?.intent === 'createBroker' ? formText('approvedDateTime', eventDateDefault) : eventDateDefault} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="broker-create">
                      <span>Next review</span>
                      <DateTimeInput size="sm" form="broker-create" name="nextReview" required step="1" value={form?.intent === 'createBroker' ? formText('nextReview', eventDateDefault) : eventDateDefault} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="broker-create">
                      <span>Notes</span>
                      <input class="house-control house-control-sm house-control-full" form="broker-create" name="notes" type="text" value={form?.intent === 'createBroker' ? formText('notes') : ''} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="broker-create">
                      <span>Event date</span>
                      <DateTimeInput size="sm" form="broker-create" name="eventDateTime" required step="1" value={form?.intent === 'createBroker' ? formText('eventDateTime', eventDateDefault) : eventDateDefault} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={cancelAdd} type="button">Cancel</button>
                        <button class="house-button house-button-primary house-button-sm" disabled={submittingCreate} form="broker-create" type="submit">{submittingCreate ? 'Adding' : 'Add'}</button>
                      </div>
                    </div>
                  </td>
                </tr>
              {/if}

              {#each sortedBrokers as broker (broker.lei)}
                {#if editingLei === broker.lei}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      <form id={`broker-edit-${broker.lei}`} action="?/modifyBroker" method="POST" use:enhance={enhanceBrokerAction('modifyBroker')}>
                        <input name="lei" type="hidden" value={broker.lei} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Broker</span>
                          <input class="house-control house-control-sm house-control-full" name="name" required type="text" value={form?.lei === broker.lei ? formText('name', broker.name) : broker.name} />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>LEI</span>
                        <span class="h-8 py-1.5 font-mono text-sm font-normal text-slate-700">{broker.lei}</span>
                      </div>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`broker-edit-${broker.lei}`}>
                        <span>Commission</span>
                        <input class="house-control house-control-sm w-28 text-right font-mono" form={`broker-edit-${broker.lei}`} min="0" name="commission" required step="0.00000001" type="number" value={form?.lei === broker.lei ? formText('commission', broker.commission.toString()) : broker.commission} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <form id={`broker-active-${broker.lei}`} action="?/setBrokerActive" method="POST" use:enhance={enhanceBrokerAction('setBrokerActive')}>
                        <input name="lei" type="hidden" value={broker.lei} />
                        <input name="active" type="hidden" value={broker.active ? 'false' : 'true'} />
                        <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                        <div class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Status</span>
                          <button class={`h-8 rounded-full px-3 text-xs font-semibold ${broker.active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'}`} disabled={isSubmitting('setBrokerActive', broker.lei)} type="submit">
                            {broker.active ? 'Active' : 'Inactive'}
                          </button>
                        </div>
                      </form>
                    </td>
                    <td class="px-3 py-2">
                      <form id={`broker-approved-${broker.lei}`} action="?/setBrokerApprovedDateTime" method="POST" use:enhance={enhanceBrokerAction('setBrokerApprovedDateTime')}>
                        <input name="lei" type="hidden" value={broker.lei} />
                        <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Approved</span>
                          <DateTimeInput size="sm" name="approvedDateTime" required step="1" value={form?.lei === broker.lei ? formText('approvedDateTime', inputDateTime(broker.approvedDateTime)) : inputDateTime(broker.approvedDateTime)} />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2">
                      <form id={`broker-review-${broker.lei}`} action="?/setBrokerNextReview" method="POST" use:enhance={enhanceBrokerAction('setBrokerNextReview')}>
                        <input name="lei" type="hidden" value={broker.lei} />
                        <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Next review</span>
                          <DateTimeInput size="sm" name="nextReview" required step="1" value={form?.lei === broker.lei ? formText('nextReview', inputDateTime(broker.nextReview)) : inputDateTime(broker.nextReview)} />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2">
                      <form id={`broker-notes-${broker.lei}`} action="?/setBrokerNotes" method="POST" use:enhance={enhanceBrokerAction('setBrokerNotes')}>
                        <input name="lei" type="hidden" value={broker.lei} />
                        <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Notes</span>
                          <input class="house-control house-control-sm house-control-full" name="notes" type="text" value={form?.lei === broker.lei ? formText('notes', broker.notes) : broker.notes} />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`broker-edit-${broker.lei}`}>
                        <span>Event date</span>
                        <DateTimeInput size="sm" form={`broker-edit-${broker.lei}`} name="eventDateTime" required step="1" value={form?.lei === broker.lei ? formText('eventDateTime', eventDateDefault) : eventDateDefault} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                        <span>Actions</span>
                        <div class="flex flex-wrap justify-end gap-2">
                          <button class="house-button house-button-secondary house-button-sm" onclick={cancelEdit} type="button">Cancel</button>
                          <button class="house-button house-button-secondary house-button-sm" disabled={isSubmitting('setBrokerApprovedDateTime', broker.lei)} form={`broker-approved-${broker.lei}`} type="submit">Approved</button>
                          <button class="house-button house-button-secondary house-button-sm" disabled={isSubmitting('setBrokerNextReview', broker.lei)} form={`broker-review-${broker.lei}`} type="submit">Review</button>
                          <button class="house-button house-button-secondary house-button-sm" disabled={isSubmitting('setBrokerNotes', broker.lei)} form={`broker-notes-${broker.lei}`} type="submit">Notes</button>
                          <button class="house-button house-button-primary house-button-sm" disabled={isSubmitting('modifyBroker', broker.lei)} form={`broker-edit-${broker.lei}`} type="submit">{isSubmitting('modifyBroker', broker.lei) ? 'Saving' : 'Save'}</button>
                        </div>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="hover:bg-slate-50">
                    <td class="px-3 py-2 font-medium text-slate-950">{broker.name}</td>
                    <td class="px-3 py-2 font-mono text-slate-700">{broker.lei}</td>
                    <td class="px-3 py-2 text-right font-mono text-slate-700">{formatFeeRate(broker.commission)}</td>
                    <td class="px-3 py-2">
                      <span class={`rounded-full px-2.5 py-1 text-xs font-semibold ${broker.active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'}`}>
                        {broker.active ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(broker.approvedDateTime)}</td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(broker.nextReview)}</td>
                    <td class="max-w-72 truncate px-3 py-2 text-slate-600" title={broker.notes}>{broker.notes}</td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(broker.lastAuditDateTime)}</td>
                    <td class="px-3 py-2">
                      <div class="flex justify-end gap-2">
                        <button class="house-button house-button-secondary house-button-sm" onclick={() => toggleHistory(broker.lei)} type="button">
                          {openHistoryLei === broker.lei ? 'Hide' : 'History'}
                        </button>
                        <button class="house-button house-button-secondary house-button-sm" onclick={() => startEdit(broker.lei)} type="button">
                          Edit
                        </button>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryLei === broker.lei}
                    {@const history = historyByLei[broker.lei]}
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
                              emptyMessage="No history found for this broker."
                            />
                          {/if}
                        </div>
                      </td>
                    </tr>
                  {/if}
                {/if}
              {:else}
                <tr>
                  <td class="px-3 py-8 text-center text-sm text-slate-500" colspan="9">No brokers found.</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </section>
</main>
