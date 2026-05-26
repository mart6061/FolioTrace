<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import { formatDisplayDateTime, formatTableDateTime, toApiDateTime } from '$lib/dates';
  import type { Holding, HoldingNominalType, HoldingReferenceEvent, HoldingType } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  type SortKey = 'name' | 'type' | 'account' | 'instrument' | 'status' | 'lastAudit';

  const holdingTypes: HoldingType[] = ['Position', 'Nominal', 'CashOnHand', 'CashDebt'];
  const nominalTypes: HoldingNominalType[] = ['Inflow', 'Outflow', 'FeesCustodian', 'FeesAdministrator', 'FeesBank', 'Income', 'Interest'];
  const holdingCount = $derived(data.holdings?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.holdings ? formatDisplayDateTime(data.holdings.asOfDateTime) : 'now');
  const accountsByID = $derived(new Map((data.accounts?.items ?? []).map((account) => [account.accountID, account.name])));
  const instrumentsByID = $derived(new Map((data.instruments?.items ?? []).map((instrument) => [instrument.instrumentID, instrument.name])));

  let sortKey = $state<SortKey>('name');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let addingHolding = $state(false);
  let editingHoldingID = $state('');
  let submittingHoldingID = $state('');
  let submittingCreate = $state(false);
  let openHistoryHoldingID = $state('');
  let historyByHoldingID = $state<Record<string, { events: HoldingReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');

  const filteredHoldings = $derived(
    (data.holdings?.items ?? []).filter((holding: Holding) => {
      const filter = filterText.trim().toLocaleLowerCase();

      if (!filter)
        return true;

      return [
        holding.name,
        holding.holdingType,
        holding.nominalType ?? '',
        accountsByID.get(holding.accountID) ?? '',
        instrumentsByID.get(holding.instrumentID) ?? '',
        holding.active ? 'active' : 'inactive',
        holding.includeInValuation ? 'valuation' : 'excluded'
      ].some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  const sortedHoldings = $derived(
    [...filteredHoldings].sort((left, right) => {
      const direction = sortDirection;

      switch (sortKey) {
        case 'type':
          return direction * left.holdingType.localeCompare(right.holdingType);
        case 'account':
          return direction * (accountsByID.get(left.accountID) ?? '').localeCompare(accountsByID.get(right.accountID) ?? '');
        case 'instrument':
          return direction * (instrumentsByID.get(left.instrumentID) ?? '').localeCompare(instrumentsByID.get(right.instrumentID) ?? '');
        case 'status':
          return direction * Number(left.active === right.active ? 0 : left.active ? -1 : 1);
        case 'lastAudit':
          return direction * (new Date(left.lastAuditDateTime).getTime() - new Date(right.lastAuditDateTime).getTime());
        case 'name':
        default:
          return direction * displayName(left).localeCompare(displayName(right));
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
    if (openHistoryHoldingID)
      void loadHistory(openHistoryHoldingID);
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

  function displayName(holding: Holding) {
    return holding.name || holding.holdingType;
  }

  function startEdit(holdingID: string) {
    addingHolding = false;
    editingHoldingID = holdingID;
  }

  function cancelEdit() {
    editingHoldingID = '';
  }

  function startAdd() {
    editingHoldingID = '';
    addingHolding = true;
  }

  function cancelAdd() {
    addingHolding = false;
  }

  const enhanceHoldingCreate: SubmitFunction = () => {
    submittingCreate = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;

      if (result.type === 'success')
        addingHolding = false;
    };
  };

  const enhanceHoldingEdit: SubmitFunction = ({ formData }) => {
    const holdingID = formData.get('holdingID');
    submittingHoldingID = typeof holdingID === 'string' ? holdingID : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingHoldingID = '';

      if (result.type === 'success')
        editingHoldingID = '';
    };
  };

  const enhanceHoldingActive: SubmitFunction = ({ formData }) => {
    const holdingID = formData.get('holdingID');
    submittingHoldingID = typeof holdingID === 'string' ? holdingID : '';

    return async ({ update }) => {
      await update({ reset: false });
      submittingHoldingID = '';
    };
  };

  async function toggleHistory(holdingID: string) {
    if (openHistoryHoldingID === holdingID) {
      openHistoryHoldingID = '';
      delete historyByHoldingID[holdingID];
      return;
    }

    openHistoryHoldingID = holdingID;

    if (historyByHoldingID[holdingID])
      return;

    await loadHistory(holdingID);
  }

  async function loadHistory(holdingID: string) {
    historyByHoldingID[holdingID] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Reference/Holdings/History', window.location.origin);
      historyUrl.searchParams.set('holdingID', holdingID);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByHoldingID[holdingID] = {
        events: await response.json() as HoldingReferenceEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByHoldingID[holdingID] = {
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
      data.holdings?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
  }

  function holdingEventSummary(event: HoldingReferenceEvent) {
    if (event.$type === 'HoldingActiveModifiedEvent')
      return event.active ? 'Activated' : 'Deactivated';

    return [
      event.name,
      event.holdingType,
      event.nominalType ?? '',
      typeof event.default === 'boolean' ? event.default ? 'Default' : 'Non-default' : ''
    ].filter(Boolean).join(' · ');
  }
</script>

<svelte:head>
  <title>Holdings - FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <p class="page-kicker">Reference Data</p>
        <h1 class="page-title">Holdings</h1>
      </div>

      <form class="grid gap-4 md:grid-cols-[minmax(220px,280px)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <input class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 shadow-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="valuationDate" step="1" type="datetime-local" value={data.valuationDate} />
        </label>

        {#if data.auditDateTime}
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
        {/if}

        <button class="h-10 rounded-md bg-teal-700 px-4 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 focus:outline-none focus:ring-2 focus:ring-teal-600/30" type="submit">Apply</button>
      </form>
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">{data.error}</div>
    {:else if data.holdings}
      {#if form?.message}
        <div class={`mb-4 rounded-md border px-4 py-3 text-sm ${form.status === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-red-200 bg-red-50 text-red-800'}`} role="status">
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
        </div>
      {/if}

      <AggregateUpdateWatcher aggregateKind="Holdings" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.holdings.lastEventID} />

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{holdingCount}</span> holdings</div>
        <div>Valuation {formatDisplayDateTime(data.holdings.valuationDateTime)} · As-of {asOfSummary}</div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter holdings</span>
            <input bind:value={filterText} placeholder="Filter holdings..." type="search" />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add holding" onclick={startAdd} title="Add holding" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('name')} type="button">Name{sortLabel('name')}</button></th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('type')} type="button">Type{sortLabel('type')}</button></th>
                <th class="px-3 py-2">Nominal</th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('account')} type="button">Account{sortLabel('account')}</button></th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('instrument')} type="button">Instrument{sortLabel('instrument')}</button></th>
                <th class="px-3 py-2">Default</th>
                <th class="px-3 py-2">Valuation</th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('status')} type="button">Status{sortLabel('status')}</button></th>
                <th class="px-3 py-2"><button class="table-sort-button" onclick={() => setSort('lastAudit')} type="button">Last audit{sortLabel('lastAudit')}</button></th>
                <th class="w-56 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#if addingHolding}
                <tr class="bg-teal-50/30 align-top">
                  <td class="px-3 py-2">
                    <form id="holding-create" action="?/createHolding" method="POST" use:enhance={enhanceHoldingCreate}>
                      <label class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Name</span>
                        <input class="h-8 w-40 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="name" type="text" value={form?.intent === 'createHolding' ? (form.values?.name ?? '') : ''} />
                      </label>
                    </form>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Type</span>
                      <select class="h-8 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" form="holding-create" name="holdingType">
                        {#each holdingTypes as holdingType}
                          <option value={holdingType}>{holdingType}</option>
                        {/each}
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Nominal</span>
                      <select class="h-8 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" form="holding-create" name="nominalType">
                        <option value="">-</option>
                        {#each nominalTypes as nominalType}
                          <option value={nominalType}>{nominalType}</option>
                        {/each}
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Account</span>
                      <select class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" form="holding-create" name="accountID" required>
                        {#each data.accounts?.items ?? [] as account}
                          <option value={account.accountID}>{account.name}</option>
                        {/each}
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Instrument</span>
                      <select class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" form="holding-create" name="instrumentID" required>
                        {#each data.instruments?.items ?? [] as instrument}
                          <option value={instrument.instrumentID}>{instrument.name}</option>
                        {/each}
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Default</span>
                      <select class="h-8 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" form="holding-create" name="default">
                        <option value="false">No</option>
                        <option value="true">Yes</option>
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2"></td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Status</span>
                      <select class="h-8 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" form="holding-create" name="active">
                        <option value="true">Active</option>
                        <option value="false">Inactive</option>
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="holding-create">
                      <span>Event date</span>
                      <input class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form="holding-create" name="eventDateTime" required step="1" type="datetime-local" value={data.valuationDate} />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400" onclick={cancelAdd} type="button">Cancel</button>
                        <button class="h-8 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70" disabled={submittingCreate} form="holding-create" type="submit">{submittingCreate ? 'Adding' : 'Add'}</button>
                      </div>
                    </div>
                  </td>
                </tr>
              {/if}

              {#each sortedHoldings as holding}
                {#if editingHoldingID === holding.holdingID}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      <form id={`holding-edit-${holding.holdingID}`} action="?/modifyHolding" method="POST" use:enhance={enhanceHoldingEdit}>
                        <input name="holdingID" type="hidden" value={holding.holdingID} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Name</span>
                          <input class="h-8 w-40 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="name" type="text" value={form?.holdingID === holding.holdingID ? (form.values?.name ?? holding.name) : holding.name} />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2 text-slate-700">{holding.holdingType}</td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`holding-edit-${holding.holdingID}`}>
                        <span>Nominal</span>
                        <select class="h-8 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" form={`holding-edit-${holding.holdingID}`} name="nominalType">
                          <option value="">-</option>
                          {#each nominalTypes as nominalType}
                            <option selected={holding.nominalType === nominalType} value={nominalType}>{nominalType}</option>
                          {/each}
                        </select>
                      </label>
                    </td>
                    <td class="px-3 py-2 text-slate-700">{accountsByID.get(holding.accountID) ?? holding.accountID}</td>
                    <td class="px-3 py-2 text-slate-700">{instrumentsByID.get(holding.instrumentID) ?? holding.instrumentID}</td>
                    <td class="px-3 py-2">
                      <select class="h-8 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" form={`holding-edit-${holding.holdingID}`} name="default">
                        <option selected={!holding.default} value="false">No</option>
                        <option selected={holding.default} value="true">Yes</option>
                      </select>
                    </td>
                    <td class="px-3 py-2">{holding.includeInValuation ? 'Included' : 'Excluded'}</td>
                    <td class="px-3 py-2">{holding.active ? 'Active' : 'Inactive'}</td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`holding-edit-${holding.holdingID}`}>
                        <span>Event date</span>
                        <input class="h-8 w-44 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" form={`holding-edit-${holding.holdingID}`} name="eventDateTime" required step="1" type="datetime-local" value={data.valuationDate} />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="flex justify-end gap-2">
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400" onclick={cancelEdit} type="button">Cancel</button>
                        <button class="h-8 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70" disabled={submittingHoldingID === holding.holdingID} form={`holding-edit-${holding.holdingID}`} type="submit">{submittingHoldingID === holding.holdingID ? 'Saving' : 'Save'}</button>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="hover:bg-slate-50">
                    <td class="px-3 py-2 font-medium text-slate-950">{displayName(holding)}</td>
                    <td class="px-3 py-2 text-slate-700">{holding.holdingType}</td>
                    <td class="px-3 py-2 text-slate-700">{holding.nominalType ?? '-'}</td>
                    <td class="px-3 py-2 text-slate-700">{accountsByID.get(holding.accountID) ?? holding.accountID}</td>
                    <td class="px-3 py-2 text-slate-700">{instrumentsByID.get(holding.instrumentID) ?? holding.instrumentID}</td>
                    <td class="px-3 py-2">{holding.default ? 'Default' : '-'}</td>
                    <td class="px-3 py-2">{holding.includeInValuation ? 'Included' : 'Excluded'}</td>
                    <td class="px-3 py-2">
                      <span class={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${holding.active ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'}`}>{holding.active ? 'Active' : 'Inactive'}</span>
                    </td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(holding.lastAuditDateTime)}</td>
                    <td class="px-3 py-2">
                      <div class="flex justify-end gap-2">
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => toggleHistory(holding.holdingID)} type="button">{openHistoryHoldingID === holding.holdingID ? 'Hide' : 'History'}</button>
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => startEdit(holding.holdingID)} type="button">Edit</button>
                        <form action="?/modifyHoldingActive" method="POST" use:enhance={enhanceHoldingActive}>
                          <input name="holdingID" type="hidden" value={holding.holdingID} />
                          <input name="name" type="hidden" value={displayName(holding)} />
                          <input name="eventDateTime" type="hidden" value={data.valuationDate} />
                          <input name="active" type="hidden" value={holding.active ? 'false' : 'true'} />
                          <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700 disabled:cursor-wait disabled:opacity-70" disabled={submittingHoldingID === holding.holdingID} type="submit">{holding.active ? 'Deactivate' : 'Activate'}</button>
                        </form>
                      </div>
                    </td>
                  </tr>
                  {#if openHistoryHoldingID === holding.holdingID}
                    {@const history = historyByHoldingID[holding.holdingID]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="10">
                        <div class="grid gap-3 rounded-md border border-slate-200 bg-white p-3">
                          <div class="flex items-center justify-between gap-3">
                            <h2 class="text-sm font-semibold text-slate-950">{displayName(holding)} history</h2>
                            <span class="text-xs text-slate-500">{history?.events.length ?? 0} events</span>
                          </div>

                          {#if history?.loading}
                            <div class="text-sm text-slate-600">Loading history...</div>
                          {:else if history?.error}
                            <div class="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">{history.error}</div>
                          {:else if history?.events.length}
                            <ol class="grid gap-2">
                              {#each history.events as event}
                                <li class={`grid gap-2 rounded-md border px-3 py-2 md:grid-cols-[180px_1fr] ${event.applicationStatus === 'omitted' ? 'border-amber-200 bg-amber-50/70' : 'border-slate-200'}`}>
                                  <div class="text-xs text-slate-500">
                                    <div>{formatTableDateTime(event.eventDateTime)}</div>
                                    <div>Audit {formatTableDateTime(event.auditDateTime)}</div>
                                  </div>
                                  <div class="grid gap-1">
                                    <div class="flex flex-wrap items-center gap-2">
                                      <span class="font-medium text-slate-950">{event.$type}</span>
                                      {#if event.applicationStatus === 'omitted'}
                                        <span class="rounded-full border border-amber-300 bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-900">Not applied</span>
                                      {:else if data.auditDateTime}
                                        <span class="rounded-full border border-emerald-200 bg-emerald-50 px-2 py-0.5 text-xs font-semibold text-emerald-800">Applied</span>
                                      {/if}
                                      <span class="font-mono text-xs text-slate-500">{event.eventID}</span>
                                    </div>
                                    <div class="text-sm text-slate-700">{holdingEventSummary(event)}</div>
                                    <div class="text-xs text-slate-500">{event.reason}</div>
                                  </div>
                                </li>
                              {/each}
                            </ol>
                          {:else}
                            <div class="text-sm text-slate-600">No history found for this holding.</div>
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
