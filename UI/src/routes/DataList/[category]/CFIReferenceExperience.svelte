<script lang="ts">
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import { formatDisplayDateTime, formatTableDateTime } from '$lib/dates';
  import type { Instrument } from '$lib/types';

  let { data, renderMode = 'full' } = $props();

  type CfiRow = {
    code: string;
    category: string;
    examples: string[];
    instrumentCount: number;
    lastAuditDateTime: string;
  };

  const showFilter = $derived(renderMode !== 'body');
  const showBody = $derived(renderMode !== 'filter');
  const asOfSummary = $derived(data.auditDateTime && data.instruments ? formatDisplayDateTime(data.instruments.asOfDateTime) : 'now');
  const cfiRows = $derived(createCfiRows(data.instruments?.items ?? []));

  function cfiCategory(code: string) {
    switch (code.charAt(0).toUpperCase()) {
      case 'E':
        return 'Equity';
      case 'D':
        return 'Debt';
      case 'R':
        return 'Entitlement';
      case 'O':
        return 'Option';
      case 'F':
        return 'Future';
      case 'M':
        return 'Other';
      default:
        return 'Unclassified';
    }
  }

  function createCfiRows(instruments: Instrument[]) {
    const groups: Record<string, CfiRow> = {};

    for (const instrument of instruments) {
      const code = instrument.cfi || 'Unassigned';
      const group = groups[code] ?? {
        code,
        category: cfiCategory(code),
        examples: [],
        instrumentCount: 0,
        lastAuditDateTime: instrument.lastAuditDateTime
      };

      group.instrumentCount += 1;
      group.lastAuditDateTime = maxDate(group.lastAuditDateTime, instrument.lastAuditDateTime);

      if (group.examples.length < 3)
        group.examples.push(instrument.name);

      groups[code] = group;
    }

    return Object.values(groups).sort((left, right) => left.code.localeCompare(right.code));
  }

  function maxDate(left: string, right: string) {
    return new Date(left).getTime() >= new Date(right).getTime() ? left : right;
  }
</script>

{#if showFilter}
  <div class="data-list-empty-filter" aria-label="CFI filter placeholder"></div>
{/if}

{#if showBody}
  <section class="page-container page-section data-list-embedded-page data-list-embedded-body">
    {#if data.error}
      <div class="status-panel status-panel-error">{data.error}</div>
    {:else if data.instruments}
      <AggregateUpdateWatcher aggregateKind="Instruments" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.instruments.lastEventID} />

      <div class="data-summary">
        <div><span class="font-semibold text-slate-950">{cfiRows.length}</span> CFI codes</div>
        <div>Valuation {formatDisplayDateTime(data.instruments.valuationDateTime)} - As-of {asOfSummary}</div>
      </div>

      <div class="data-panel">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">CFI</th>
                <th class="px-3 py-2">Category</th>
                <th class="px-3 py-2 text-right">Instruments</th>
                <th class="px-3 py-2">Examples</th>
                <th class="px-3 py-2">Last audit</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each cfiRows as row (row.code)}
                <tr class="hover:bg-slate-50">
                  <td class="px-3 py-2 font-mono text-slate-800">{row.code}</td>
                  <td class="px-3 py-2 text-slate-700">{row.category}</td>
                  <td class="px-3 py-2 text-right font-mono text-slate-700">{row.instrumentCount}</td>
                  <td class="px-3 py-2 text-slate-600">{row.examples.join(', ')}</td>
                  <td class="px-3 py-2 text-slate-600">{formatTableDateTime(row.lastAuditDateTime)}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </section>
{/if}

<style>
  .data-list-empty-filter {
    min-height: 0;
  }
</style>
