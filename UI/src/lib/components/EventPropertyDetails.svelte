<script lang="ts">
  import type { EventPropertyDetail } from '$lib/types';

  let { details = [], excludeNames = [] }: { details?: EventPropertyDetail[]; excludeNames?: string[] } = $props();

  const commonPropertyNames = new Set(['Type', 'EventID', 'UserID', 'EventDateTime', 'AuditDateTime', 'Reason']);
  const duplicatePropertyNames = new Set(['Type', 'Reason']);

  const rawOrderedDetails = $derived.by(() =>
    (details ?? [])
      .map((detail, index) => ({ detail, index }))
      .sort((left, right) =>
        (left.detail.order ?? Number.MAX_SAFE_INTEGER) - (right.detail.order ?? Number.MAX_SAFE_INTEGER) ||
        left.index - right.index
      )
      .map((item) => item.detail)
  );
  const excludedPropertyNames = $derived(new Set([...duplicatePropertyNames, ...excludeNames]));
  const orderedDetails = $derived(rawOrderedDetails.filter((detail) => !excludedPropertyNames.has(detail.name)));

  const commonDetails = $derived(orderedDetails.filter((detail) => commonPropertyNames.has(detail.name)));
  const typeDetails = $derived(orderedDetails.filter((detail) => !commonPropertyNames.has(detail.name)));
  const eventType = $derived(formatValue(detailValue('Type')));
  const isTransactionCancellation = $derived(eventType === 'TransactionCancellationEvent');
  const cancelledEventID = $derived(formatValue(detailValue('CancelledEventID')));
  const eventSetID = $derived(formatValue(detailValue('EventSetID')));
  const auditDateTime = $derived(formatValue(detailValue('AuditDateTime')));
  const cancelledIDGroup = $derived(toValueList(detailValue('CancelledIDGroup')));
  const hasRenderedDetails = $derived(orderedDetails.length || isTransactionCancellation);

  function detailValue(name: string): unknown {
    return rawOrderedDetails.find((detail) => detail.name === name)?.value;
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

{#if hasRenderedDetails}
  <div class="grid gap-1">
    {#if isTransactionCancellation}
      <div class="rounded-md border border-red-200 bg-red-50 px-2 py-1 text-xs text-red-900">
        <div class="flex flex-wrap items-center gap-1.5">
          <span class="rounded-full bg-red-100 px-1.5 py-0.5 text-[0.68rem] font-semibold text-red-700">Cancelled</span>
          <span class="font-semibold">Transaction cancellation</span>
          <span>Cancelled {cancelledEventID === '-' ? 'transaction' : cancelledEventID} from set {eventSetID}</span>
        </div>
        {#if cancelledIDGroup.length}
          <div class="mt-0.5 break-all font-mono text-[0.68rem] text-red-700">
            Cancelled group {cancelledIDGroup.join(', ')}
          </div>
        {/if}
        {#if auditDateTime !== '-'}
          <div class="mt-0.5 font-medium text-red-700">Cancelled on {auditDateTime}</div>
        {/if}
      </div>
    {/if}

    {#if commonDetails.length}
      <dl class="flex flex-wrap gap-x-3 gap-y-1 text-xs">
        {#each commonDetails as detail, index (`${detail.name}-${index}`)}
          <div class="flex min-w-0 items-baseline gap-1">
            <dt class="shrink-0 text-[0.68rem] font-light uppercase tracking-wide text-slate-400">{detail.description}</dt>
            <dd class="min-w-0 break-words font-medium text-slate-900">{formatValue(detail.value)}</dd>
          </div>
        {/each}
      </dl>
    {/if}

    {#if typeDetails.length}
      <dl class="flex flex-wrap gap-x-3 gap-y-1 text-xs">
        {#each typeDetails as detail, index (`${detail.name}-${index}`)}
          <div class="flex min-w-0 items-baseline gap-1">
            <dt class="shrink-0 text-[0.68rem] font-light uppercase tracking-wide text-slate-400">{detail.description}</dt>
            <dd class="min-w-0 break-words font-semibold text-slate-900">{formatValue(detail.value)}</dd>
          </div>
        {/each}
      </dl>
    {/if}
  </div>
{/if}
