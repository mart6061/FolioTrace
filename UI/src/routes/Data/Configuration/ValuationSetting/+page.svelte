<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { ValuationSetting, ValuationSettingReferenceEvent } from '$lib/types';
  import type { SubmitFunction } from './$types';
  import ValuationNodeEditor from './ValuationNodeEditor.svelte';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  const allocationCount = $derived(data.valuationSettings?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.valuationSettings ? formatDisplayDateTime(data.valuationSettings.asOfDateTime) : 'now');

  type SortKey = 'name' | 'status' | 'accounts' | 'nodes' | 'lastAudit';
  type HistoryState = { events: ValuationSettingReferenceEvent[]; error: string; loading: boolean };

  let sortKey = $state<SortKey>('name');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let showAllConfigs = $state(false);
  let addingAllocation = $state(false);
  let editingAllocationID = $state('');
  let editingAccountsID = $state('');
  let submittingAllocationID = $state('');
  let submittingCreate = $state(false);
  let openHistoryID = $state('');
  let historyByID = $state<Record<string, HistoryState>>({});
  let editNodesJsonByID = $state<Record<string, string>>({});

  const accounts = $derived(data.accounts?.items ?? []);

  const filteredAllocations = $derived(
    (data.valuationSettings?.items ?? []).filter((allocation) => {
      if (!showAllConfigs && !allocation.active)
        return false;

      const filter = filterText.trim().toLocaleLowerCase();

      if (!filter)
        return true;

      return [
        allocation.name,
        allocation.assetAllocationID,
        allocation.active ? 'active' : 'inactive',
        ...allocation.accountIDs.map(accountName),
        ...allocation.nodes.map((node) => node.name)
      ].some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  const sortedAllocations = $derived(
    [...filteredAllocations].sort((left, right) => {
      const direction = sortDirection;

      switch (sortKey) {
        case 'status':
          return direction * Number(left.active !== right.active);
        case 'accounts':
          return direction * (left.accountIDs.length - right.accountIDs.length);
        case 'nodes':
          return direction * (visibleNodeCount(left) - visibleNodeCount(right));
        case 'lastAudit':
          return direction * (new Date(left.lastAuditDateTime).getTime() - new Date(right.lastAuditDateTime).getTime());
        case 'name':
        default:
          return direction * left.name.localeCompare(right.name);
      }
    })
  );

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

  function startAdd() {
    addingAllocation = true;
    editingAllocationID = '';
    editingAccountsID = '';
  }

  function cancelAdd() {
    addingAllocation = false;
  }

  function startEdit(allocation: ValuationSetting) {
    addingAllocation = false;
    editingAccountsID = '';
    editingAllocationID = allocation.assetAllocationID;
    editNodesJsonByID[allocation.assetAllocationID] = nodesJsonForEdit(allocation);
  }

  function cancelEdit() {
    delete editNodesJsonByID[editingAllocationID];
    editingAllocationID = '';
  }

  function startAccountEdit(assetAllocationID: string) {
    addingAllocation = false;
    editingAllocationID = '';
    editingAccountsID = assetAllocationID;
  }

  function cancelAccountEdit() {
    editingAccountsID = '';
  }

  const enhanceCreate: SubmitFunction = () => {
    submittingCreate = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;

      if (result.type === 'success')
        addingAllocation = false;
    };
  };

  const enhanceAllocation: SubmitFunction = ({ formData }) => {
    const assetAllocationID = formData.get('assetAllocationID');
    submittingAllocationID = typeof assetAllocationID === 'string' ? assetAllocationID : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingAllocationID = '';

      if (result.type === 'success') {
        if (typeof assetAllocationID === 'string')
          delete editNodesJsonByID[assetAllocationID];

        editingAllocationID = '';
        editingAccountsID = '';
      }
    };
  };

  async function toggleHistory(assetAllocationID: string) {
    if (openHistoryID === assetAllocationID) {
      openHistoryID = '';
      delete historyByID[assetAllocationID];
      return;
    }

    openHistoryID = assetAllocationID;

    if (historyByID[assetAllocationID])
      return;

    await loadHistory(assetAllocationID);
  }

  async function loadHistory(assetAllocationID: string) {
    historyByID[assetAllocationID] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Configuration/ValuationSetting/History', window.location.origin);
      historyUrl.searchParams.set('assetAllocationID', assetAllocationID);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByID[assetAllocationID] = {
        events: await response.json() as ValuationSettingReferenceEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByID[assetAllocationID] = {
        events: [],
        error: error instanceof Error ? error.message : 'Unable to load history.',
        loading: false
      };
    }
  }

  function accountName(accountID: string) {
    return accounts.find((account) => account.accountID === accountID)?.name ?? accountID;
  }

  function visibleNodeCount(allocation: ValuationSetting) {
    return allocation.nodes.filter((node) => !node.hidden).length;
  }

  function nodeSummary(allocation: ValuationSetting) {
    const visibleNodes = allocation.nodes.filter((node) => !node.hidden);
    if (!visibleNodes.length)
      return 'No visible nodes';

    return visibleNodes.map((node) => node.name).join(', ');
  }

  function nodesJson(allocation: ValuationSetting) {
    return JSON.stringify(allocation.nodes, null, 2);
  }

  function nodesJsonForEdit(allocation: ValuationSetting) {
    if (formStringValue('modifyAllocation', 'assetAllocationID') !== allocation.assetAllocationID)
      return nodesJson(allocation);

    return formStringValue('modifyAllocation', 'nodesJson', nodesJson(allocation));
  }

  function nodesJsonForExport(allocation: ValuationSetting) {
    return editNodesJsonByID[allocation.assetAllocationID] ?? nodesJsonForEdit(allocation);
  }

  function safeFileName(value: string) {
    const fileName = value
      .trim()
      .replace(/[^a-z0-9-_]+/gi, '-')
      .replace(/^-+|-+$/g, '')
      .toLowerCase();

    return fileName || 'valuation-setting';
  }

  function exportNodesJson(allocation: ValuationSetting) {
    const blob = new Blob([nodesJsonForExport(allocation)], {
      type: 'application/json;charset=utf-8'
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = `${safeFileName(allocation.name)}-nodes.json`;
    document.body.append(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 0);
  }

  function eventSummary(event: ValuationSettingReferenceEvent) {
    if (event.$type === 'AssetAllocationActiveSetEvent')
      return event.active ? 'Active' : 'Inactive';

    if (event.$type === 'AssetAllocationAccountIDsSetEvent')
      return `${event.accountIDs?.length ?? 0} accounts`;

    if (event.nodes)
      return `${event.name ?? ''} - ${event.nodes.length} nodes`;

    return event.name ?? event.assetAllocationID;
  }

  function formStringValue(intent: string, key: string, fallback = '') {
    if (form?.intent !== intent || !form.values || typeof form.values !== 'object')
      return fallback;

    const value = (form.values as Record<string, unknown>)[key];
    return typeof value === 'string' ? value : fallback;
  }

  function formStringValues(intent: string, key: string, fallback: string[] = []) {
    if (form?.intent !== intent || !form.values || typeof form.values !== 'object')
      return fallback;

    const value = (form.values as Record<string, unknown>)[key];
    return Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : fallback;
  }

  function checkedAccountIDsForEdit(assetAllocationID: string, fallback: string[]) {
    if (formStringValue('setAccounts', 'assetAllocationID') !== assetAllocationID)
      return fallback;

    return formStringValues('setAccounts', 'accountIDs', fallback);
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <p class="page-kicker">Configuration</p>
        <div class="page-title-row">
          <h1 class="page-title">Valuation Setting</h1>
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
          <input name="auditDateTime" type="hidden" value={data.auditDateTime} />
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
    {:else if data.valuationSettings}
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

      <AggregateUpdateWatcher aggregateKind="ValuationSettings" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.valuationSettings.lastEventID} />

      <div class="data-summary">
        <div>
          <span class="font-semibold text-slate-950">{allocationCount}</span>
          asset allocations
        </div>
        <div>
          Valuation {formatDisplayDateTime(data.valuationSettings.valuationDateTime)} - As-of {asOfSummary}
        </div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter asset allocations</span>
            <input bind:value={filterText} placeholder="Filter allocations..." type="search" />
          </label>

          <label class="valuation-show-all-toggle">
            <span>Show All</span>
            <span class="trace-toggle">
              <input bind:checked={showAllConfigs} type="checkbox" />
              <span aria-hidden="true"></span>
            </span>
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add asset allocation" onclick={startAdd} title="Add asset allocation" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
            </button>
          </div>
        </div>

        {#if addingAllocation}
          <form action="?/createAllocation" class="valuation-allocation-create-form mb-4 grid gap-4 rounded-md border border-teal-200 bg-teal-50/40 p-4" method="POST" use:enhance={enhanceCreate}>
            <div class="grid gap-4 md:grid-cols-2">
              <label class="grid gap-1 text-sm font-medium text-slate-700">
                Name
                <input class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="name" required type="text" value={formStringValue('createAllocation', 'name')} />
              </label>
              <label class="grid gap-1 text-sm font-medium text-slate-700">
                Initial asset node
                <input class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="initialNodeName" type="text" value={formStringValue('createAllocation', 'initialNodeName')} />
              </label>
            </div>

            <div class="valuation-allocation-create-layout">
              <fieldset class="valuation-account-checkbox-field">
                <legend>Accounts</legend>
                <div class="valuation-account-checkbox-list">
                  {#each accounts as account (account.accountID)}
                    <label>
                      <input checked={formStringValues('createAllocation', 'accountIDs').includes(account.accountID)} name="accountIDs" type="checkbox" value={account.accountID} />
                      <span>{account.name}</span>
                    </label>
                  {:else}
                    <p>No accounts available</p>
                  {/each}
                </div>
              </fieldset>

              <div class="valuation-allocation-create-side">
                <div class="valuation-event-active-row">
                  <label class="valuation-event-date-field grid gap-1 text-sm font-medium text-slate-700">
                    Event date
                    <DateTimeInput class="h-10 w-full rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="eventDateTime" required step="1" value={formStringValue('createAllocation', 'eventDateTime', eventDateDefault)} />
                  </label>

                  <label class="valuation-active-field">
                    <span>Active</span>
                    <span class="trace-toggle">
                      <input checked name="active" type="checkbox" value="true" />
                      <span aria-hidden="true"></span>
                    </span>
                  </label>
                </div>
              </div>
            </div>

            <div class="valuation-form-actions">
              <button class="h-9 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400" onclick={cancelAdd} type="button">Cancel</button>
              <button class="h-9 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70" disabled={submittingCreate} type="submit">{submittingCreate ? 'Adding' : 'Add'}</button>
            </div>
          </form>
        {/if}

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('name')} type="button">Name{sortLabel('name')}</button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('status')} type="button">Status{sortLabel('status')}</button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('accounts')} type="button">Accounts{sortLabel('accounts')}</button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('nodes')} type="button">Nodes{sortLabel('nodes')}</button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('lastAudit')} type="button">Last audit{sortLabel('lastAudit')}</button>
                </th>
                <th class="min-w-[30rem] px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each sortedAllocations as allocation (allocation.assetAllocationID)}
                <tr class="align-top hover:bg-slate-50">
                  <td class="px-3 py-3">
                    <div class="font-medium text-slate-950">{allocation.name}</div>
                  </td>
                  <td class="px-3 py-3">
                    <span class={`rounded-full border px-2 py-0.5 text-xs font-semibold ${allocation.active ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-slate-200 bg-slate-50 text-slate-600'}`}>
                      {allocation.active ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td class="px-3 py-3 text-slate-700">
                    {#if allocation.accountIDs.length}
                      {allocation.accountIDs.map(accountName).join(', ')}
                    {:else}
                      <span class="text-slate-500">No accounts</span>
                    {/if}
                  </td>
                  <td class="px-3 py-3 text-slate-700">
                    <div>{visibleNodeCount(allocation)} visible / {allocation.nodes.length} total</div>
                    <div class="text-xs text-slate-500">{nodeSummary(allocation)}</div>
                  </td>
                  <td class="px-3 py-3 text-slate-600">{formatTableDateTime(allocation.lastAuditDateTime)}</td>
                  <td class="px-3 py-3">
                    <div class="flex flex-nowrap justify-end gap-2 whitespace-nowrap">
                      <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => exportNodesJson(allocation)} title="Export nodes JSON" type="button">
                        Export
                      </button>
                      <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => toggleHistory(allocation.assetAllocationID)} type="button">
                        {openHistoryID === allocation.assetAllocationID ? 'Hide' : 'History'}
                      </button>
                      <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => startAccountEdit(allocation.assetAllocationID)} type="button">
                        Accounts
                      </button>
                      <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => startEdit(allocation)} type="button">
                        Edit
                      </button>
                      <form action="?/setActive" class="shrink-0" method="POST" use:enhance={enhanceAllocation}>
                        <input name="assetAllocationID" type="hidden" value={allocation.assetAllocationID} />
                        <input name="name" type="hidden" value={allocation.name} />
                        <input name="active" type="hidden" value={String(!allocation.active)} />
                        <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                        <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700 disabled:cursor-wait disabled:opacity-70" disabled={submittingAllocationID === allocation.assetAllocationID} type="submit">
                          {allocation.active ? 'Disable' : 'Enable'}
                        </button>
                      </form>
                    </div>
                  </td>
                </tr>

                {#if editingAccountsID === allocation.assetAllocationID}
                  <tr class="bg-teal-50/30">
                    <td class="px-3 py-3" colspan="6">
                      <form action="?/setAccounts" class="grid gap-4 md:grid-cols-[minmax(260px,1fr)_220px_auto] md:items-end" method="POST" use:enhance={enhanceAllocation}>
                        <input name="assetAllocationID" type="hidden" value={allocation.assetAllocationID} />
                        <input name="name" type="hidden" value={allocation.name} />
                        <fieldset class="valuation-account-checkbox-field">
                          <legend>Accounts</legend>
                          <div class="valuation-account-checkbox-list">
                            {#each accounts as account (account.accountID)}
                              <label>
                                <input checked={checkedAccountIDsForEdit(allocation.assetAllocationID, allocation.accountIDs).includes(account.accountID)} name="accountIDs" type="checkbox" value={account.accountID} />
                                <span>{account.name}</span>
                              </label>
                            {:else}
                              <p>No accounts available</p>
                            {/each}
                          </div>
                        </fieldset>
                        <label class="grid gap-1 text-sm font-medium text-slate-700">
                          Event date
                          <DateTimeInput class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="eventDateTime" required step="1" value={eventDateDefault} />
                        </label>
                        <div class="flex justify-end gap-2">
                          <button class="h-9 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400" onclick={cancelAccountEdit} type="button">Cancel</button>
                          <button class="h-9 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70" disabled={submittingAllocationID === allocation.assetAllocationID} type="submit">Save</button>
                        </div>
                      </form>
                    </td>
                  </tr>
                {/if}

                {#if editingAllocationID === allocation.assetAllocationID}
                  <tr class="bg-teal-50/30">
                    <td class="px-3 py-3" colspan="6">
											<form action="?/modifyAllocation" class="grid gap-4" method="POST" use:enhance={enhanceAllocation}>
												<input name="assetAllocationID" type="hidden" value={allocation.assetAllocationID} />
												<input name="rootNodeID" type="hidden" value={allocation.rootNodeID} />
												<div class="valuation-allocation-edit-fields">
													<label class="valuation-allocation-name-field grid gap-1 text-sm font-medium text-slate-700">
														Name
														<input class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="name" required type="text" value={allocation.name} />
													</label>
													<label class="valuation-allocation-edit-date-field grid gap-1 text-sm font-medium text-slate-700">
														Event date
														<DateTimeInput class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="eventDateTime" required step="1" value={eventDateDefault} />
													</label>
												</div>
												{#key allocation.assetAllocationID}
													<ValuationNodeEditor accounts={accounts} allocationAccountIDs={allocation.accountIDs} bind:nodesJson={editNodesJsonByID[allocation.assetAllocationID]} rootNodeID={allocation.rootNodeID} rootNodeName={allocation.name} />
												{/key}
												<div class="flex justify-end gap-2">
                          <button class="h-9 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-slate-400" onclick={cancelEdit} type="button">Cancel</button>
                          <button class="h-9 rounded-md bg-teal-700 px-3 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-wait disabled:opacity-70" disabled={submittingAllocationID === allocation.assetAllocationID} type="submit">Save</button>
                        </div>
                      </form>
                    </td>
                  </tr>
                {/if}

                {#if openHistoryID === allocation.assetAllocationID}
                  {@const history = historyByID[allocation.assetAllocationID]}
                  <tr class="bg-slate-50/80">
                    <td class="px-3 py-3" colspan="6">
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
                            emptyMessage="No history found for this allocation."
                          />
                        {/if}
                      </div>
                    </td>
                  </tr>
                {/if}
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </section>
</main>

<style>
  .valuation-allocation-create-layout {
    display: grid;
    gap: 1rem;
  }

  .valuation-allocation-create-side {
    display: grid;
    gap: 1rem;
    align-content: start;
    min-width: 0;
  }

  .valuation-event-active-row {
    display: grid;
    gap: 0.75rem;
    align-items: end;
  }

  .valuation-allocation-edit-fields {
    display: grid;
    gap: 1rem;
    align-items: end;
  }

  .valuation-allocation-name-field {
    width: 100%;
    max-width: 360px;
  }

  .valuation-allocation-edit-date-field {
    width: 100%;
    max-width: 280px;
  }

  .valuation-event-date-field {
    min-width: 0;
    width: 100%;
    max-width: 280px;
  }

  .valuation-event-date-field :global(.datetime-input-control) {
    width: 100%;
    grid-template-columns: minmax(0, 1fr) auto;
  }

  .valuation-account-checkbox-field {
    display: grid;
    gap: 0.25rem;
    min-width: 0;
    border: 0;
    margin: 0;
    padding: 0;
    color: rgb(51 65 85);
    font-size: 0.875rem;
    font-weight: 500;
  }

  .valuation-account-checkbox-field legend {
    padding: 0;
    color: rgb(51 65 85);
    font-size: 0.875rem;
    font-weight: 500;
  }

  .valuation-account-checkbox-list {
    display: grid;
    max-height: 10.5rem;
    overflow: auto;
    border: 1px solid rgb(203 213 225);
    border-radius: 0.375rem;
    background: white;
    padding: 0.35rem;
  }

  .valuation-account-checkbox-list label {
    display: flex;
    min-width: 0;
    align-items: center;
    gap: 0.5rem;
    border-radius: 0.25rem;
    cursor: pointer;
    padding: 0.35rem 0.45rem;
    color: rgb(15 23 42);
    font-size: 0.875rem;
    font-weight: 500;
  }

  .valuation-account-checkbox-list label:hover {
    background: rgb(240 253 250);
  }

  .valuation-account-checkbox-list input {
    flex: 0 0 auto;
    width: 1rem;
    height: 1rem;
    accent-color: var(--accent);
  }

  .valuation-account-checkbox-list span {
    overflow-wrap: anywhere;
  }

  .valuation-account-checkbox-list p {
    margin: 0;
    padding: 0.35rem 0.45rem;
    color: rgb(100 116 139);
    font-size: 0.875rem;
  }

  :global(.dark) .valuation-account-checkbox-field,
  :global(.dark) .valuation-account-checkbox-field legend {
    color: var(--muted);
  }

  :global(.dark) .valuation-account-checkbox-list {
    border-color: var(--line);
    background: var(--bg-muted);
  }

  :global(.dark) .valuation-account-checkbox-list label {
    color: var(--ink);
  }

  :global(.dark) .valuation-account-checkbox-list label:hover {
    background: var(--accent-soft);
  }

  :global(.dark) .valuation-account-checkbox-list p {
    color: var(--muted);
  }

  .valuation-active-field {
    display: inline-flex;
    min-height: 2.5rem;
    align-items: center;
    gap: 0.5rem;
    justify-self: start;
    color: var(--muted);
    font-size: 0.875rem;
    font-weight: 700;
    white-space: nowrap;
  }

  .valuation-form-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    justify-content: flex-end;
  }

  .valuation-show-all-toggle {
    display: inline-flex;
    min-height: 2.5rem;
    align-items: center;
    gap: 0.5rem;
    color: var(--muted);
    font-size: 0.875rem;
    font-weight: 700;
    white-space: nowrap;
  }

  @media (min-width: 768px) {
    .valuation-allocation-edit-fields {
      grid-template-columns: minmax(180px, 360px) minmax(220px, 280px);
    }

    .valuation-event-active-row {
      grid-template-columns: minmax(220px, 280px) auto;
    }

    .valuation-active-field {
      justify-self: end;
    }
  }

  @media (min-width: 1024px) {
    .valuation-allocation-create-layout {
      grid-template-columns: minmax(16rem, 0.9fr) minmax(24rem, 1fr);
      align-items: start;
    }
  }
</style>
