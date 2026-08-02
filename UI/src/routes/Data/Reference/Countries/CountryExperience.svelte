<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import SortableHeader from '$lib/components/page/SortableHeader.svelte';
  import TableTools from '$lib/components/page/TableTools.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { TableExportDefinition } from '$lib/export';
  import type { CountryReferenceEvent } from '$lib/types';
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
  const countryCount = $derived(data.countries?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.countries ? formatDisplayDateTime(data.countries.asOfDateTime) : 'now');

  type SortKey = 'country' | 'alpha2' | 'alpha3' | 'numeric' | 'lastAudit';

  let sortKey = $state<SortKey>('country');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let debouncedFilterText = $state('');
  let addingCountry = $state(false);
  let editingAlpha2 = $state('');
  let submittingAlpha2 = $state('');
  let submittingCreate = $state(false);
  let openHistoryAlpha2 = $state('');
  let historyByAlpha2 = $state<Record<string, { events: CountryReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');

  $effect(() => {
    const value = filterText;
    const timeout = setTimeout(() => {
      debouncedFilterText = value;
    }, 200);

    return () => clearTimeout(timeout);
  });

  const filteredCountries = $derived(
    (data.countries?.items ?? []).filter((country) => {
      const filter = debouncedFilterText.trim().toLocaleLowerCase();

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


  const countryExportDefinition = $derived.by((): TableExportDefinition => ({
    fileName: 'countries',
    sheetName: 'Countries',
    columns: [
      { key: 'country', label: 'Country', kind: 'text' },
      { key: 'alpha2', label: 'Alpha-2', kind: 'text' },
      { key: 'alpha3', label: 'Alpha-3', kind: 'text' },
      { key: 'numeric', label: 'Numeric', kind: 'text' },
      { key: 'lastAuditDateTime', label: 'Last audit', kind: 'datetime' }
    ],
    rows: sortedCountries.map((country) => ({
      country: country.name,
      alpha2: country.alpha2,
      alpha3: country.alpha3,
      numeric: country.numeric.toString().padStart(3, '0'),
      lastAuditDateTime: country.lastAuditDateTime
    }))
  }));

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

<main class={shellClass}>
  {#if showFilter}
  <section class="page-header">
    <div class="page-container">
      {#if showHeader}
        <div class="page-header-main">
          <p class="page-kicker">Reference Data</p>
          <div class="page-title-row">
            <h1 class="page-title">Countries</h1>
            <BookmarkButton />
          </div>
        </div>
      {/if}

      <form action={formAction} class="house-form grid gap-4 md:grid-cols-[var(--house-datetime-width)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput
            fullWidth
            name="valuationDate"
            step="1"
            value={data.valuationDate}
          />
        </label>

        {#if selectedSection}
          <input name="section" type="hidden" value={selectedSection} />
        {/if}

        {#if data.auditDateTime}
          <input
            name="auditDateTime"
            type="hidden"
            value={data.auditDateTime}
          />
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
  {/if}

  {#if showBody}
  <section class="page-container page-section">
    {#if data.error}
      <Card density="compact" intent="error">
        {data.error}
      </Card>
    {:else if data.countries}
      {#if form?.message}
        <Card class="mb-4" density="compact" intent={form.status === 'success' ? 'success' : 'error'} role="status">
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
        </Card>
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
        <TableTools bind:filterText filterLabel="Filter countries" placeholder="Filter countries..." onadd={startAdd} exportDefinition={countryExportDefinition} onprint={printTable} />

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="w-14 px-3 py-2">Flag</th>
                <SortableHeader activeKey={sortKey} class="px-3 py-2" direction={sortDirection} onsort={setSort} sortKey="country">Country</SortableHeader>
                <SortableHeader activeKey={sortKey} class="px-3 py-2" direction={sortDirection} onsort={setSort} sortKey="alpha2">Alpha-2</SortableHeader>
                <SortableHeader activeKey={sortKey} class="px-3 py-2" direction={sortDirection} onsort={setSort} sortKey="alpha3">Alpha-3</SortableHeader>
                <SortableHeader activeKey={sortKey} class="px-3 py-2 text-right" buttonClass="ml-auto" direction={sortDirection} onsort={setSort} sortKey="numeric">Numeric</SortableHeader>
                <SortableHeader activeKey={sortKey} class="px-3 py-2" direction={sortDirection} onsort={setSort} sortKey="lastAudit">Last audit</SortableHeader>
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
                          class="house-control house-control-sm house-control-full"
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
                        class="house-control house-control-sm w-20 font-mono uppercase"
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
                        class="house-control house-control-sm w-24 font-mono uppercase"
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
                        class="house-control house-control-sm w-24 text-right font-mono"
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
                        size="sm"
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
                          class="house-button house-button-secondary house-button-sm"
                          onclick={cancelAdd}
                          type="button"
                        >
                          Cancel
                        </button>
                        <button
                          class="house-button house-button-primary house-button-sm"
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
                            class="house-control house-control-sm house-control-full"
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
                          class="house-control house-control-sm w-24 font-mono uppercase"
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
                          class="house-control house-control-sm w-24 text-right font-mono"
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
                          size="sm"
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
                            class="house-button house-button-secondary house-button-sm"
                            onclick={cancelEdit}
                            type="button"
                          >
                            Cancel
                          </button>
                          <button
                            class="house-button house-button-primary house-button-sm"
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
                          class="house-button house-button-secondary house-button-sm"
                          onclick={() => toggleHistory(country.alpha2)}
                          type="button"
                        >
                          {openHistoryAlpha2 === country.alpha2 ? 'Hide' : 'History'}
                        </button>
                        <button
                          class="house-button house-button-secondary house-button-sm"
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
                            <Card density="compact" intent="error">{history.error}</Card>
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
  {/if}
</main>
