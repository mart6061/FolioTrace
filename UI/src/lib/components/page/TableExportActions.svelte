<script lang="ts">
  import { exportTable, type TableExportDefinition, type TableExportFormat } from '$lib/export';

  let {
    definition,
    inline = false
  }: {
    definition: TableExportDefinition;
    inline?: boolean;
  } = $props();

  let busyFormat = $state<TableExportFormat | null>(null);
  let error = $state('');
  const empty = $derived(definition.rows.length === 0 && !(definition.summaryRows?.length));

  async function run(format: TableExportFormat) {
    if (empty || busyFormat)
      return;

    busyFormat = format;
    error = '';
    try {
      await exportTable(definition, format);
    } catch (caught) {
      error = caught instanceof Error ? caught.message : `Unable to export ${format.toUpperCase()}.`;
    } finally {
      busyFormat = null;
    }
  }
</script>

<div class:table-export-actions-inline={inline} class:table-actions={!inline} class="table-export-actions no-print">
  <button aria-label="Export to JSON" disabled={empty || busyFormat !== null} onclick={() => run('json')} title={empty ? 'No rows to export' : 'Export JSON'} type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" /></svg></button>
  <button aria-label="Export to CSV" disabled={empty || busyFormat !== null} onclick={() => run('csv')} title={empty ? 'No rows to export' : 'Export CSV'} type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 4h16v16H4zM4 10h16M10 4v16" /></svg></button>
  <button aria-label="Export to XLSX" disabled={empty || busyFormat !== null} onclick={() => run('xlsx')} title={empty ? 'No rows to export' : busyFormat === 'xlsx' ? 'Creating XLSX…' : 'Export XLSX'} type="button"><svg aria-hidden="true" viewBox="0 0 24 24"><path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" /></svg></button>
  <span aria-live="polite" class="sr-only">{error || (busyFormat ? `Creating ${busyFormat.toUpperCase()} export` : '')}</span>
</div>

<style>
  .table-export-actions-inline {
    display: contents;
  }

  :global(.table-export-actions button:disabled) {
    cursor: not-allowed;
    opacity: 0.45;
  }

  @media print {
    .table-export-actions {
      display: none !important;
    }
  }
</style>
