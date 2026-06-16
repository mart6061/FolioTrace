<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import type { ReportChartPieLevel, ReportConfig, ReportNodeBase, ReportNodePageOrientation, ReportNodeType, ReportValuationColumn, ReportValuationColumnKey } from '$lib/types';
  import type { ActionData, PageData, SubmitFunction } from './$types';

  let { data, form }: { data: PageData; form: ActionData } = $props();

  const nodeTypes: { type: ReportNodeType; label: string; title: string }[] = [
    { type: 'ReportNodeCoverPage', label: 'Cover Page', title: 'Cover Page' },
    { type: 'ReportNodeIndex', label: 'Index', title: 'Index' },
    { type: 'ReportNodeChart', label: 'Chart', title: 'Asset Allocation Chart' },
    { type: 'ReportNodeValuation', label: 'Valuation', title: 'Valuation' },
    { type: 'ReportNodeTransactions', label: 'Transactions', title: 'Transactions' },
    { type: 'ReportNodeCash', label: 'Cash', title: 'Cash' }
  ];

  const valuationColumnDefinitions: { key: ReportValuationColumnKey; label: string }[] = [
    { key: 'InstrumentName', label: 'Instrument Name' },
    { key: 'ISIN', label: 'ISIN' },
    { key: 'Sedol', label: 'Sedol' },
    { key: 'QuotePrice', label: 'Quote Price' },
    { key: 'Quantity', label: 'Quantity' },
    { key: 'BookValue', label: 'Book Value' },
    { key: 'BookCost', label: 'Book Cost' },
    { key: 'Weight', label: 'Weight' },
    { key: 'Target', label: 'Target' },
    { key: 'Min', label: 'Min' },
    { key: 'Max', label: 'Max' }
  ];

  const pageOrientationOptions: { value: ReportNodePageOrientation; label: string }[] = [
    { value: 'Portrait', label: 'Portrait' },
    { value: 'Landscape', label: 'Landscape' }
  ];

  const pieLevelOptions: { value: ReportChartPieLevel; label: string }[] = [
    { value: 1, label: 'Level 1' },
    { value: 2, label: 'Level 2' },
    { value: 3, label: 'Level 3' }
  ];

  const accounts = $derived(data.accounts?.items ?? []);
  const reportConfigs = $derived(data.reportConfigs?.items ?? []);
  const assetAllocations = $derived(data.valuationSettings?.items ?? []);
  const selectedAccountIDs = $derived((data.selectedAccountIDs ?? []) as string[]);

  let editingReportID = $state('');
  let draftName = $state('');
  let draftActive = $state(true);
  let draftNodes = $state<ReportNodeBase[]>([]);
  let submitting = $state('');
  let templateDragInProgress = $state(false);
  let draggedReportNodeID = $state<string | null>(null);

  const draftNodesJson = $derived(JSON.stringify(normalizeDraftNodes(draftNodes)));

  const enhanceAction = (name: string): SubmitFunction => {
    return () => {
      submitting = name;
      return async ({ update }) => {
        await update({ reset: false, invalidateAll: false });
        submitting = '';
      };
    };
  };

  function startEdit(report: ReportConfig) {
    editingReportID = report.reportID;
    draftName = report.name;
    draftActive = report.active;
    draftNodes = normalizeReportNodes(sortReportNodes(report.nodes));
  }

  function closeEdit() {
    editingReportID = '';
    draftName = '';
    draftActive = true;
    draftNodes = [];
  }

  function submitFilterChange(event: Event) {
    const input = event.currentTarget;
    input instanceof HTMLInputElement && input.form?.requestSubmit();
  }

  function normalizeReportNodes(nodes: ReportNodeBase[]) {
    return nodes.map((node, index) => ({
      ...node,
      type: reportNodeType(node),
      displayOrder: index + 1,
      reportNodeID: node.reportNodeID,
      pageOrientation: normalizePageOrientation(node.pageOrientation),
      chartType: reportNodeType(node) === 'ReportNodeChart' ? node.chartType ?? 'Pie' : undefined,
      pieLevel: reportNodeType(node) === 'ReportNodeChart' ? normalizePieLevel(node.pieLevel) : undefined,
      columns: reportNodeType(node) === 'ReportNodeValuation' ? normalizeValuationColumns(node.columns) : undefined,
      colourBullet: reportNodeType(node) === 'ReportNodeValuation' ? node.colourBullet ?? true : undefined,
      colourText: reportNodeType(node) === 'ReportNodeValuation' ? node.colourText ?? false : undefined
    }));
  }

  function sortReportNodes(nodes: ReportNodeBase[]) {
    return [...nodes].sort((left, right) => left.displayOrder - right.displayOrder);
  }

  function normalizeDraftNodes(nodes: ReportNodeBase[]) {
    return nodes.map((node, index) => {
      const type = reportNodeType(node);
      return {
        type,
        reportNodeID: node.reportNodeID,
        displayOrder: index + 1,
        name: node.name,
        title: node.title,
        pageOrientation: normalizePageOrientation(node.pageOrientation),
        assetAllocationID: requiresAssetAllocation(type) ? node.assetAllocationID || firstAssetAllocationID() : undefined,
        chartType: type === 'ReportNodeChart' ? node.chartType ?? 'Pie' : undefined,
        pieLevel: type === 'ReportNodeChart' && (node.chartType ?? 'Pie') === 'Pie' ? normalizePieLevel(node.pieLevel) : undefined,
        columns: type === 'ReportNodeValuation' ? normalizeValuationColumns(node.columns) : undefined,
        colourBullet: type === 'ReportNodeValuation' ? node.colourBullet ?? true : undefined,
        colourText: type === 'ReportNodeValuation' ? node.colourText ?? false : undefined
      };
    });
  }

  function reportNodeType(node: ReportNodeBase): ReportNodeType {
    return node.type ?? node.$type ?? 'ReportNodeCoverPage';
  }

  function nodeTypeLabel(type: ReportNodeType) {
    return nodeTypes.find((nodeType) => nodeType.type === type)?.label ?? type.replace(/^ReportNode/, '');
  }

  function normalizePageOrientation(value: ReportNodeBase['pageOrientation']): ReportNodePageOrientation {
    return value === 'Landscape' ? 'Landscape' : 'Portrait';
  }

  function normalizePieLevel(value: ReportNodeBase['pieLevel']): ReportChartPieLevel {
    return value === 2 || value === 3 ? value : 1;
  }

  function firstAssetAllocationID() {
    return assetAllocations[0]?.assetAllocationID ?? '';
  }

  function requiresAssetAllocation(type: ReportNodeType) {
    return type === 'ReportNodeChart' || type === 'ReportNodeValuation' || type === 'ReportNodeTransactions' || type === 'ReportNodeCash';
  }

  function createNode(type: ReportNodeType): ReportNodeBase {
    const template = nodeTypes.find((nodeType) => nodeType.type === type) ?? nodeTypes[0];
    const node: ReportNodeBase = {
      type,
      reportNodeID: crypto.randomUUID(),
      displayOrder: draftNodes.length + 1,
      name: template.label,
      title: template.title,
      pageOrientation: 'Portrait'
    };

    if (requiresAssetAllocation(type))
      node.assetAllocationID = firstAssetAllocationID();

    if (type === 'ReportNodeChart') {
      node.chartType = 'Pie';
      node.pieLevel = 1;
    }

    if (type === 'ReportNodeValuation') {
      node.columns = defaultValuationColumns();
      node.colourBullet = true;
      node.colourText = false;
    }

    return node;
  }

  function addNode(type: ReportNodeType, index = draftNodes.length) {
    const next = [...draftNodes];
    next.splice(index, 0, createNode(type));
    draftNodes = normalizeReportNodes(next);
  }

  function handleTemplateClick(type: ReportNodeType) {
    if (templateDragInProgress) {
      templateDragInProgress = false;
      return;
    }

    addNode(type);
  }

  function removeNode(reportNodeID: string) {
    draftNodes = normalizeReportNodes(draftNodes.filter((node) => node.reportNodeID !== reportNodeID));
  }

  function moveNodeByOffset(reportNodeID: string, offset: -1 | 1) {
    const currentIndex = draftNodes.findIndex((node) => node.reportNodeID === reportNodeID);
    const nextIndex = currentIndex + offset;

    if (currentIndex < 0 || nextIndex < 0 || nextIndex >= draftNodes.length)
      return;

    const next = [...draftNodes];
    [next[currentIndex], next[nextIndex]] = [next[nextIndex], next[currentIndex]];
    draftNodes = normalizeReportNodes(next);
  }

  function moveNode(reportNodeID: string, index: number) {
    const currentIndex = draftNodes.findIndex((node) => node.reportNodeID === reportNodeID);
    if (currentIndex < 0)
      return;

    const next = [...draftNodes];
    const [node] = next.splice(currentIndex, 1);
    const targetIndex = currentIndex < index ? index - 1 : index;
    next.splice(Math.max(0, Math.min(targetIndex, next.length)), 0, node);
    draftNodes = normalizeReportNodes(next);
  }

  function handleTemplateDragStart(event: DragEvent, type: ReportNodeType) {
    templateDragInProgress = true;
    event.dataTransfer?.setData('application/x-report-node-template', type);
    event.dataTransfer?.setData('text/plain', type);
    if (event.dataTransfer)
      event.dataTransfer.effectAllowed = 'copy';
  }

  function handleTemplateDragEnd() {
    setTimeout(() => {
      templateDragInProgress = false;
    }, 0);
  }

  function handleNodeDragStart(event: DragEvent, reportNodeID: string) {
    event.stopPropagation();
    draggedReportNodeID = reportNodeID;
    event.dataTransfer?.setData('application/x-report-node-id', reportNodeID);
    event.dataTransfer?.setData('text/plain', reportNodeID);
    if (event.dataTransfer)
      event.dataTransfer.effectAllowed = 'move';
  }

  function handleNodeDragEnd() {
    draggedReportNodeID = null;
  }

  function handleDrop(event: DragEvent, index = draftNodes.length) {
    if (!isReportNodeDrag(event))
      return;

    event.preventDefault();
    event.stopPropagation();
    const templateType = event.dataTransfer?.getData('application/x-report-node-template') as ReportNodeType;
    const reportNodeID = event.dataTransfer?.getData('application/x-report-node-id');

    if (templateType && nodeTypes.some((nodeType) => nodeType.type === templateType)) {
      addNode(templateType, index);
      return;
    }

    if (reportNodeID)
      moveNode(reportNodeID, index);

    draggedReportNodeID = null;
  }

  function handleNodeCardDrop(event: DragEvent, index: number) {
    const card = event.currentTarget;
    const targetIndex = card instanceof HTMLElement && event.clientY > card.getBoundingClientRect().top + card.offsetHeight / 2
      ? index + 1
      : index;

    handleDrop(event, targetIndex);
  }

  function allowDrop(event: DragEvent) {
    if (!isReportNodeDrag(event))
      return;

    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer)
      event.dataTransfer.dropEffect = event.dataTransfer.types.includes('application/x-report-node-template') ? 'copy' : 'move';
  }

  function isReportNodeDrag(event: DragEvent) {
    return Boolean(
      event.dataTransfer?.types.includes('application/x-report-node-template')
      || event.dataTransfer?.types.includes('application/x-report-node-id')
    );
  }

  function defaultValuationColumns(): ReportValuationColumn[] {
    return valuationColumnDefinitions.map((column, index) => ({ columnKey: column.key, displayOrder: index + 1 }));
  }

  function normalizeValuationColumns(columns: ReportNodeBase['columns']): ReportValuationColumn[] {
    return (columns ?? defaultValuationColumns())
      .filter((column) => valuationColumnDefinitions.some((definition) => definition.key === column.columnKey))
      .sort((left, right) => left.displayOrder - right.displayOrder)
      .map((column, index) => ({ columnKey: column.columnKey, displayOrder: index + 1 }));
  }

  function valuationNodeColumns(node: ReportNodeBase): ReportValuationColumn[] {
    return normalizeValuationColumns(node.columns);
  }

  function valuationColumnLabel(columnKey: ReportValuationColumnKey) {
    return valuationColumnDefinitions.find((column) => column.key === columnKey)?.label ?? columnKey;
  }

  function isAssetAllocationColumn(columnKey: ReportValuationColumnKey) {
    return columnKey === 'Weight' || columnKey === 'Target' || columnKey === 'Min' || columnKey === 'Max';
  }

  function valuationColumnToneClass(columnKey: ReportValuationColumnKey) {
    return isAssetAllocationColumn(columnKey) ? 'report-column-pill-asset-allocation' : 'report-column-pill-valuation';
  }

  function updateValuationNodeColumns(reportNodeID: string, columns: ReportValuationColumn[]) {
    draftNodes = normalizeReportNodes(draftNodes.map((node) => node.reportNodeID === reportNodeID ? { ...node, columns: sequenceValuationColumns(columns) } : node));
  }

  function sequenceValuationColumns(columns: ReportValuationColumn[]) {
    return columns
      .filter((column) => valuationColumnDefinitions.some((definition) => definition.key === column.columnKey))
      .map((column, index) => ({ columnKey: column.columnKey, displayOrder: index + 1 }));
  }

  function addValuationColumn(reportNodeID: string, columnKey: ReportValuationColumnKey, index?: number) {
    const node = draftNodes.find((item) => item.reportNodeID === reportNodeID);
    if (!node)
      return;

    const columns = normalizeValuationColumns(node.columns);
    columns.splice(index ?? columns.length, 0, { columnKey, displayOrder: columns.length + 1 });
    updateValuationNodeColumns(reportNodeID, columns);
  }

  function removeValuationColumn(reportNodeID: string, index: number) {
    const node = draftNodes.find((item) => item.reportNodeID === reportNodeID);
    if (!node)
      return;

    const columns = normalizeValuationColumns(node.columns);
    columns.splice(index, 1);
    updateValuationNodeColumns(reportNodeID, columns);
  }

  function moveValuationColumn(reportNodeID: string, currentIndex: number, index: number) {
    const node = draftNodes.find((item) => item.reportNodeID === reportNodeID);
    if (!node || currentIndex < 0)
      return;

    const columns = normalizeValuationColumns(node.columns);
    const [column] = columns.splice(currentIndex, 1);
    if (!column)
      return;

    const targetIndex = currentIndex < index ? index - 1 : index;
    columns.splice(Math.max(0, Math.min(targetIndex, columns.length)), 0, column);
    updateValuationNodeColumns(reportNodeID, columns);
  }

  function handleValuationColumnSourceDragStart(event: DragEvent, columnKey: ReportValuationColumnKey) {
    event.stopPropagation();
    event.dataTransfer?.setData('application/x-report-valuation-column-template', columnKey);
    event.dataTransfer?.setData('text/plain', columnKey);
    if (event.dataTransfer)
      event.dataTransfer.effectAllowed = 'copy';
  }

  function handleValuationColumnDragStart(event: DragEvent, reportNodeID: string, index: number) {
    event.stopPropagation();
    event.dataTransfer?.setData('application/x-report-valuation-column-index', `${reportNodeID}:${index}`);
    event.dataTransfer?.setData('text/plain', `${reportNodeID}:${index}`);
    if (event.dataTransfer)
      event.dataTransfer.effectAllowed = 'move';
  }

  function allowValuationColumnDrop(event: DragEvent) {
    if (!isValuationColumnDrag(event))
      return;

    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer)
      event.dataTransfer.dropEffect = event.dataTransfer.types.includes('application/x-report-valuation-column-template') ? 'copy' : 'move';
  }

  function handleValuationColumnDrop(event: DragEvent, reportNodeID: string, index?: number) {
    if (!isValuationColumnDrag(event))
      return;

    event.preventDefault();
    event.stopPropagation();

    const columnKey = event.dataTransfer?.getData('application/x-report-valuation-column-template');
    if (isValuationColumnKey(columnKey)) {
      addValuationColumn(reportNodeID, columnKey, index);
      return;
    }

    const dragged = event.dataTransfer?.getData('application/x-report-valuation-column-index') ?? '';
    const [sourceReportNodeID, sourceIndexValue] = dragged.split(':');
    const sourceIndex = Number(sourceIndexValue);

    if (sourceReportNodeID === reportNodeID && Number.isInteger(sourceIndex))
      moveValuationColumn(reportNodeID, sourceIndex, index ?? Number.MAX_SAFE_INTEGER);
  }

  function handleValuationColumnPillDrop(event: DragEvent, reportNodeID: string, index: number) {
    if (!isValuationColumnDrag(event))
      return;

    const target = event.currentTarget;
    const targetIndex = target instanceof HTMLElement && event.clientX > target.getBoundingClientRect().left + target.offsetWidth / 2
      ? index + 1
      : index;

    handleValuationColumnDrop(event, reportNodeID, targetIndex);
  }

  function isValuationColumnDrag(event: DragEvent) {
    return Boolean(
      event.dataTransfer?.types.includes('application/x-report-valuation-column-template')
      || event.dataTransfer?.types.includes('application/x-report-valuation-column-index')
    );
  }

  function isValuationColumnKey(value: string | undefined): value is ReportValuationColumnKey {
    return valuationColumnDefinitions.some((column) => column.key === value);
  }

  function assetAllocationName(assetAllocationID: string | undefined) {
    return assetAllocations.find((allocation) => allocation.assetAllocationID === assetAllocationID)?.name ?? 'Asset allocation';
  }
