<script lang="ts">
  import { SvelteSet } from 'svelte/reactivity';
  import type { Account, AssetAllocationNode, AssetAllocationNodeAccountSetting, HoldingPosition } from '$lib/types';

  const DEFAULT_NODE_COLOUR = '#0f766e';
  const SPECIAL_NODE_NAME = 'Unallocated';
  const SPECIAL_NODE_COLOUR = '#dc2626';

  type TreeRow = {
    depth: number;
    effectiveColour: string | null;
    hasChildren: boolean;
    node: AssetAllocationNode;
  };

  let {
    accounts = [],
    holdingsByNodeID = {},
    nodes,
    onHoldingDragStart,
    onHoldingDrop,
    rootNodeID
  }: {
    accounts?: Account[];
    holdingsByNodeID?: Record<string, HoldingPosition[]>;
    nodes: AssetAllocationNode[];
    onHoldingDragStart?: (holdingID: string) => void;
    onHoldingDrop?: (nodeID: string) => void;
    rootNodeID: string;
  } = $props();

  let dragOverNodeID = $state('');
  let collapsedNodeIDs = $state<string[]>([]);
  let collapsedAccountNodeIDs = $state<string[]>(initialCollapsedAccountNodeIDs());

  const rows = $derived.by(() => buildRows(nodes, rootNodeID, collapsedNodeIDs));

  function buildRows(sourceNodes: AssetAllocationNode[], rootID: string, collapsedIDs: string[]) {
    const byID = new Map(sourceNodes.map((node) => [node.nodeID, node]));
    const root = byID.get(rootID);
    const nextRows: TreeRow[] = [];

    if (root) {
      for (const childNodeID of root.nodes) {
        const childNode = byID.get(childNodeID);

        if (childNode)
          appendRows(childNode, 0, null, byID, new Set<string>(), nextRows, collapsedIDs);
      }

      return nextRows;
    }

    for (const topLevelNode of topLevelNodes(sourceNodes))
      appendRows(topLevelNode, 0, null, byID, new Set<string>(), nextRows, collapsedIDs);

    return nextRows;
  }

  function appendRows(node: AssetAllocationNode, depth: number, inheritedColour: string | null, byID: Map<string, AssetAllocationNode>, path: Set<string>, nextRows: TreeRow[], collapsedIDs: string[]) {
    if (path.has(node.nodeID))
      return;

    const special = isSpecialNode(node);
    const nodeColour = special ? SPECIAL_NODE_COLOUR : isHexColour(node.colour) ? node.colour : null;
    const effectiveColour = special ? SPECIAL_NODE_COLOUR : inheritedColour ?? nodeColour;
    const hasChildren = hasVisibleChildren(node, byID);
    nextRows.push({ depth, effectiveColour, hasChildren, node });

    if (hasChildren && collapsedIDs.includes(node.nodeID))
      return;

    const nextPath = new SvelteSet(path);
    nextPath.add(node.nodeID);

    for (const childNodeID of node.nodes) {
      const childNode = byID.get(childNodeID);

      if (childNode)
        appendRows(childNode, depth + 1, special ? inheritedColour : effectiveColour, byID, nextPath, nextRows, collapsedIDs);
    }
  }

  function toggleNodeCollapsed(nodeID: string) {
    collapsedNodeIDs = collapsedNodeIDs.includes(nodeID)
      ? collapsedNodeIDs.filter((collapsedNodeID) => collapsedNodeID !== nodeID)
      : [...collapsedNodeIDs, nodeID];
  }

  function toggleAccountNodeCollapsed(nodeID: string) {
    collapsedAccountNodeIDs = collapsedAccountNodeIDs.includes(nodeID)
      ? collapsedAccountNodeIDs.filter((collapsedNodeID) => collapsedNodeID !== nodeID)
      : [...collapsedAccountNodeIDs, nodeID];
  }

  function startHoldingDrag(event: DragEvent, holdingID: string) {
    event.dataTransfer?.setData('text/plain', holdingID);

    if (event.dataTransfer)
      event.dataTransfer.effectAllowed = 'move';

    onHoldingDragStart?.(holdingID);
  }

  function allowHoldingDrop(event: DragEvent, node: AssetAllocationNode) {
    if (!isLeaf(node))
      return;

    event.preventDefault();
    dragOverNodeID = node.nodeID;

    if (event.dataTransfer)
      event.dataTransfer.dropEffect = 'move';
  }

  function dropHolding(event: DragEvent, node: AssetAllocationNode) {
    event.preventDefault();
    dragOverNodeID = '';

    if (!isLeaf(node))
      return;

    onHoldingDrop?.(node.nodeID);
  }

  function endDropHover() {
    dragOverNodeID = '';
  }

  function isLeaf(node: AssetAllocationNode) {
    return node.nodes.length === 0;
  }

  function hasVisibleChildren(node: AssetAllocationNode, byID: Map<string, AssetAllocationNode>) {
    return node.nodes.some((childNodeID) => byID.has(childNodeID));
  }

  function accountTargetsForNode(node: AssetAllocationNode) {
    const settingByAccountID = new Map(node.accountSettings.map((setting) => [setting.accountID, setting]));
    return accounts.map((account) => ({
      account,
      setting: settingByAccountID.get(account.accountID) ?? defaultAccountSetting(account.accountID)
    }));
  }

  function initialCollapsedAccountNodeIDs() {
    return defaultCollapsedAccountNodeIDs(nodes);
  }

  function defaultCollapsedAccountNodeIDs(sourceNodes: AssetAllocationNode[]) {
    return sourceNodes
      .filter((node) => !isSpecialNode(node))
      .map((node) => node.nodeID);
  }

  function defaultAccountSetting(accountID: string): AssetAllocationNodeAccountSetting {
    return {
      accountID,
      targetWeight: null,
      targetWeightMax: null,
      targetWeightMin: null,
      targetYield: null
    };
  }

  function isSpecialNode(node: AssetAllocationNode) {
    return node.name.trim().toLocaleLowerCase() === SPECIAL_NODE_NAME.toLocaleLowerCase();
  }

  function topLevelNodes(sourceNodes: AssetAllocationNode[]) {
    const childNodeIDs = new SvelteSet(sourceNodes.flatMap((node) => node.nodes));
    return sourceNodes.filter((node) => !childNodeIDs.has(node.nodeID));
  }

  function displayColour(colour: string | null | undefined) {
    return isHexColour(colour) ? colour : DEFAULT_NODE_COLOUR;
  }

  function hasColour(colour: string | null | undefined) {
    return isHexColour(colour);
  }

  function formatPercent(value: number | null) {
    return value === null ? '' : `${(value * 100).toFixed(2)}%`;
  }

  function isHexColour(colour: string | null | undefined): colour is string {
    return typeof colour === 'string' && /^#[0-9a-fA-F]{6}$/.test(colour);
  }
