<script lang="ts">
  import AssetAllocationReadOnlyTree from '$lib/components/AssetAllocationReadOnlyTree.svelte';
  import type { AssetAllocationMapping, AssetAllocationNode, HoldingPosition, ValuationSetting } from '$lib/types';
  import { SvelteSet } from 'svelte/reactivity';

  let {
    accountIDs,
    assetAllocationMappings,
    auditDateTime,
    holdingPositions,
    mappingEventDateTime,
    valuationSetting
  }: {
    accountIDs: string[];
    assetAllocationMappings: AssetAllocationMapping[];
    auditDateTime: string;
    holdingPositions: HoldingPosition[];
    mappingEventDateTime: string;
    valuationSetting: ValuationSetting;
  } = $props();

  let draggedHoldingID = $state('');
  let draftOverrides = $state<Record<string, string>>({});
  let dirtyHoldingIDs = $state<string[]>([]);

  const selectedNodes = $derived(valuationSetting.nodes);
  const rootNodeID = $derived(valuationSetting.rootNodeID);
  const unallocatedNodeID = $derived(findUnallocatedNodeID(selectedNodes, rootNodeID));
  const mappableNodeIDs = $derived(findMappableNodeIDs(selectedNodes, rootNodeID));
  const existingNodeByHoldingID = $derived(new Map(assetAllocationMappings.map((mapping) => [mapping.holdingID, mapping.nodeID])));
  const orphanedMappedHoldingIDs = $derived(
    holdingPositions
      .filter((holding) => {
        const existingNodeID = existingNodeByHoldingID.get(holding.holdingID);
        return !!existingNodeID && !isMappableNodeID(existingNodeID);
      })
      .map((holding) => holding.holdingID)
  );
  const dirtyMappings = $derived(
    uniqueHoldingIDs([...dirtyHoldingIDs, ...orphanedMappedHoldingIDs])
      .map((holdingID) => ({ holdingID, nodeID: nodeIDForHolding(holdingID) }))
      .filter((mapping) => mapping.nodeID)
  );
  const holdingsByNodeID = $derived(groupHoldingsByNode(holdingPositions));
  const saveDisabled = $derived(!accountIDs.length || dirtyMappings.length === 0);

  function startHoldingDrag(holdingID: string) {
    draggedHoldingID = holdingID;
  }

  function dropHolding(nodeID: string) {
    if (!draggedHoldingID)
      return;

    const holdingID = draggedHoldingID;
    draggedHoldingID = '';

    if (nodeIDForHolding(holdingID) === nodeID)
      return;

    draftOverrides = {
      ...draftOverrides,
      [holdingID]: nodeID
    };

    if (!dirtyHoldingIDs.includes(holdingID))
      dirtyHoldingIDs = [...dirtyHoldingIDs, holdingID];
  }

  function nodeIDForHolding(holdingID: string) {
    const draftNodeID = draftOverrides[holdingID];
    if (draftNodeID)
      return draftNodeID;

    const existingNodeID = existingNodeByHoldingID.get(holdingID);
    if (existingNodeID && isMappableNodeID(existingNodeID))
      return existingNodeID;

    return unallocatedNodeID;
  }

  function groupHoldingsByNode(holdings: HoldingPosition[]) {
    const grouped: Record<string, HoldingPosition[]> = {};

    for (const holding of holdings) {
      const nodeID = nodeIDForHolding(holding.holdingID);

      if (!nodeID)
        continue;

      grouped[nodeID] = [...(grouped[nodeID] ?? []), holding];
    }

    for (const nodeHoldings of Object.values(grouped))
      nodeHoldings.sort((left, right) => left.name.localeCompare(right.name));

    return grouped;
  }

  function findUnallocatedNodeID(nodes: AssetAllocationNode[], rootID: string) {
    const root = nodes.find((node) => node.nodeID === rootID);
    const firstChild = root?.nodes[0];

    if (firstChild && nodes.some((node) => node.nodeID === firstChild))
      return firstChild;

    return nodes.find((node) => node.name.trim().toLocaleLowerCase() === 'unallocated')?.nodeID ?? '';
  }

  function findMappableNodeIDs(nodes: AssetAllocationNode[], rootID: string) {
    const byID = new Map(nodes.map((node) => [node.nodeID, node]));
    const root = byID.get(rootID);
    const mappable = new SvelteSet<string>();

    if (!root)
      return mappable;

    visitReachableLeafNodes(root, byID, new SvelteSet<string>(), mappable);
    return mappable;
  }

  function visitReachableLeafNodes(node: AssetAllocationNode, byID: Map<string, AssetAllocationNode>, path: SvelteSet<string>, mappable: SvelteSet<string>) {
    if (path.has(node.nodeID))
      return;

    const nextPath = new SvelteSet(path);
    nextPath.add(node.nodeID);

    if (node.nodes.length === 0) {
      mappable.add(node.nodeID);
      return;
    }

    for (const childNodeID of node.nodes) {
      const childNode = byID.get(childNodeID);

      if (childNode)
        visitReachableLeafNodes(childNode, byID, nextPath, mappable);
    }
  }

  function isMappableNodeID(nodeID: string) {
    return mappableNodeIDs.has(nodeID);
  }

  function uniqueHoldingIDs(holdingIDs: string[]) {
    return [...new SvelteSet(holdingIDs)];
  }
</script>

<form action="?/saveMappings" class="grid gap-4 rounded-md border border-slate-200 bg-white p-4 shadow-sm" method="POST">
  {#each accountIDs as accountID (accountID)}
    <input name="accountIDs" type="hidden" value={accountID} />
  {/each}
  <input name="assetAllocationID" type="hidden" value={valuationSetting.assetAllocationID} />
  <input name="eventDateTime" type="hidden" value={mappingEventDateTime} />
  <input name="auditDateTime" type="hidden" value={auditDateTime} />
  <input name="mappingsJson" type="hidden" value={JSON.stringify(dirtyMappings)} />

  <div class="flex flex-wrap items-center justify-between gap-3">
    <div>
      <h2 class="text-base font-semibold text-slate-950">{valuationSetting.name}</h2>
      <p class="text-sm text-slate-600">{holdingPositions.length} holdings</p>
    </div>

    <button class="h-9 rounded-md bg-teal-700 px-4 text-sm font-semibold text-white shadow-sm hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-slate-300" disabled={saveDisabled} type="submit">Save</button>
  </div>

  {#if unallocatedNodeID}
    <AssetAllocationReadOnlyTree
      holdingsByNodeID={holdingsByNodeID}
      nodes={selectedNodes}
      onHoldingDragStart={startHoldingDrag}
      onHoldingDrop={dropHolding}
      {rootNodeID}
    />
  {:else}
    <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">This allocation does not have an Unallocated node.</div>
  {/if}
</form>
