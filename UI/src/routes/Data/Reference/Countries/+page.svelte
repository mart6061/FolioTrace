<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { CountryReferenceEvent } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  const countryCount = $derived(data.countries?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.countries ? formatDisplayDateTime(data.countries.asOfDateTime) : 'now');

  type SortKey = 'country' | 'alpha2' | 'alpha3' | 'numeric' | 'lastAudit';

  let sortKey = $state<SortKey>('country');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let addingCountry = $state(false);
  let editingAlpha2 = $state('');
  let submittingAlpha2 = $state('');
  let submittingCreate = $state(false);
  let openHistoryAlpha2 = $state('');
  let historyByAlpha2 = $state<Record<string, { events: CountryReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');

  const filteredCountries = $derived(
    (data.countries?.items ?? []).filter((country) => {
      const filter = filterText.trim().toLocaleLowerCase();

      if (!filter)
        return true;

      return [
        country.name,
        country.alpha2,
        country.alpha3,
        country.numeric.toString().padStart(3, '0'),
        country.lastAuditDateTime
      ].some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  const sortedCountries = $derived(
    [...filteredCountries].sort((left, right) => {
      const direction = sortDirection;

      switch (sortKey) {
        case 'alpha2':
          return direction * left.alpha2.localeCompare(right.alpha2);
        case 'alpha3':
          return direction * left.alpha3.localeCompare(right.alpha3);
        case 'numeric':
          return direction * (left.numeric - right.numeric);
        case 'lastAudit':
          return direction * (new Date(left.lastAuditDateTime).getTime() - new Date(right.lastAuditDateTime).getTime());
        case 'country':
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
    if (openHistoryAlpha2)
      void loadHistory(openHistoryAlpha2);
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

  function countryExportRows() {
    return sortedCountries.map((country) => ({
      country: country.name,
      alpha2: country.alpha2,
      alpha3: country.alpha3,
      numeric: country.numeric.toString().padStart(3, '0'),
      lastAuditDateTime: country.lastAuditDateTime
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
    downloadFile('countries.json', JSON.stringify(countryExportRows(), null, 2), 'application/json');
  }

  function exportCsv() {
    const rows = countryExportRows();
    const header = ['Country', 'Alpha-2', 'Alpha-3', 'Numeric', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...rows.map((row) =>
        [row.country, row.alpha2, row.alpha3, row.numeric, row.lastAuditDateTime].map(csvValue).join(',')
      )
    ];

    downloadFile('countries.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const rows = countryExportRows();
    const html = `
      <table>
        <thead>
          <tr>
            <th>Country</th>
            <th>Alpha-2</th>
            <th>Alpha-3</th>
            <th>Numeric</th>
            <th>Last audit</th>
          </tr>
        </thead>
        <tbody>
          ${rows.map((row) => `
            <tr>
              <td>${htmlValue(row.country)}</td>
              <td>${htmlValue(row.alpha2)}</td>
              <td>${htmlValue(row.alpha3)}</td>
              <td>${htmlValue(row.numeric)}</td>
              <td>${htmlValue(row.lastAuditDateTime)}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    `;

    downloadFile('countries.xls', html, 'application/vnd.ms-excel');
  }

  function printTable() {
    window.print();
  }

  function startEdit(alpha2: string) {
    addingCountry = false;
    editingAlpha2 = alpha2;
  }

  function cancelEdit() {
    editingAlpha2 = '';
  }

  function startAdd() {
    editingAlpha2 = '';
    addingCountry = true;
  }

  function cancelAdd() {
    addingCountry = false;
  }

  const enhanceCountryCreate: SubmitFunction = () => {
    submittingCreate = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;

      if (result.type === 'success')
        addingCountry = false;
    };
  };

  const enhanceCountryEdit: SubmitFunction = ({ formData }) => {
    const alpha2 = formData.get('alpha2');

    submittingAlpha2 = typeof alpha2 === 'string' ? alpha2 : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingAlpha2 = '';

      if (result.type === 'success')
        editingAlpha2 = '';
    };
  };

  async function toggleHistory(alpha2: string) {
    if (openHistoryAlpha2 === alpha2) {
      openHistoryAlpha2 = '';
      delete historyByAlpha2[alpha2];
      return;
    }

    openHistoryAlpha2 = alpha2;

    if (historyByAlpha2[alpha2])
      return;

    await loadHistory(alpha2);
  }

  async function loadHistory(alpha2: string) {
    historyByAlpha2[alpha2] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Reference/Countries/History', window.location.origin);
      historyUrl.searchParams.set('alpha2', alpha2);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByAlpha2[alpha2] = {
        events: await response.json() as CountryReferenceEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByAlpha2[alpha2] = {
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
      data.countries?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
  }

  function countryEventSummary(event: CountryReferenceEvent) {
    if (event.$type === 'CountryFlagModifiedEvent')
      return 'Flag updated';

    return [
      event.name,
      event.alpha2,
      event.alpha3,
      typeof event.numeric === 'number' ? event.numeric.toString().padStart(3, '0') : ''
    ].filter(Boolean).join(' · ');
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <p class="page-kicker">Reference Data</p>
        <div class="page-title-row">
          <h1 class="page-title">Countries</h1>
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
          <input
            name="auditDateTime"
            type="hidden"
            value={data.auditDateTime}
          />
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
    {:else if data.countries}
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

      <AggregateUpdateWatcher aggregateKind="Countries" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.countries.lastEventID} />

      <div class="data-summary">
        <div>
          <span class="font-semibold text-slate-950">{countryCount}</span>
          countries
        </div>
        <div>
          Valuation {formatDisplayDateTime(data.countries.valuationDateTime)} · As-of {asOfSummary}
        </div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter countries</span>
            <input
              bind:value={filterText}
              placeholder="Filter countries..."
              type="search"
            />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add country" onclick={startAdd} title="Add country" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M12 5v14M5 12h14" />
              </svg>
            </button>
            <button aria-label="Export countries to JSON" onclick={exportJson} title="Export JSON" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" />
              </svg>
            </button>
            <button aria-label="Export countries to CSV" onclick={exportCsv} title="Export CSV" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M4 4h16v16H4zM4 10h16M10 4v16" />
              </svg>
            </button>
            <button aria-label="Export countries to XLSX" onclick={exportXlsx} title="Export XLSX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" />
              </svg>
            </button>
            <button aria-label="Print countries" onclick={printTable} title="Print" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" />
              </svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="w-14 px-3 py-2">Flag</th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('country')} type="button">
                    Country{sortLabel('country')}
                  </button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('alpha2')} type="button">
                    Alpha-2{sortLabel('alpha2')}
                  </button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('alpha3')} type="button">
                    Alpha-3{sortLabel('alpha3')}
                  </button>
                </th>
                <th class="px-3 py-2 text-right">
                  <button class="table-sort-button ml-auto" onclick={() => setSort('numeric')} type="button">
                    Numeric{sortLabel('numeric')}
                  </button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('lastAudit')} type="button">
                    Last audit{sortLabel('lastAudit')}
                  </button>
                </th>
                <th class="w-40 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#if addingCountry}
                <tr class="bg-teal-50/30 align-top">
                  <td class="px-3 py-2"></td>
                  <td class="px-3 py-2">
                    <form
                      id="country-create"
                      action="?/createCountry"
                      method="POST"
                      use:enhance={enhanceCountryCreate}
                    >
                      <label class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Country</span>
                        <input
                          class="h-8 w-full rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                          name="name"
                          required
                          type="text"
                          value={form?.intent === 'createCountry' ? (form.values?.name ?? '') : ''}
                        />
                      </label>
                    </form>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="country-create">
                      <span>Alpha-2</span>
                      <input
                        class="h-8 w-20 rounded-md border border-slate-300 bg-white px-2 font-mono text-sm uppercase text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                        form="country-create"
                        maxlength="2"
                        minlength="2"
                        name="alpha2"
                        required
                        type="text"
                        value={form?.intent === 'createCountry' ? (form.values?.alpha2 ?? '') : ''}
                      />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="country-create">
                      <span>Alpha-3</span>
                      <input
                        class="h-8 w-24 rounded-md border border-slate-300 bg-white px-2 font-mono text-sm uppercase text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                        form="country-create"
                        maxlength="3"
                        minlength="3"
                        name="alpha3"
                        required
                        type="text"
                        value={form?.intent === 'createCountry' ? (form.values?.alpha3 ?? '') : ''}
                      />
                    </label>
                  </td>
                  <td class="px-3 py-2 text-right">
                    <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form="country-create">
                      <span>Numeric</span>
                      <input
                        class="h-8 w-24 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                        form="country-create"
                        max="999"
                        min="0"
                        name="numeric"
                        required
                        type="number"
                        value={form?.intent === 'createCountry' ? (form.values?.numeric ?? '') : ''}
                      />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="country-create">
                      <span>Event date</span>
                      <DateTimeInput
                        class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                        form="country-create"
                        name="eventDateTime"
                        required
                        step="1"
                        value={form?.intent === 'createCountry' ? (form.values?.eventDateTime ?? eventDateDefault) : eventDateDefault}
                      />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button
                          class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400"
                          onclick={cancelAdd}
                          type="button"
                        >
                          Cancel
                        </button>
                        <button
                          class="h-8 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70"
                          disabled={submittingCreate}
                          form="country-create"
                          type="submit"
                        >
                          {submittingCreate ? 'Adding' : 'Add'}
                        </button>
                      </div>
                    </div>
                  </td>
                </tr>
              {/if}

              {#each sortedCountries as country}
                {#if editingAlpha2 === country.alpha2}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      {#if country.flag?.svg}
                        <span class="flag" aria-label={`${country.name} flag`}>{@html country.flag.svg}</span>
                      {/if}
                    </td>
                    <td class="px-3 py-2">
                      <form
                        id={`country-edit-${country.alpha2}`}
                        action="?/modifyCountry"
                        method="POST"
                        use:enhance={enhanceCountryEdit}
                      >
                        <input name="alpha2" type="hidden" value={country.alpha2} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Country</span>
                          <input
                            class="h-8 w-full rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                            name="name"
                            required
                            type="text"
                            value={form?.alpha2 === country.alpha2 ? (form.values?.name ?? country.name) : country.name}
                          />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Alpha-2</span>
                        <span class="h-8 py-1.5 font-mono text-sm font-normal text-slate-700">
                          {country.alpha2}
                        </span>
                      </div>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`country-edit-${country.alpha2}`}>
                        <span>Alpha-3</span>
                        <input
                          class="h-8 w-24 rounded-md border border-slate-300 bg-white px-2 font-mono text-sm uppercase text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                          form={`country-edit-${country.alpha2}`}
                          maxlength="3"
                          minlength="3"
                          name="alpha3"
                          required
                          type="text"
                          value={form?.alpha2 === country.alpha2 ? (form.values?.alpha3 ?? country.alpha3) : country.alpha3}
                        />
                      </label>
                    </td>
                    <td class="px-3 py-2 text-right">
                      <label class="grid justify-end gap-1 text-xs font-medium text-slate-600" form={`country-edit-${country.alpha2}`}>
                        <span>Numeric</span>
                        <input
                          class="h-8 w-24 rounded-md border border-slate-300 bg-white px-2 text-right font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                          form={`country-edit-${country.alpha2}`}
                          max="999"
                          min="0"
                          name="numeric"
                          required
                          type="number"
                          value={form?.alpha2 === country.alpha2 ? (form.values?.numeric ?? country.numeric.toString().padStart(3, '0')) : country.numeric.toString().padStart(3, '0')}
                        />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`country-edit-${country.alpha2}`}>
                        <span>Event date</span>
                        <DateTimeInput
                          class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20"
                          form={`country-edit-${country.alpha2}`}
                          name="eventDateTime"
                          required
                          step="1"
                          value={form?.alpha2 === country.alpha2 ? (form.values?.eventDateTime ?? eventDateDefault) : eventDateDefault}
                        />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                        <span>Actions</span>
                        <div class="flex justify-end gap-2">
                          <button
                            class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400"
                            onclick={cancelEdit}
                            type="button"
                          >
                            Cancel
                          </button>
                          <button
                            class="h-8 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70"
                            disabled={submittingAlpha2 === country.alpha2}
                            form={`country-edit-${country.alpha2}`}
                            type="submit"
                          >
                            {submittingAlpha2 === country.alpha2 ? 'Saving' : 'Save'}
                          </button>
                        </div>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="hover:bg-slate-50">
                    <td class="px-3 py-2">
                      {#if country.flag?.svg}
                        <span class="flag" aria-label={`${country.name} flag`}>{@html country.flag.svg}</span>
                      {/if}
                    </td>
                    <td class="px-3 py-2 font-medium text-slate-950">{country.name}</td>
                    <td class="px-3 py-2 font-mono text-slate-700">{country.alpha2}</td>
                    <td class="px-3 py-2 font-mono text-slate-700">{country.alpha3}</td>
                    <td class="px-3 py-2 text-right font-mono text-slate-700">
                      {country.numeric.toString().padStart(3, '0')}
                    </td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(country.lastAuditDateTime)}</td>
                    <td class="px-3 py-2">
                      <div class="flex justify-end gap-2">
                        <button
                          class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700"
                          onclick={() => toggleHistory(country.alpha2)}
                          type="button"
                        >
                          {openHistoryAlpha2 === country.alpha2 ? 'Hide' : 'History'}
                        </button>
                        <button
                          class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700"
                          onclick={() => startEdit(country.alpha2)}
                          type="button"
                        >
                          Edit
                        </button>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryAlpha2 === country.alpha2}
                    {@const history = historyByAlpha2[country.alpha2]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="7">
                        <div>
                          {#if history?.loading}
                            <div class="text-sm text-slate-600">Loading history...</div>
                          {:else if history?.error}
                            <div class="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">{history.error}</div>
                          {:else}
                            <HistoryEventsCard
                              eventDateTime={data.valuationDate}
                              asAtDateTime={data.auditDateTime}
                              events={history?.events ?? []}
                              emptyMessage="No history found for this country."
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
</main>
