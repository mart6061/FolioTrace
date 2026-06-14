<script lang="ts">
  import { SvelteSet } from 'svelte/reactivity';
  import type { AssetAllocationNode, HoldingPosition } from '$lib/types';

  const DEFAULT_NODE_COLOUR = '#0f766e';
  const SPECIAL_NODE_NAME = 'Unallocated';
  const SPECIAL_NODE_COLOUR = '#dc2626';

  type TreeRow = {
    depth: number;
    effectiveColour: string | null;
    node: AssetAllocationNode;
  };

  let {
    holdingsByNodeID = {},
    nodes,
    onHoldingDragStart,
    onHoldingDrop,
    rootNodeID
  }: {
    holdingsByNodeID?: Record<string, HoldingPosition[]>;
    nodes: AssetAllocationNode[];
    onHoldingDragStart?: (holdingID: string) => void;
    onHoldingDrop?: (nodeID: string) => void;
    rootNodeID: string;
  } = $props();

  let dragOverNodeID = $state('');

  const rows = $derived.by(() => buildRows(nodes, rootNodeID));

  function buildRows(sourceNodes: AssetAllocationNode[], rootID: string) {
    const byID = new Map(sourceNodes.map((node) => [node.nodeID, node]));
    const root = byID.get(rootID);
    const nextRows: TreeRow[] = [];

    if (!root)
      return nextRows;

    appendRows(root, 0, null, byID, new Set<string>(), nextRows);
    return nextRows;
  }

  function appendRows(node: AssetAllocationNode, depth: number, inheritedColour: string | null, byID: Map<string, AssetAllocationNode>, path: Set<string>, nextRows: TreeRow[]) {
    if (path.has(node.nodeID))
      return;

    const special = isSpecialNode(node);
    const nodeColour = special ? SPECIAL_NODE_COLOUR : isHexColour(node.colour) ? node.colour : null;
    const effectiveColour = special ? SPECIAL_NODE_COLOUR : inheritedColour ?? nodeColour;
    nextRows.push({ depth, effectiveColour, node });

    const nextPath = new SvelteSet(path);
    nextPath.add(node.nodeID);

    for (const childNodeID of node.nodes) {
      const childNode = byID.get(childNodeID);

      if (childNode)
        appendRows(childNode, depth + 1, special ? inheritedColour : effectiveColour, byID, nextPath, nextRows);
    }
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

  function isSpecialNode(node: AssetAllocationNode) {
    return node.name.trim().toLocaleLowerCase() === SPECIAL_NODE_NAME.toLocaleLowerCase();
  }

  function displayColour(colour: string | null | undefined) {
    return isHexColour(colour) ? colour : DEFAULT_NODE_COLOUR;
  }

  function hasColour(colour: string | null | undefined) {
    return isHexColour(colour);
  }

  function isHexColour(colour: string | null | undefined): colour is string {
    return typeof colour === 'string' && /^#[0-9a-fA-F]{6}$/.test(colour);
  }
</script>

<div class="allocation-tree" role="tree">
  {#each rows as row (row.node.nodeID)}
    {@const nodeHoldings = holdingsByNodeID[row.node.nodeID] ?? []}
    <section class="allocation-tree-row" style={`--node-colour: ${displayColour(row.effectiveColour)}; margin-left: ${row.depth * 1.25}rem`}>
      <div
        aria-level={row.depth + 1}
        aria-selected="false"
        class={`allocation-node-pill ${!hasColour(row.effectiveColour) ? 'allocation-node-pill-none' : ''} ${isSpecialNode(row.node) ? 'allocation-node-pill-special' : ''}`}
        role="treeitem"
      >
        <span class="allocation-node-title" title={isSpecialNode(row.node) ? SPECIAL_NODE_NAME : row.node.name}>
          {isSpecialNode(row.node) ? SPECIAL_NODE_NAME : row.node.name}
        </span>
      </div>

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
    </section>
  {:else}
    <div class="allocation-tree-message">Root node is missing.</div>
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
    border: 1px solid color-mix(in srgb, var(--node-colour) 58%, var(--line));
    border-radius: 999px;
    background: color-mix(in srgb, var(--node-colour) 15%, var(--panel));
    padding: 0.2rem 0.75rem;
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
