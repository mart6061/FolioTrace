<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import EventPropertyDetails from '$lib/components/EventPropertyDetails.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { ValuationSetting, ValuationSettingReferenceEvent } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  const allocationCount = $derived(data.valuationSettings?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.valuationSettings ? formatDisplayDateTime(data.valuationSettings.asOfDateTime) : 'now');

  type SortKey = 'name' | 'status' | 'accounts' | 'nodes' | 'lastAudit';
  type HistoryState = { events: ValuationSettingReferenceEvent[]; error: string; loading: boolean };

  let sortKey = $state<SortKey>('name');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let addingAllocation = $state(false);
  let editingAllocationID = $state('');
  let editingAccountsID = $state('');
  let submittingAllocationID = $state('');
  let submittingCreate = $state(false);
  let openHistoryID = $state('');
  let historyByID = $state<Record<string, HistoryState>>({});

  const accounts = $derived(data.accounts?.items ?? []);

  const filteredAllocations = $derived(
    (data.valuationSettings?.items ?? []).filter((allocation) => {
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

  function startEdit(assetAllocationID: string) {
    addingAllocation = false;
    editingAccountsID = '';
    editingAllocationID = assetAllocationID;
  }

  function cancelEdit() {
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
      const historyUrl = new URL('/Data/Reference/ValuationSetting/History', window.location.origin);
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
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <p class="page-kicker">Reference Data</p>
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

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add asset allocation" onclick={startAdd} title="Add asset allocation" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
            </button>
          </div>
        </div>

        {#if addingAllocation}
          <form action="?/createAllocation" class="mb-4 grid gap-4 rounded-md border border-teal-200 bg-teal-50/40 p-4" method="POST" use:enhance={enhanceCreate}>
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

            <div class="grid gap-4 md:grid-cols-[minmax(220px,1fr)_minmax(220px,1fr)_auto] md:items-end">
              <label class="grid gap-1 text-sm font-medium text-slate-700">
                Accounts
                <select class="min-h-28 rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" multiple name="accountIDs">
                  {#each accounts as account (account.accountID)}
                    <option value={account.accountID}>{account.name}</option>
                  {/each}
                </select>
              </label>
              <label class="grid gap-1 text-sm font-medium text-slate-700">
                Event date
                <DateTimeInput class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="eventDateTime" required step="1" value={formStringValue('createAllocation', 'eventDateTime', eventDateDefault)} />
              </label>
              <label class="trace-toggle mb-1">
                <input checked name="active" type="checkbox" value="true" />
                <span>Active</span>
              </label>
            </div>

            <div class="flex justify-end gap-2">
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
                <th class="w-64 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each sortedAllocations as allocation (allocation.assetAllocationID)}
                <tr class="align-top hover:bg-slate-50">
                  <td class="px-3 py-3">
                    <div class="font-medium text-slate-950">{allocation.name}</div>
                    <div class="font-mono text-xs text-slate-500">{allocation.assetAllocationID}</div>
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
                    <div class="flex flex-wrap justify-end gap-2">
                      <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => toggleHistory(allocation.assetAllocationID)} type="button">
                        {openHistoryID === allocation.assetAllocationID ? 'Hide' : 'History'}
                      </button>
                      <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => startAccountEdit(allocation.assetAllocationID)} type="button">
                        Accounts
                      </button>
                      <button class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700" onclick={() => startEdit(allocation.assetAllocationID)} type="button">
                        Edit
                      </button>
                      <form action="?/setActive" method="POST" use:enhance={enhanceAllocation}>
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
                        <label class="grid gap-1 text-sm font-medium text-slate-700">
                          Accounts
                          <select class="min-h-28 rounded-md border border-slate-300 bg-white px-3 py-2 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" multiple name="accountIDs">
                            {#each accounts as account (account.accountID)}
                              <option selected={allocation.accountIDs.includes(account.accountID)} value={account.accountID}>{account.name}</option>
                            {/each}
                          </select>
                        </label>
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
                        <div class="grid gap-4 md:grid-cols-2">
                          <label class="grid gap-1 text-sm font-medium text-slate-700">
                            Name
                            <input class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="name" required type="text" value={allocation.name} />
                          </label>
                          <label class="grid gap-1 text-sm font-medium text-slate-700">
                            Root node
                            <input class="h-10 rounded-md border border-slate-300 bg-white px-3 font-mono text-sm text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="rootNodeID" required type="text" value={allocation.rootNodeID} />
                          </label>
                        </div>
                        <div class="grid gap-4 md:grid-cols-[1fr_220px]">
                          <label class="grid gap-1 text-sm font-medium text-slate-700">
                            Nodes JSON
                            <textarea class="min-h-72 rounded-md border border-slate-300 bg-white px-3 py-2 font-mono text-xs text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="nodesJson" required spellcheck="false">{nodesJson(allocation)}</textarea>
                          </label>
                          <label class="grid content-start gap-1 text-sm font-medium text-slate-700">
                            Event date
                            <DateTimeInput class="h-10 rounded-md border border-slate-300 bg-white px-3 text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="eventDateTime" required step="1" value={eventDateDefault} />
                          </label>
                        </div>
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
                      <div class="grid gap-3 rounded-md border border-slate-200 bg-white p-3">
                        <div class="flex items-center justify-between gap-3">
                          <h2 class="text-sm font-semibold text-slate-950">{allocation.name} history</h2>
                          <span class="text-xs text-slate-500">{history?.events.length ?? 0} events</span>
                        </div>

                        {#if history?.loading}
                          <div class="text-sm text-slate-600">Loading history...</div>
                        {:else if history?.error}
                          <div class="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">{history.error}</div>
                        {:else if history?.events.length}
                          <ol class="grid gap-2">
                            {#each history.events as event (event.eventID)}
                              <li class={`grid gap-2 rounded-md border px-3 py-2 md:grid-cols-[180px_1fr] ${
                                event.applicationStatus === 'omitted'
                                  ? 'border-amber-200 bg-amber-50/70'
                                  : 'border-slate-200'
                              }`}>
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
                                  <div class="text-sm text-slate-700">{eventSummary(event)}</div>
                                  <EventPropertyDetails details={event.propertyDetails} />
                                  <div class="text-xs text-slate-500">{event.reason}</div>
                                </div>
                              </li>
                            {/each}
                          </ol>
                        {:else}
                          <div class="text-sm text-slate-600">No history found for this allocation.</div>
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
