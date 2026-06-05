<script lang="ts">
  import type { EventPropertyDetail } from '$lib/types';

  let { details = [] }: { details?: EventPropertyDetail[] } = $props();

  const commonPropertyNames = new Set(['Type', 'EventID', 'UserID', 'EventDateTime', 'AuditDateTime', 'Reason']);

  const orderedDetails = $derived.by(() =>
    (details ?? [])
      .map((detail, index) => ({ detail, index }))
      .sort((left, right) =>
        (left.detail.order ?? Number.MAX_SAFE_INTEGER) - (right.detail.order ?? Number.MAX_SAFE_INTEGER) ||
        left.index - right.index
      )
      .map((item) => item.detail)
  );

  const commonDetails = $derived(orderedDetails.filter((detail) => commonPropertyNames.has(detail.name)));
  const typeDetails = $derived(orderedDetails.filter((detail) => !commonPropertyNames.has(detail.name)));
  const eventType = $derived(formatValue(detailValue('Type')));
  const isTransactionCancellation = $derived(eventType === 'TransactionCancellationEvent');
  const cancelledEventID = $derived(formatValue(detailValue('CancelledEventID')));
  const eventSetID = $derived(formatValue(detailValue('EventSetID')));
  const auditDateTime = $derived(formatValue(detailValue('AuditDateTime')));
  const cancelledIDGroup = $derived(toValueList(detailValue('CancelledIDGroup')));

  function detailValue(name: string): unknown {
    return orderedDetails.find((detail) => detail.name === name)?.value;
  }

  function formatValue(value: unknown): string {
    if (value === null || value === undefined)
      return '-';

    if (typeof value === 'string')
      return value.trim().length ? value : '-';

    if (typeof value === 'number' || typeof value === 'boolean')
      return String(value);

    if (Array.isArray(value))
      return value.length ? value.map(formatValue).join(', ') : '-';

    if (value && typeof value === 'object') {
      const record = value as Record<string, unknown>;
      const primitive = record.value ?? record.Value ?? record.amount ?? record.Amount;

      if (primitive !== undefined && primitive !== value)
        return formatValue(primitive);

      return JSON.stringify(value);
    }

    return '-';
  }

  function toValueList(value: unknown): string[] {
    if (!Array.isArray(value))
      return [];

    return value
      .map(formatValue)
      .filter((item) => item !== '-');
  }
</script>

{#if orderedDetails.length}
  <div class="grid gap-2">
    {#if isTransactionCancellation}
      <div class="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-900">
        <div class="flex flex-wrap items-center gap-2">
          <span class="rounded-full bg-red-100 px-2 py-0.5 font-semibold text-red-700">Cancelled</span>
          <span class="font-semibold">Transaction cancellation</span>
        </div>
        <div class="mt-1">
          Cancelled {cancelledEventID === '-' ? 'transaction' : cancelledEventID} from set {eventSetID}
        </div>
        {#if cancelledIDGroup.length}
          <div class="mt-1 break-all font-mono text-[0.7rem] text-red-700">
            Cancelled group {cancelledIDGroup.join(', ')}
          </div>
        {/if}
        {#if auditDateTime !== '-'}
          <div class="mt-1 font-medium text-red-700">Cancelled on {auditDateTime}</div>
        {/if}
      </div>
    {/if}

    {#if commonDetails.length}
      <dl class="grid gap-1.5 text-xs sm:grid-cols-2 lg:grid-cols-3">
        {#each commonDetails as detail, index (`${detail.name}-${index}`)}
          <div class="grid min-w-0 gap-0.5 rounded-md border border-slate-100 bg-white px-2 py-1.5">
            <dt class="text-[0.68rem] font-light uppercase tracking-wide text-slate-400">{detail.description}</dt>
            <dd class="min-w-0 break-words font-medium text-slate-900">{formatValue(detail.value)}</dd>
          </div>
        {/each}
      </dl>
    {/if}

    {#if typeDetails.length}
      <dl class="grid gap-1.5 text-xs sm:grid-cols-2">
        {#each typeDetails as detail, index (`${detail.name}-${index}`)}
          <div class="grid min-w-0 gap-0.5 rounded-md bg-slate-50 px-2 py-1.5">
            <dt class="text-[0.68rem] font-light uppercase tracking-wide text-slate-400">{detail.description}</dt>
            <dd class="min-w-0 break-words font-semibold text-slate-900">{formatValue(detail.value)}</dd>
          </div>
        {/each}
      </dl>
    {/if}
  </div>
{/if}