</script>

<div class="allocation-tree" role="tree">
  {#each rows as row (row.node.nodeID)}
    {@const nodeHoldings = holdingsByNodeID[row.node.nodeID] ?? []}
    {@const nodeAccountTargets = accountTargetsForNode(row.node)}
    {@const hideEmptySpecialNode = isSpecialNode(row.node) && nodeHoldings.length === 0}
    {#if !hideEmptySpecialNode}
      <section class="allocation-tree-row" style={`--node-colour: ${displayColour(row.effectiveColour)}; margin-left: ${row.depth * 1.25}rem`}>
        <div
          aria-level={row.depth + 1}
          aria-selected="false"
          class={`allocation-node-pill ${!hasColour(row.effectiveColour) ? 'allocation-node-pill-none' : ''} ${isSpecialNode(row.node) ? 'allocation-node-pill-special' : ''}`}
          role="treeitem"
        >
          {#if row.hasChildren}
            <button
              aria-expanded={!collapsedNodeIDs.includes(row.node.nodeID)}
              aria-label={`${collapsedNodeIDs.includes(row.node.nodeID) ? 'Expand' : 'Collapse'} ${isSpecialNode(row.node) ? SPECIAL_NODE_NAME : row.node.name}`}
              class="allocation-node-expand-button"
              onclick={() => toggleNodeCollapsed(row.node.nodeID)}
              title={collapsedNodeIDs.includes(row.node.nodeID) ? 'Expand' : 'Collapse'}
              type="button"
            >
              <svg aria-hidden="true" class:allocation-node-expand-open={!collapsedNodeIDs.includes(row.node.nodeID)} viewBox="0 0 24 24"><path d="m9 6 6 6-6 6" /></svg>
            </button>
          {/if}
          <span class="allocation-node-title" title={isSpecialNode(row.node) ? SPECIAL_NODE_NAME : row.node.name}>
            {isSpecialNode(row.node) ? SPECIAL_NODE_NAME : row.node.name}
          </span>
        </div>

        {#if (!isSpecialNode(row.node) && nodeAccountTargets.length) || isLeaf(row.node)}
          <div class="allocation-leaf-detail">
            {#if !isSpecialNode(row.node) && nodeAccountTargets.length}
              <section class="allocation-account-targets">
                <button
                  aria-expanded={!collapsedAccountNodeIDs.includes(row.node.nodeID)}
                  class="allocation-account-target-toggle"
                  onclick={() => toggleAccountNodeCollapsed(row.node.nodeID)}
                  type="button"
                >
                  <svg aria-hidden="true" class:allocation-account-target-open={!collapsedAccountNodeIDs.includes(row.node.nodeID)} viewBox="0 0 24 24"><path d="m9 6 6 6-6 6" /></svg>
                  <span>Metrics</span>
                </button>

                {#if !collapsedAccountNodeIDs.includes(row.node.nodeID)}
                  <div class="allocation-account-target-grid">
                    <div class="allocation-account-target-header">Target</div>
                    <div class="allocation-account-target-header">Min</div>
                    <div class="allocation-account-target-header">Max</div>
                    <div class="allocation-account-target-header">Yield</div>
                    {#each nodeAccountTargets as item (item.account.accountID)}
                      <div class="allocation-account-target-account" title={item.account.formalName}>{item.account.name}</div>
                      <div>{formatPercent(item.setting.targetWeight)}</div>
                      <div>{formatPercent(item.setting.targetWeightMin)}</div>
                      <div>{formatPercent(item.setting.targetWeightMax)}</div>
                      <div>{formatPercent(item.setting.targetYield)}</div>
                    {/each}
                  </div>
                {/if}
              </section>
            {/if}

            {#if isLeaf(row.node)}
              <div
                aria-label={`${isSpecialNode(row.node) ? SPECIAL_NODE_NAME : row.node.name} holdings`}
                class={`allocation-holding-drop-zone ${dragOverNodeID === row.node.nodeID ? 'allocation-holding-drop-zone-active' : ''}`}
                ondragover={(event) => allowHoldingDrop(event, row.node)}
                ondragleave={endDropHover}
                ondrop={(event) => dropHolding(event, row.node)}
                role="group"
              >
                {#each nodeHoldings as holding (holding.holdingID)}
                  <button
                    class="allocation-holding"
                    draggable="true"
                    ondragend={endDropHover}
                    ondragstart={(event) => startHoldingDrag(event, holding.holdingID)}
                    type="button"
                  >
                    <span class="allocation-holding-name">{holding.name}</span>
                    <span class="allocation-holding-meta">{holding.instrumentName}</span>
                    <span class="allocation-holding-account" title={`Account: ${holding.accountName}`}>{holding.accountName}</span>
                  </button>
                {:else}
                  <div class="allocation-empty-holdings">No holdings</div>
                {/each}
              </div>
            {/if}
          </div>
        {/if}
      </section>
    {/if}
  {:else}
    <div class="allocation-tree-message">No allocation nodes configured.</div>
  {/each}
</div>

<style>
  .allocation-tree {
    display: grid;
    gap: 0.4rem;
    overflow-x: auto;
    border: 1px solid var(--line);
    border-radius: 0.5rem;
    background: color-mix(in srgb, var(--panel-muted) 54%, var(--panel));
    padding: 0.75rem;
  }

  .allocation-tree-row {
    display: grid;
    grid-template-columns: 24rem minmax(20rem, 1fr);
    gap: 0.5rem;
    align-items: start;
    min-width: 56rem;
  }

  .allocation-node-pill {
    display: inline-flex;
    min-height: 2rem;
    align-items: center;
    gap: 0.25rem;
    border: 1px solid color-mix(in srgb, var(--node-colour) 58%, var(--line));
    border-radius: 999px;
    background: color-mix(in srgb, var(--node-colour) 15%, var(--panel));
    padding: 0.2rem 0.75rem;
  }

  .allocation-node-expand-button {
    display: inline-grid;
    width: 1.35rem;
    height: 1.35rem;
    flex: 0 0 auto;
    place-items: center;
    border: 1px solid color-mix(in srgb, var(--node-colour) 42%, var(--line));
    border-radius: 999px;
    background: color-mix(in srgb, var(--node-colour) 10%, var(--panel));
    color: color-mix(in srgb, var(--node-colour) 78%, var(--ink));
    cursor: pointer;
    padding: 0;
  }

  .allocation-node-expand-button:hover,
  .allocation-node-expand-button:focus-visible {
    border-color: color-mix(in srgb, var(--node-colour) 68%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 18%, var(--panel));
    outline: none;
  }

  .allocation-node-expand-button svg {
    width: 0.9rem;
    height: 0.9rem;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.5;
    stroke-linecap: round;
    stroke-linejoin: round;
    transition: transform 140ms ease;
  }

  .allocation-node-expand-open {
    transform: rotate(90deg);
  }

  .allocation-node-pill-none {
    border-color: var(--line);
    background: var(--panel);
  }

  .allocation-node-pill-special {
    border-color: color-mix(in srgb, var(--node-colour) 72%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 18%, var(--panel));
  }

  .allocation-node-title {
    min-width: 0;
    overflow: hidden;
    color: var(--ink);
    font-size: 0.86rem;
    font-weight: 750;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .allocation-leaf-detail {
    display: grid;
    gap: 0.4rem;
    min-width: 0;
  }

  .allocation-account-targets {
    display: grid;
    gap: 0.35rem;
    border: 1px solid color-mix(in srgb, var(--node-colour) 28%, var(--line));
    border-radius: 0.375rem;
    background: color-mix(in srgb, var(--node-colour) 5%, var(--panel));
    padding: 0.35rem;
  }

  .allocation-account-target-toggle {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    justify-self: start;
    border: 0;
    background: transparent;
    color: color-mix(in srgb, var(--node-colour) 78%, var(--ink));
    cursor: pointer;
    font-size: 0.76rem;
    font-weight: 780;
    padding: 0;
  }

  .allocation-account-target-toggle svg {
    width: 0.85rem;
    height: 0.85rem;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.5;
    stroke-linecap: round;
    stroke-linejoin: round;
    transition: transform 140ms ease;
  }

  .allocation-account-target-open {
    transform: rotate(90deg);
  }

  .allocation-account-target-grid {
    display: grid;
    grid-template-columns: minmax(9rem, 1fr) repeat(4, minmax(4.5rem, max-content));
    gap: 0.2rem 0.55rem;
    align-items: center;
    color: var(--muted);
    font-size: 0.72rem;
    font-weight: 650;
  }

  .allocation-account-target-grid > .allocation-account-target-header:first-child {
    grid-column: 2;
  }

  .allocation-account-target-header {
    color: var(--ink);
    font-size: 0.68rem;
    font-weight: 800;
    text-align: right;
  }

  .allocation-account-target-account {
    min-width: 0;
    overflow: hidden;
    color: var(--ink);
    font-weight: 500;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .allocation-holding-drop-zone {
    display: flex;
    min-height: 2.15rem;
    flex-wrap: wrap;
    gap: 0.35rem;
    border: 1px dashed color-mix(in srgb, var(--node-colour) 34%, var(--line));
    border-radius: 0.375rem;
    background: color-mix(in srgb, var(--node-colour) 6%, var(--panel));
    padding: 0.3rem;
  }

  .allocation-holding-drop-zone-active {
    border-color: var(--node-colour);
    box-shadow: inset 0 0 0 2px color-mix(in srgb, var(--node-colour) 18%, transparent);
  }

  .allocation-holding {
    display: grid;
    min-width: 12rem;
    max-width: 18rem;
    gap: 0.1rem;
    border: 1px solid var(--line);
    border-radius: 0.375rem;
    background: var(--panel);
    padding: 0.35rem 0.45rem;
    color: var(--ink);
    cursor: grab;
    text-align: left;
  }

  .allocation-holding:active {
    cursor: grabbing;
  }

  .allocation-holding-name {
    overflow: hidden;
    font-size: 0.78rem;
    font-weight: 750;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .allocation-holding-meta,
  .allocation-holding-account {
    color: var(--muted);
    font-size: 0.68rem;
    line-height: 1.2;
  }

  .allocation-holding-account {
    overflow: hidden;
    font-weight: 650;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .allocation-empty-holdings,
  .allocation-tree-message {
    color: var(--muted);
    font-size: 0.8rem;
    font-weight: 650;
  }

  @media (max-width: 900px) {
    .allocation-tree-row {
      grid-template-columns: minmax(18rem, 1fr);
      min-width: 30rem;
    }
  }
</style>