</script>

<svelte:head>
  <title>Report Tools - FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-content">
        <div class="page-header-main">
          <p class="page-kicker">Data / Configuration</p>
          <div class="page-title-row">
            <h1 class="page-title">Report Tools</h1>
          </div>
        </div>
        <div class="page-header-aside">
          <div class="page-header-summary">Report configs to {data.valuationDate} as-of {data.auditDateTime || 'now'}</div>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <section class="error-panel">{data.error}</section>
    {:else if data.accounts && data.reportConfigs && data.valuationSettings}
    <div class="report-tools-toolbar">
      <form class="report-tools-filter" method="GET">
        <label class="field report-account-filter">
          <span>Accounts</span>
          <details class="report-multiselect">
            <summary>{selectedAccountIDs.length === accounts.length ? 'All accounts' : `${selectedAccountIDs.length} selected`}</summary>
            <div class="report-multiselect-options">
              {#each accounts as account (account.accountID)}
                <label class="report-checkbox-option">
                  <input name="accountID" type="checkbox" value={account.accountID} checked={selectedAccountIDs.includes(account.accountID)} onchange={submitFilterChange} />
                  <span>{account.name}</span>
                </label>
              {/each}
            </div>
          </details>
        </label>
        <label class="toggle-row report-show-all-toggle">
          <span>Show All</span>
          <span class="report-switch">
            <input name="showAll" type="checkbox" value="true" checked={data.showAll} onchange={submitFilterChange} />
            <span class="report-switch-track" aria-hidden="true"></span>
          </span>
        </label>
      </form>

      <form class="report-new-form table-actions" aria-label="Report actions" method="POST" action="?/createReport" use:enhance={enhanceAction('createReport')}>
        <input type="hidden" name="name" value="New Report" />
        <button aria-label="Add report" title="Add report" type="submit" disabled={submitting === 'createReport'}>
          <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14" /></svg>
        </button>
      </form>
    </div>

    {#if form?.message}
      <div class:success-panel={form.status === 'success'} class:error-panel={form.status !== 'success'}>{form.message}</div>
    {/if}

    <AggregateUpdateWatcher aggregateKind="ReportConfigs" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.reportConfigs.lastEventID} />

    <section class="report-config-list">
      {#each reportConfigs as report (report.reportID)}
        <article class="report-config-card">
          <div class="report-config-summary">
            <div>
              <h2>{report.name}</h2>
              <div class="report-config-meta">
                <span>{report.active ? 'Active' : 'Disabled'}</span>
                <span>{report.nodes.length} nodes</span>
              </div>
            </div>
            <div class="report-config-actions">
              {#if editingReportID === report.reportID}
                <button class="btn btn-primary" type="submit" form={`report-editor-${report.reportID}`} disabled={submitting === `saveReport-${report.reportID}`}>Save</button>
              {/if}
              <button class="btn btn-secondary" type="button" onclick={() => editingReportID === report.reportID ? closeEdit() : startEdit(report)}>
                {editingReportID === report.reportID ? 'Close' : 'Edit'}
              </button>
            </div>
          </div>

          {#if editingReportID === report.reportID}
            <form id={`report-editor-${report.reportID}`} class="report-editor" method="POST" action="?/saveReport" use:enhance={enhanceAction(`saveReport-${report.reportID}`)}>
              <input type="hidden" name="reportID" value={report.reportID} />
              <input type="hidden" name="nodesJson" value={draftNodesJson} />

              <div class="report-editor-fields">
                <label class="field">
                  <span>Name</span>
                  <input class="input" name="name" bind:value={draftName} required />
                </label>
                <label class="toggle-row report-active-toggle">
                  <span>Active</span>
                  <span class="report-switch">
                    <input name="active" type="checkbox" value="true" bind:checked={draftActive} />
                    <span class="report-switch-track" aria-hidden="true"></span>
                  </span>
                </label>
              </div>

              <div class="report-builder-layout">
                <aside class="report-node-palette">
                  <h3>Nodes</h3>
                  {#each nodeTypes as nodeType (nodeType.type)}
                    <button
                      class="report-node-template"
                      draggable="true"
                      type="button"
                      ondragstart={(event) => handleTemplateDragStart(event, nodeType.type)}
                      ondragend={handleTemplateDragEnd}
                      onclick={() => handleTemplateClick(nodeType.type)}
                    >
                      {nodeType.label}
                    </button>
                  {/each}
                </aside>

                <div class="report-flow" role="list" ondragover={allowDrop} ondrop={(event) => handleDrop(event)}>
                  {#each draftNodes as node, index (node.reportNodeID)}
                    {@const type = reportNodeType(node)}
                    <div class="report-drop-target" role="presentation" ondragover={allowDrop} ondrop={(event) => handleDrop(event, index)}></div>
                    <section
                      class={['report-node-card', draggedReportNodeID === node.reportNodeID && 'is-dragging']}
                      role="listitem"
                      draggable="true"
                      ondragstart={(event) => handleNodeDragStart(event, node.reportNodeID)}
                      ondragend={handleNodeDragEnd}
                      ondragover={allowDrop}
                      ondrop={(event) => handleNodeCardDrop(event, index)}
                    >
                      <div class="report-node-card-header">
                        <span class="report-node-kind">{index + 1}. {nodeTypeLabel(type)}</span>
                        <div class="report-node-actions">
                          <div class="report-node-order-controls" aria-label="Card order controls">
                            {#if index > 0}
                              <button
                                class="report-node-move-button"
                                type="button"
                                aria-label={`Move ${node.name} up`}
                                title="Move up"
                                onclick={() => moveNodeByOffset(node.reportNodeID, -1)}
                              >
                                <svg aria-hidden="true" viewBox="0 0 24 24">
                                  <path d="M12 5v14M6 11l6-6 6 6" />
                                </svg>
                              </button>
                            {/if}
                            {#if index < draftNodes.length - 1}
                              <button
                                class="report-node-move-button"
                                type="button"
                                aria-label={`Move ${node.name} down`}
                                title="Move down"
                                onclick={() => moveNodeByOffset(node.reportNodeID, 1)}
                              >
                                <svg aria-hidden="true" viewBox="0 0 24 24">
                                  <path d="M12 5v14M6 13l6 6 6-6" />
                                </svg>
                              </button>
                            {/if}
                          </div>
                          <button
                            class="report-node-delete-button"
                            type="button"
                            aria-label={`Delete ${node.name}`}
                            title="Delete"
                            onclick={() => removeNode(node.reportNodeID)}
                          >
                            <svg aria-hidden="true" viewBox="0 0 24 24">
                              <path d="M4 7h16M10 11v6M14 11v6M6 7l1 14h10l1-14M9 7V4h6v3" />
                            </svg>
                          </button>
                        </div>
                      </div>
                      <div class="report-node-fields">
                        <label class="field">
                          <span>Name</span>
                          <input class="input" bind:value={node.name} required />
                        </label>
                        <label class="field">
                          <span>Title</span>
                          <input class="input" bind:value={node.title} required />
                        </label>
                        <fieldset class="field report-orientation-field">
                          <legend>Orientation</legend>
                          <div class="report-segmented-control">
                            {#each pageOrientationOptions as option (option.value)}
                              <label>
                                <input type="radio" bind:group={node.pageOrientation} value={option.value} />
                                <span>{option.label}</span>
                              </label>
                            {/each}
                          </div>
                        </fieldset>
                        {#if requiresAssetAllocation(type)}
                          <label class="field">
                            <span>Asset allocation</span>
                            <select class="input" bind:value={node.assetAllocationID} required>
                              {#each assetAllocations as allocation (allocation.assetAllocationID)}
                                <option value={allocation.assetAllocationID}>{allocation.name}</option>
                              {/each}
                            </select>
                          </label>
                        {/if}
                        {#if type === 'ReportNodeChart'}
                          <label class="field">
                            <span>Chart type</span>
                            <select class="input" bind:value={node.chartType}>
                              <option value="Pie">Pie</option>
                              <option value="Bar">Bar</option>
                            </select>
                          </label>
                          {#if (node.chartType ?? 'Pie') === 'Pie'}
                            <label class="field">
                              <span>Pie level</span>
                              <select class="input" bind:value={node.pieLevel}>
                                {#each pieLevelOptions as option (option.value)}
                                  <option value={option.value}>{option.label}</option>
                                {/each}
                              </select>
                            </label>
                          {/if}
                        {/if}
                      </div>
                      {#if type === 'ReportNodeValuation'}
                        {@const valuationColumns = valuationNodeColumns(node)}
                        <div class="report-valuation-display-options" aria-label="Valuation display options">
                          <label class="report-valuation-colour-option">
                            <input bind:checked={node.colourBullet} type="checkbox" />
                            <span>Colour Bullet</span>
                          </label>
                          <label class="report-valuation-colour-option">
                            <input bind:checked={node.colourText} type="checkbox" />
                            <span>Colour Text</span>
                          </label>
                        </div>
                        <div class="report-valuation-columns">
                          <aside class="report-column-source">
                            <h4>Columns</h4>
                            <div class="report-column-source-list">
                              {#each valuationColumnDefinitions as column (column.key)}
                                <button
                                  class={[
                                    'report-column-pill',
                                    'report-column-source-pill',
                                    valuationColumnToneClass(column.key)
                                  ]}
                                  draggable="true"
                                  type="button"
                                  ondragstart={(event) => handleValuationColumnSourceDragStart(event, column.key)}
                                  onclick={() => addValuationColumn(node.reportNodeID, column.key)}
                                >
                                  {column.label}
                                </button>
                              {/each}
                            </div>
                          </aside>
                          <div class="report-column-selected">
                            <h4>Selected</h4>
                            <div
                              class="report-column-selected-list"
                              role="list"
                              ondragover={allowValuationColumnDrop}
                              ondrop={(event) => handleValuationColumnDrop(event, node.reportNodeID)}
                            >
                              <div class="report-column-pill report-column-selected-pill report-column-fixed-pill" role="listitem" aria-label="Fixed Name column">
                                <span>Name</span>
                              </div>
                              {#each valuationColumns as column, columnIndex (`${column.columnKey}-${columnIndex}`)}
                                <div
                                  class={[
                                    'report-column-pill',
                                    'report-column-selected-pill',
                                    valuationColumnToneClass(column.columnKey)
                                  ]}
                                  role="listitem"
                                  draggable="true"
                                  ondragstart={(event) => handleValuationColumnDragStart(event, node.reportNodeID, columnIndex)}
                                  ondragover={allowValuationColumnDrop}
                                  ondrop={(event) => handleValuationColumnPillDrop(event, node.reportNodeID, columnIndex)}
                                >
                                  <span>{valuationColumnLabel(column.columnKey)}</span>
                                  <button
                                    class="report-column-delete"
                                    type="button"
                                    aria-label={`Remove ${valuationColumnLabel(column.columnKey)}`}
                                    onclick={() => removeValuationColumn(node.reportNodeID, columnIndex)}
                                  >
                                    &times;
                                  </button>
                                </div>
                              {:else}
                                <div class="report-column-empty">No additional columns selected.</div>
                              {/each}
                            </div>
                          </div>
                        </div>
                      {/if}
                      {#if requiresAssetAllocation(type)}
                        <div class="report-node-footnote">{assetAllocationName(node.assetAllocationID)}</div>
                      {/if}
                    </section>
                  {:else}
                    <div class="report-flow-empty" role="presentation" ondragover={allowDrop} ondrop={(event) => handleDrop(event)}>
                      Drag report nodes here
                    </div>
                  {/each}
                  {#if draftNodes.length}
                    <div class="report-drop-target report-drop-target-end" role="presentation" ondragover={allowDrop} ondrop={(event) => handleDrop(event)}></div>
                  {/if}
                </div>
              </div>
            </form>
          {/if}
        </article>
      {:else}
        <section class="empty-panel">No report configs found.</section>
      {/each}
    </section>
    {/if}
  </section>
</main>

<style>
  .report-tools-toolbar,
  .report-config-card,
  .report-editor {
    border: 1px solid var(--line);
    border-radius: 0.5rem;
    background: var(--panel);
  }

  .report-tools-toolbar {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 0.75rem;
    align-items: end;
    padding: 0.75rem;
  }

  .report-tools-filter {
    display: grid;
    grid-template-columns: minmax(18rem, 28rem) auto;
    gap: 0.75rem;
    align-items: end;
  }

  .report-new-form {
    display: flex;
    align-items: end;
  }

  .report-multiselect {
    position: relative;
    border: 1px solid var(--line);
    border-radius: 0.45rem;
    background: var(--panel);
  }

  .report-multiselect summary {
    min-height: 2.25rem;
    padding: 0.5rem 0.65rem;
    cursor: pointer;
    font-size: 0.875rem;
    font-weight: 700;
    list-style: none;
  }

  .report-multiselect summary::-webkit-details-marker {
    display: none;
  }

  .report-multiselect-options {
    position: absolute;
    z-index: 30;
    top: calc(100% + 0.3rem);
    left: 0;
    min-width: max(100%, 18rem);
    max-height: 18rem;
    overflow: auto;
    border: 1px solid var(--line);
    border-radius: 0.45rem;
    background: var(--panel);
    box-shadow: 0 0.9rem 1.6rem color-mix(in srgb, var(--ink) 14%, transparent);
  }

  .report-checkbox-option {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr);
    gap: 0.45rem;
    align-items: center;
    padding: 0.35rem 0.65rem;
    border-top: 1px solid color-mix(in srgb, var(--line) 70%, transparent);
    font-size: 0.8125rem;
    font-weight: 650;
  }

  .toggle-row {
    display: inline-flex;
    gap: 0.45rem;
    align-items: center;
    min-height: 2.25rem;
    color: var(--ink);
    font-size: 0.875rem;
    font-weight: 720;
  }

  .report-show-all-toggle {
    gap: 0.55rem;
    white-space: nowrap;
  }

  .report-switch {
    position: relative;
    display: inline-flex;
    width: 2.4rem;
    height: 1.35rem;
    cursor: pointer;
    align-items: center;
  }

  .report-switch input {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip: rect(0 0 0 0);
    white-space: nowrap;
  }

  .report-switch-track {
    position: relative;
    width: 2.4rem;
    height: 1.35rem;
    border: 1px solid var(--line);
    border-radius: 999px;
    background: color-mix(in srgb, var(--line) 72%, var(--panel));
    transition:
      background 160ms ease,
      border-color 160ms ease;
  }

  .report-switch-track::after {
    position: absolute;
    top: 0.15rem;
    left: 0.15rem;
    width: 0.95rem;
    height: 0.95rem;
    border-radius: 999px;
    background: var(--panel);
    box-shadow: 0 1px 3px color-mix(in srgb, var(--ink) 24%, transparent);
    content: '';
    transition: transform 160ms ease;
  }

  .report-switch input:checked + .report-switch-track {
    border-color: var(--accent);
    background: var(--accent);
  }

  .report-switch input:checked + .report-switch-track::after {
    transform: translateX(1.05rem);
  }

  .report-switch input:focus-visible + .report-switch-track {
    outline: 2px solid var(--focus-ring);
    outline-offset: 2px;
  }

  .report-active-toggle {
    align-self: end;
    gap: 0.55rem;
    white-space: nowrap;
  }

  .report-config-list {
    display: grid;
    gap: 0.85rem;
    margin-top: 0.85rem;
  }

  .report-config-summary {
    display: flex;
    gap: 1rem;
    align-items: center;
    justify-content: space-between;
    padding: 0.85rem;
  }

  .report-config-summary h2 {
    margin: 0;
    color: var(--ink);
    font-size: 1rem;
  }

  .report-config-actions {
    display: inline-flex;
    flex-wrap: wrap;
    gap: 0.45rem;
    align-items: center;
    justify-content: flex-end;
  }

  .report-config-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-top: 0.25rem;
    color: var(--muted);
    font-size: 0.75rem;
    font-weight: 680;
  }

  .report-editor {
    display: grid;
    gap: 0.85rem;
    margin: 0 0.85rem 0.85rem;
    padding: 0.85rem;
    background: var(--panel-muted);
  }

  .report-editor-fields {
    display: grid;
    grid-template-columns: minmax(16rem, 24rem) auto;
    gap: 0.75rem;
    align-items: end;
  }

  .report-builder-layout {
    display: grid;
    grid-template-columns: 12rem minmax(0, 1fr);
    gap: 1rem;
    align-items: start;
  }

  .report-node-palette {
    display: grid;
    gap: 0.45rem;
  }

  .report-node-palette h3 {
    margin: 0 0 0.15rem;
    color: var(--muted);
    font-size: 0.75rem;
    font-weight: 760;
    text-transform: uppercase;
  }

  .report-node-template {
    border: 1px solid var(--line);
    border-radius: 0.45rem;
    background: var(--panel);
    color: var(--ink);
    cursor: grab;
    padding: 0.45rem 0.6rem;
    text-align: left;
    font-size: 0.8125rem;
    font-weight: 720;
  }

  .report-flow {
    position: relative;
    display: grid;
    gap: 0;
    padding-left: 1.4rem;
  }

  .report-flow::before {
    position: absolute;
    top: 0.9rem;
    bottom: 0.9rem;
    left: 0.45rem;
    width: 2px;
    background: color-mix(in srgb, var(--accent) 45%, var(--line));
    content: '';
  }

  .report-drop-target {
    min-height: 0.45rem;
  }

  .report-drop-target-end {
    min-height: 1.5rem;
  }

  .report-node-card {
    position: relative;
    display: grid;
    gap: 0.65rem;
    border: 1px solid var(--line);
    border-radius: 0.5rem;
    background: var(--panel);
    cursor: grab;
    padding: 0.75rem;
  }

  .report-node-card:active {
    cursor: grabbing;
  }

  .report-node-card input,
  .report-node-card select,
  .report-node-card button {
    cursor: auto;
  }

  .report-node-card::before {
    position: absolute;
    top: 1.05rem;
    left: -1.15rem;
    width: 0.65rem;
    height: 0.65rem;
    border: 2px solid var(--accent);
    border-radius: 999px;
    background: var(--panel);
    content: '';
  }

  .report-node-card-header {
    display: flex;
    gap: 0.75rem;
    align-items: center;
    justify-content: space-between;
  }

  .report-node-kind {
    color: var(--ink);
    font-size: 0.875rem;
    font-weight: 780;
  }

  .report-node-actions {
    display: inline-flex;
    flex-wrap: wrap;
    gap: 0.35rem;
    align-items: center;
    justify-content: flex-end;
  }

  .report-node-order-controls {
    display: inline-grid;
    grid-template-columns: repeat(2, max-content);
    gap: 0.22rem;
    align-items: center;
  }

  .report-node-move-button {
    display: inline-grid;
    width: 1.55rem;
    height: 1.55rem;
    place-items: center;
    border: 1px solid var(--line);
    border-radius: 0.35rem;
    background: var(--panel);
    color: var(--muted);
    cursor: pointer;
  }

  .report-node-delete-button {
    display: inline-grid;
    width: 1.55rem;
    height: 1.55rem;
    place-items: center;
    border: 1px solid var(--danger);
    border-radius: 0.35rem;
    background: var(--danger);
    color: white;
    cursor: pointer;
    box-shadow: 0 4px 10px color-mix(in srgb, var(--danger) 16%, transparent);
  }

  .report-node-move-button:hover,
  .report-node-move-button:focus-visible {
    border-color: var(--accent);
    color: var(--accent);
    outline: none;
  }

  .report-node-delete-button:hover,
  .report-node-delete-button:focus-visible {
    border-color: var(--danger-strong);
    background: var(--danger-strong);
    outline: none;
  }

  .report-node-move-button svg,
  .report-node-delete-button svg {
    width: 0.82rem;
    height: 0.82rem;
    fill: none;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 2;
  }

  .report-node-fields {
    display: grid;
    grid-template-columns: repeat(2, minmax(10rem, 1fr)) minmax(12rem, 0.75fr);
    gap: 0.7rem;
  }

  .report-orientation-field {
    margin: 0;
    min-width: 0;
    border: 0;
    padding: 0;
  }

  .report-segmented-control {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    min-height: 2.25rem;
    overflow: hidden;
    border: 1px solid var(--line);
    border-radius: 0.45rem;
    background: var(--panel);
  }

  .report-segmented-control label {
    position: relative;
    display: grid;
    min-width: 0;
    place-items: center;
  }

  .report-segmented-control label + label {
    border-left: 1px solid var(--line);
  }

  .report-segmented-control input {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip: rect(0 0 0 0);
    white-space: nowrap;
  }

  .report-segmented-control span {
    display: grid;
    width: 100%;
    min-width: 0;
    min-height: 2.25rem;
    place-items: center;
    padding: 0 0.45rem;
    color: var(--muted);
    font-size: 0.78rem;
    font-weight: 760;
    text-align: center;
  }

  .report-segmented-control input:checked + span {
    background: var(--accent);
    color: white;
  }

  .report-segmented-control input:focus-visible + span {
    outline: 2px solid var(--focus-ring);
    outline-offset: -2px;
  }

  .report-valuation-display-options {
    display: flex;
    flex-wrap: wrap;
    gap: 0.6rem;
    align-items: center;
    border-top: 1px solid color-mix(in srgb, var(--line) 70%, transparent);
    padding-top: 0.7rem;
  }

  .report-valuation-colour-option {
    display: inline-flex;
    gap: 0.4rem;
    align-items: center;
    border: 1px solid var(--line);
    border-radius: 0.45rem;
    background: var(--panel);
    color: var(--ink);
    cursor: pointer;
    padding: 0.38rem 0.55rem;
    font-size: 0.78rem;
    font-weight: 720;
  }

  .report-valuation-colour-option input {
    width: 0.95rem;
    height: 0.95rem;
    accent-color: var(--accent);
  }

  .report-valuation-columns {
    display: grid;
    grid-template-columns: minmax(10rem, 13rem) minmax(0, 1fr);
    gap: 0.75rem;
    align-items: start;
    border-top: 1px solid color-mix(in srgb, var(--line) 70%, transparent);
    padding-top: 0.7rem;
  }

  .report-column-source,
  .report-column-selected {
    display: grid;
    gap: 0.45rem;
    min-width: 0;
  }

  .report-column-source h4,
  .report-column-selected h4 {
    margin: 0;
    color: var(--muted);
    font-size: 0.72rem;
    font-weight: 760;
    text-transform: uppercase;
  }

  .report-column-source-list {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
  }

  .report-column-selected-list {
    display: flex;
    min-height: 2.35rem;
    flex-wrap: wrap;
    gap: 0.35rem 0.25rem;
    align-items: center;
    border: 1px dashed color-mix(in srgb, var(--line) 85%, transparent);
    border-radius: 0.5rem;
    background: color-mix(in srgb, var(--panel) 72%, var(--panel-muted));
    padding: 0.35rem;
  }

  .report-column-pill {
    --column-pill-background: var(--panel);
    --column-pill-border: var(--line);
    --column-pill-text: var(--ink);

    display: inline-flex;
    flex: 0 0 auto;
    gap: 0.35rem;
    align-items: center;
    max-width: 12rem;
    border: 1px solid var(--column-pill-border);
    border-radius: 999px;
    background: var(--column-pill-background);
    color: var(--column-pill-text);
    cursor: grab;
    padding: 0.28rem 0.55rem;
    font-size: 0.76rem;
    font-weight: 720;
    white-space: nowrap;
  }

  .report-column-pill:active {
    cursor: grabbing;
  }

  .report-column-fixed-pill,
  .report-column-fixed-pill:active {
    cursor: default;
  }

  .report-column-source-pill,
  .report-column-selected-pill {
    box-shadow: inset 0 0 0 1px color-mix(in srgb, white 34%, transparent);
  }

  .report-column-pill-valuation {
    --column-pill-background: color-mix(in srgb, #dbeafe 76%, var(--panel));
    --column-pill-border: color-mix(in srgb, #2563eb 58%, var(--line));
    --column-pill-text: #1e3a8a;
  }

  .report-column-pill-asset-allocation {
    --column-pill-background: color-mix(in srgb, #fce7f3 76%, var(--panel));
    --column-pill-border: color-mix(in srgb, #db2777 58%, var(--line));
    --column-pill-text: #9d174d;
  }

  .report-column-delete {
    display: inline-grid;
    width: 1.05rem;
    height: 1.05rem;
    place-items: center;
    border: 0;
    border-radius: 999px;
    background: color-mix(in srgb, currentColor 12%, transparent);
    color: currentColor;
    cursor: pointer;
    font-size: 0.8rem;
    font-weight: 800;
    line-height: 1;
    padding: 0;
  }

  .report-column-delete:hover,
  .report-column-delete:focus-visible {
    background: color-mix(in srgb, var(--danger) 14%, transparent);
    color: var(--danger);
    outline: none;
  }

  .report-column-empty {
    color: var(--muted);
    font-size: 0.78rem;
    font-weight: 680;
    padding: 0 0.25rem;
  }

  .report-node-footnote {
    color: var(--muted);
    font-size: 0.75rem;
    font-weight: 650;
  }

  .report-flow-empty {
    border: 1px dashed var(--line);
    border-radius: 0.5rem;
    padding: 1rem;
    color: var(--muted);
    font-size: 0.875rem;
    font-weight: 700;
    text-align: center;
  }

  .success-panel {
    margin-top: 0.75rem;
    border: 1px solid color-mix(in srgb, var(--success) 60%, var(--line));
    border-radius: 0.5rem;
    background: color-mix(in srgb, var(--success-soft) 55%, var(--panel));
    padding: 0.75rem;
    color: var(--success-strong);
    font-weight: 720;
  }

  @media (max-width: 820px) {
    .report-tools-toolbar,
    .report-tools-filter,
    .report-editor-fields,
    .report-builder-layout,
    .report-node-fields,
    .report-valuation-columns {
      grid-template-columns: minmax(0, 1fr);
    }

    .report-new-form {
      justify-content: flex-end;
    }
  }
</style>
