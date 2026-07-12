<script lang="ts">
  let {
    filterText = $bindable(''),
    filterLabel = 'Filter table',
    placeholder = 'Filter...',
    onadd,
    onexportjson,
    onexportcsv,
    onexportxlsx,
    onprint
  }: {
    filterText?: string;
    filterLabel?: string;
    placeholder?: string;
    onadd?: () => void;
    onexportjson?: () => void;
    onexportcsv?: () => void;
    onexportxlsx?: () => void;
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
    {#if onexportjson}
      <button aria-label="Export to JSON" onclick={onexportjson} title="Export JSON" type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" /></svg></button>
    {/if}
    {#if onexportcsv}
      <button aria-label="Export to CSV" onclick={onexportcsv} title="Export CSV" type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 4h16v16H4zM4 10h16M10 4v16" /></svg></button>
    {/if}
    {#if onexportxlsx}
      <button aria-label="Export to XLSX" onclick={onexportxlsx} title="Export XLSX" type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" /></svg></button>
    {/if}
    {#if onprint}
      <button aria-label="Print table" onclick={onprint} title="Print" type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" /></svg></button>
    {/if}
  </div>
</div>

<style>
  .table-tools-template {
    margin: -1rem -1rem 0;
    border-radius: calc(var(--house-radius) - 1px) calc(var(--house-radius) - 1px) 0 0;
  }
</style>
