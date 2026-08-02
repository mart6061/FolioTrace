<script lang="ts">
  import type { TableExportDefinition } from '$lib/export';
  import TableExportActions from './TableExportActions.svelte';

  let {
    filterText = $bindable(''),
    filterLabel = 'Filter table',
    placeholder = 'Filter...',
    onadd,
    exportDefinition,
    onprint
  }: {
    filterText?: string;
    filterLabel?: string;
    placeholder?: string;
    onadd?: () => void;
    exportDefinition?: TableExportDefinition;
    onprint?: () => void;
  } = $props();
</script>

<div class="table-toolbar table-tools-template">
  <label class="table-filter">
    <span class="sr-only">{filterLabel}</span>
    <input bind:value={filterText} {placeholder} type="search" />
  </label>

  <div class="table-actions" aria-label="Table actions">
    {#if onadd}
      <button aria-label="Add row" onclick={onadd} title="Add" type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg></button>
    {/if}
    {#if exportDefinition}
      <TableExportActions definition={exportDefinition} inline />
    {/if}
    {#if onprint}
      <button aria-label="Print table" onclick={onprint} title="Print" type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" /></svg></button>
    {/if}
  </div>
</div>

<style>
  .table-tools-template {
    margin: 0 -1rem;
    border-radius: calc(var(--house-radius) - 1px) calc(var(--house-radius) - 1px) 0 0;
  }
</style>
