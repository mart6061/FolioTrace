<script lang="ts">
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { formatDateTime } from '$lib/dates';
  import type { FIXOperation } from '$lib/types';

  let { data } = $props();

  type FIXField = {
    tag: string;
    name: string;
    value: string;
  };

  let expandedId = $state('');

  const operations = $derived(data.operations?.items ?? []);
  const totalCount = $derived(data.operations?.totalCount ?? 0);
  const page = $derived(data.operations?.page ?? 1);
  const pageSize = $derived(data.operations?.pageSize ?? 50);
  const totalPages = $derived(Math.max(1, Math.ceil(totalCount / pageSize)));
  const tagNames: Record<string, string> = {
    '8': 'BeginString',
    '9': 'BodyLength',
    '10': 'CheckSum',
    '11': 'ClOrdID',
    '14': 'CumQty',
    '17': 'ExecID',
    '31': 'LastPx',
    '32': 'LastQty',
    '34': 'MsgSeqNum',
    '35': 'MsgType',
    '37': 'OrderID',
    '38': 'OrderQty',
    '39': 'OrdStatus',
    '40': 'OrdType',
    '44': 'Price',
    '49': 'SenderCompID',
    '52': 'SendingTime',
    '54': 'Side',
    '55': 'Symbol',
    '56': 'TargetCompID',
    '59': 'TimeInForce',
    '60': 'TransactTime',
    '150': 'ExecType',
    '151': 'LeavesQty'
  };

  function toggleExpanded(id: string) {
    expandedId = expandedId === id ? '' : id;
  }

  function directionClass(direction: string) {
    switch (direction) {
      case 'Inbound':
        return 'bg-blue-100 text-blue-800';
      case 'Outbound':
        return 'bg-emerald-100 text-emerald-800';
      default:
        return 'bg-slate-100 text-slate-800';
    }
  }

  function channelClass(channel: string) {
    switch (channel) {
      case 'Admin':
        return 'bg-amber-100 text-amber-800';
      case 'App':
        return 'bg-violet-100 text-violet-800';
      default:
        return 'bg-slate-100 text-slate-800';
    }
  }

  function formatOptionalDateTime(value: string | null) {
    return value ? formatDateTime(value) : '-';
  }

  function pageHref(nextPage: number) {
    const params = new URLSearchParams();

    for (const [key, value] of Object.entries(data.filters)) {
      if (value && key !== 'page')
        params.set(key, value);
    }

    params.set('page', String(nextPage));

    return `/Diagnostics/FIXTrace?${params.toString()}`;
  }

  function orderReference(operation: FIXOperation) {
    if (operation.clOrdID && operation.execID)
      return `${operation.clOrdID} / ${operation.execID}`;

    return operation.clOrdID || operation.execID || '-';
  }

  function counterparty(operation: FIXOperation) {
    if (operation.senderCompID && operation.targetCompID)
      return `${operation.senderCompID} -> ${operation.targetCompID}`;

    return operation.senderCompID || operation.targetCompID || '-';
  }

  function operationFields(operation: FIXOperation): FIXField[] {
    return operation.displayMessage
      .split('|')
      .map((field) => field.trim())
      .filter(Boolean)
      .map((field) => {
        const separatorIndex = field.indexOf('=');
        const tag = separatorIndex >= 0 ? field.slice(0, separatorIndex) : field;
        const value = separatorIndex >= 0 ? field.slice(separatorIndex + 1) : '';

        return {
          tag,
          name: tagNames[tag] ?? '',
          value
        };
      });
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-main">
        <p class="page-kicker">Diagnostics</p>
        <div class="page-title-row">
          <h1 class="page-title">FIX Trace</h1>
          <BookmarkButton />
        </div>
      </div>

      <form class="house-form fix-trace-filter-form">
        <label class="fix-trace-filter-field fix-trace-filter-select">
          Direction
          <select class="house-control house-control-md house-control-full" name="direction">
            <option value="">All</option>
            {#each ['Inbound', 'Outbound'] as direction (direction)}
              <option selected={data.filters.direction === direction} value={direction}>{direction}</option>
            {/each}
          </select>
        </label>

        <label class="fix-trace-filter-field fix-trace-filter-select">
          Channel
          <select class="house-control house-control-md house-control-full" name="channel">
            <option value="">All</option>
            {#each ['Admin', 'App'] as channel (channel)}
              <option selected={data.filters.channel === channel} value={channel}>{channel}</option>
            {/each}
          </select>
        </label>

        <label class="fix-trace-filter-field fix-trace-filter-msg">
          Msg
          <input
            class="house-control house-control-md house-control-full"
            name="msgType"
            placeholder="D"
            type="search"
            value={data.filters.msgType}
          />
        </label>

        <label class="fix-trace-filter-field fix-trace-filter-id">
          ClOrdID
          <input
            class="house-control house-control-md house-control-full"
            name="clOrdID"
            type="search"
            value={data.filters.clOrdID}
          />
        </label>

        <label class="fix-trace-filter-field fix-trace-filter-id">
          ExecID
          <input
            class="house-control house-control-md house-control-full"
            name="execID"
            type="search"
            value={data.filters.execID}
          />
        </label>

        <span class="fix-trace-filter-break" aria-hidden="true"></span>

        <label class="fix-trace-filter-field fix-trace-filter-date">
          From UTC
          <DateTimeInput
            fullWidth
            name="fromUtc"
            step="1"
            value={data.filters.fromUtc}
          />
        </label>

        <label class="fix-trace-filter-field fix-trace-filter-date">
          To UTC
          <DateTimeInput
            fullWidth
            name="toUtc"
            step="1"
            value={data.filters.toUtc}
          />
        </label>

        <label class="fix-trace-filter-field fix-trace-filter-search">
          Search
          <input
            class="house-control house-control-md house-control-full"
            name="text"
            type="search"
            value={data.filters.text}
          />
        </label>

        <button
          class="house-button house-button-primary house-button-md fix-trace-filter-apply"
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
    {:else}
      <div class="data-summary">
        <div>
          <span class="font-semibold text-slate-950">{totalCount}</span>
          operations
        </div>
        <div>Page {page} of {totalPages}</div>
      </div>

      <div class="data-panel">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">Recorded</th>
                <th class="px-3 py-2">Direction</th>
                <th class="px-3 py-2">Channel</th>
                <th class="px-3 py-2">Msg</th>
                <th class="px-3 py-2 text-right">Seq</th>
                <th class="px-3 py-2">Order / Exec</th>
                <th class="px-3 py-2">Counterparty</th>
                <th class="sticky right-0 z-10 w-24 bg-slate-50 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each operations as operation (operation.eventID)}
                <tr class="align-top hover:bg-slate-50">
                  <td class="whitespace-nowrap px-3 py-2 text-slate-600">{formatDateTime(operation.recordedAtUtc)}</td>
                  <td class="px-3 py-2">
                    <span class={`rounded px-2 py-1 text-xs font-semibold ${directionClass(operation.direction)}`}>
                      {operation.direction || '-'}
                    </span>
                  </td>
                  <td class="px-3 py-2">
                    <span class={`rounded px-2 py-1 text-xs font-semibold ${channelClass(operation.channel)}`}>
                      {operation.channel || '-'}
                    </span>
                  </td>
                  <td class="px-3 py-2">
                    <div class="font-mono text-xs font-semibold text-slate-900">{operation.msgType || '-'}</div>
                    <div class="whitespace-nowrap text-xs text-slate-500">{operation.messageName}</div>
                  </td>
                  <td class="px-3 py-2 text-right font-mono text-slate-700">{operation.msgSeqNum ?? '-'}</td>
                  <td class="max-w-64 px-3 py-2 font-mono text-xs text-slate-700">
                    <div class="truncate">{orderReference(operation)}</div>
                  </td>
                  <td class="max-w-64 px-3 py-2 font-mono text-xs text-slate-700">
                    <div class="truncate">{counterparty(operation)}</div>
                  </td>
                  <td class="sticky right-0 bg-white px-3 py-2 text-right shadow-[-8px_0_12px_-12px_rgb(15_23_42_/_0.45)]">
                    <button
                      class="house-button house-button-secondary house-button-sm"
                      onclick={() => toggleExpanded(operation.eventID)}
                      type="button"
                    >
                      {expandedId === operation.eventID ? 'Close' : 'View'}
                    </button>
                  </td>
                </tr>

                {#if expandedId === operation.eventID}
                  <tr>
                    <td class="bg-slate-50 px-3 py-3" colspan="8">
                      {@render OperationDetail(operation)}
                    </td>
                  </tr>
                {/if}
              {:else}
                <tr>
                  <td class="px-3 py-6 text-center text-sm text-slate-500" colspan="8">
                    No FIX operations found.
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>

        {#if totalPages > 1}
          <div class="flex items-center justify-between border-t border-slate-200 px-3 py-2 text-sm">
            <a
              class={`rounded-md border px-3 py-1.5 ${page <= 1 ? 'pointer-events-none border-slate-200 text-slate-400' : 'border-slate-300 text-slate-700 hover:border-teal-600 hover:text-teal-700'}`}
              href={pageHref(Math.max(1, page - 1))}
            >
              Previous
            </a>
            <span class="text-slate-600">{page} / {totalPages}</span>
            <a
              class={`rounded-md border px-3 py-1.5 ${page >= totalPages ? 'pointer-events-none border-slate-200 text-slate-400' : 'border-slate-300 text-slate-700 hover:border-teal-600 hover:text-teal-700'}`}
              href={pageHref(Math.min(totalPages, page + 1))}
            >
              Next
            </a>
          </div>
        {/if}
      </div>
    {/if}
  </section>
</main>

<style>
  .fix-trace-filter-form {
    display: flex;
    width: 100%;
    flex-wrap: wrap;
    align-items: end;
    gap: 0.75rem;
  }

  .fix-trace-filter-field {
    display: grid;
    flex: 1 1 12rem;
    min-width: 0;
    gap: 0.25rem;
    color: var(--muted);
    font-size: var(--house-label-size);
    font-weight: 700;
  }

  .fix-trace-filter-select {
    flex-basis: 7.5rem;
    flex-grow: 0;
  }

  .fix-trace-filter-msg {
    flex-basis: 5.75rem;
    flex-grow: 0;
  }

  .fix-trace-filter-id {
    flex-basis: 10rem;
  }

  .fix-trace-filter-date {
    flex-basis: var(--house-datetime-width);
    flex-grow: 0;
  }

  .fix-trace-filter-search {
    flex-basis: 12rem;
  }

  .fix-trace-filter-apply {
    flex: 0 0 auto;
  }

  .fix-trace-filter-break {
    display: none;
  }

  @media (min-width: 1024px) and (max-width: 1535px) {
    .fix-trace-filter-break {
      display: block;
      flex: 0 0 100%;
      height: 0;
    }
  }

  @media (max-width: 640px) {
    .fix-trace-filter-field,
    .fix-trace-filter-apply {
      flex-basis: 100%;
      width: 100%;
    }
  }
</style>

{#snippet OperationDetail(operation: FIXOperation)}
  <div class="grid gap-3">
    <dl class="grid gap-2 text-xs text-slate-600 sm:grid-cols-2 lg:grid-cols-4">
      <div>
        <dt class="font-medium text-slate-900">Session</dt>
        <dd class="break-all font-mono">{operation.sessionID || '-'}</dd>
      </div>
      <div>
        <dt class="font-medium text-slate-900">Sending Time</dt>
        <dd>{formatOptionalDateTime(operation.sendingTime)}</dd>
      </div>
      <div>
        <dt class="font-medium text-slate-900">Event Time</dt>
        <dd>{formatDateTime(operation.eventDateTime)}</dd>
      </div>
      <div>
        <dt class="font-medium text-slate-900">Audit Time</dt>
        <dd>{formatDateTime(operation.auditDateTime)}</dd>
      </div>
      <div class="sm:col-span-2 lg:col-span-4">
        <dt class="font-medium text-slate-900">Reason</dt>
        <dd>{operation.reason || '-'}</dd>
      </div>
    </dl>

    <section>
      <h2 class="text-sm font-semibold text-slate-950">Message</h2>
      <pre class="mt-2 max-h-64 overflow-auto whitespace-pre-wrap break-words rounded-md border border-slate-200 bg-white p-3 font-mono text-xs text-slate-800">{operation.displayMessage || operation.rawMessage || '-'}</pre>
    </section>

    <section>
      <h2 class="text-sm font-semibold text-slate-950">Fields</h2>
      <div class="mt-2 overflow-x-auto rounded-md border border-slate-200 bg-white">
        <table class="min-w-full divide-y divide-slate-200 text-xs">
          <thead class="bg-slate-50 text-left font-semibold uppercase tracking-wide text-slate-600">
            <tr>
              <th class="w-20 px-3 py-2">Tag</th>
              <th class="w-40 px-3 py-2">Name</th>
              <th class="px-3 py-2">Value</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            {#each operationFields(operation) as field, fieldIndex (`${field.tag}-${fieldIndex}`)}
              <tr>
                <td class="px-3 py-2 font-mono text-slate-700">{field.tag}</td>
                <td class="px-3 py-2 text-slate-600">{field.name || '-'}</td>
                <td class="break-all px-3 py-2 font-mono text-slate-800">{field.value || '-'}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </section>
  </div>
{/snippet}
