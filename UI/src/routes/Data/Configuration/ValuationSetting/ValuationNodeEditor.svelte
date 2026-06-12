<script lang="ts">
  import { SvelteMap, SvelteSet } from 'svelte/reactivity';
  import type { Account, AssetAllocationNode, AssetAllocationNodeAccountSetting } from '$lib/types';

  const DEFAULT_NODE_COLOUR = '#0f766e';
  const SPECIAL_NODE_NAME = 'Unallocated';
  const SPECIAL_NODE_COLOUR = '#dc2626';

  type EditableNode = AssetAllocationNode & { colour?: string | null };
  type NodeRow = { depth: number; effectiveColour: string | null; node: EditableNode };
  type ParseResult = { message: string; nodes: EditableNode[] };

  let {
    accounts,
    allocationAccountIDs,
    nodesJson = $bindable(''),
    rootNodeID,
    rootNodeName
  }: {
    accounts: Account[];
    allocationAccountIDs: string[];
    nodesJson: string;
    rootNodeID: string;
    rootNodeName: string;
  } = $props();

  let editorMessage = $state('');
  let draggedNodeID = $state('');
  let dragOverNodeID = $state('');
  let nodes = $state<EditableNode[]>([]);

  const allocationAccounts = $derived(accounts.filter((account) => allocationAccountIDs.includes(account.accountID)));
  const specialNodeID = $derived(specialNodeIDFor(nodes));
  const initialParse = parseNodesJson(nodesJson);
  const initialNodes = initialParse.message ? initialParse.nodes : normaliseNodes(initialParse.nodes);
  const metricsNodeIDs = new SvelteSet(initialNodes.filter(hasMetricValues).map((node) => node.nodeID));
  editorMessage = initialParse.message || validateNodes(initialNodes);
  nodes = initialNodes;

  if (!initialParse.message)
    nodesJson = JSON.stringify(initialNodes.map(toJsonNode), null, 2);

  const rows = $derived.by(() => buildRows(nodes, rootNodeID));

  function buildRows(sourceNodes: EditableNode[], rootID: string) {
    const byID = nodeMap(sourceNodes);
    const root = byID.get(rootID);
    const nextRows: NodeRow[] = [];

    if (!root)
      return nextRows;

    appendRows(root, 0, null, byID, new SvelteSet<string>(), nextRows);
    return nextRows;
  }

  function appendRows(node: EditableNode, depth: number, inheritedColour: string | null, byID: Map<string, EditableNode>, path: Set<string>, nextRows: NodeRow[]) {
    if (path.has(node.nodeID))
      return;

    const nodeColour = isHexColour(node.colour) ? node.colour : null;
    const effectiveColour = nodeColour ?? inheritedColour;
    nextRows.push({ depth, effectiveColour, node });

    const nextPath = new SvelteSet(path);
    nextPath.add(node.nodeID);

    for (const childNodeID of node.nodes) {
      const childNode = byID.get(childNodeID);

      if (childNode)
        appendRows(childNode, depth + 1, effectiveColour, byID, nextPath, nextRows);
    }
  }

  function parseNodesJson(value: string, fallbackNodes: EditableNode[] = []): ParseResult {
    try {
      const parsed = JSON.parse(value) as unknown;

      if (!Array.isArray(parsed))
        return { message: 'Node JSON must be an array.', nodes: [] };

      const parsedNodes = parsed.map(readNode);

      if (parsedNodes.some((node) => node === null))
        return { message: 'Every node requires nodeID, nodes, name, subtotal, hidden, and accountSettings fields.', nodes: [] };

      return { message: '', nodes: parsedNodes as EditableNode[] };
    } catch {
      return { message: 'Node JSON is not valid JSON.', nodes: fallbackNodes };
    }
  }

  function readNode(value: unknown): EditableNode | null {
    if (!value || typeof value !== 'object')
      return null;

    const record = value as Record<string, unknown>;
    const nodeID = readString(record, 'nodeID', 'NodeID');
    const name = readString(record, 'name', 'Name');
    const childNodes = readStringArray(record, 'nodes', 'Nodes');
    const accountSettings = readAccountSettings(record);

    if (!nodeID || !name || accountSettings === null)
      return null;

    return {
      accountSettings,
      colour: readColour(record),
      hidden: readBoolean(record, 'hidden', 'Hidden'),
      name,
      nodeID,
      nodes: childNodes,
      subtotal: readBoolean(record, 'subtotal', 'Subtotal')
    };
  }

  function rawJsonChanged(event: Event) {
    nodesJson = (event.currentTarget as HTMLTextAreaElement).value;
    const result = parseNodesJson(nodesJson, nodes);

    if (!result.message) {
      const normalisedNodes = normaliseNodes(result.nodes);
      nodes = normalisedNodes;
      nodesJson = JSON.stringify(normalisedNodes.map(toJsonNode), null, 2);
      editorMessage = validateNodes(normalisedNodes);
      return;
    }

    editorMessage = result.message || validateNodes(result.nodes);
  }

  function commitVisualNodes(nextNodes: EditableNode[]) {
    const normalisedNodes = normaliseNodes(nextNodes);
    nodes = normalisedNodes;
    editorMessage = validateNodes(normalisedNodes);
    nodesJson = JSON.stringify(normalisedNodes.map(toJsonNode), null, 2);
  }

  function addChild(parentNodeID: string) {
    if (isSpecialNodeID(parentNodeID))
      return;

    const nextNodes = cloneNodes(nodes);
    const parentNode = nextNodes.find((node) => node.nodeID === parentNodeID);

    if (!parentNode)
      return;

    const childNodeID = crypto.randomUUID();
    parentNode.nodes = [...parentNode.nodes, childNodeID];
    nextNodes.push({
      accountSettings: allocationAccounts.map((account) => defaultAccountSetting(account.accountID)),
      colour: null,
      hidden: false,
      name: 'New node',
      nodeID: childNodeID,
      nodes: [],
      subtotal: false
    });

    commitVisualNodes(nextNodes);
  }

  function isMetricsOpen(nodeID: string) {
    return metricsNodeIDs.has(nodeID);
  }

  function toggleMetrics(nodeID: string) {
    if (isSpecialNodeID(nodeID))
      return;

    if (metricsNodeIDs.has(nodeID)) {
      metricsNodeIDs.delete(nodeID);
      return;
    }

    metricsNodeIDs.add(nodeID);
  }

  function renameNode(nodeID: string, value: string) {
    if (isSpecialNodeID(nodeID))
      return;

    const nextNodes = cloneNodes(nodes);
    const node = nextNodes.find((item) => item.nodeID === nodeID);

    if (!node)
      return;

    node.name = value.trim() || 'Node';
    commitVisualNodes(nextNodes);
  }

  function setNodeColour(nodeID: string, value: string) {
    if (isSpecialNodeID(nodeID))
      return;

    setNodeExplicitColour(nodeID, safeColour(value), true);
  }

  function setNodeNoColour(nodeID: string, noColour: boolean, fallbackColour: string | null) {
    if (isSpecialNodeID(nodeID))
      return;

    setNodeExplicitColour(nodeID, noColour ? null : displayColour(fallbackColour), !noColour);
  }

  function setNodeExplicitColour(nodeID: string, colour: string | null, clearDescendantColours: boolean) {
    if (isSpecialNodeID(nodeID))
      return;

    const nextNodes = cloneNodes(nodes);
    const byID = nodeMap(nextNodes);
    const node = byID.get(nodeID);

    if (!node)
      return;

    node.colour = colour;

    if (!clearDescendantColours) {
      commitVisualNodes(nextNodes);
      return;
    }

    for (const colourNodeID of descendantIDs(nodeID, byID)) {
      const colourNode = byID.get(colourNodeID);

      if (colourNode)
        colourNode.colour = null;
    }

    commitVisualNodes(nextNodes);
  }

  function deleteNode(nodeID: string) {
    if (nodeID === rootNodeID || isSpecialNodeID(nodeID))
      return;

    const byID = nodeMap(nodes);
    const deleteIDs = descendantIDs(nodeID, byID);
    deleteIDs.add(nodeID);

    const nextNodes = cloneNodes(nodes)
      .filter((node) => !deleteIDs.has(node.nodeID))
      .map((node) => ({
        ...node,
        nodes: node.nodes.filter((childNodeID) => !deleteIDs.has(childNodeID))
      }));

    commitVisualNodes(nextNodes);
  }

  function setAccountSettingValue(nodeID: string, accountID: string, key: keyof Omit<AssetAllocationNodeAccountSetting, 'accountID'>, value: string) {
    if (isSpecialNodeID(nodeID))
      return;

    const nextNodes = cloneNodes(nodes);
    const node = nextNodes.find((item) => item.nodeID === nodeID);

    if (!node)
      return;

    const settings = settingsForNode(node);
    const setting = settings.find((item) => item.accountID === accountID);

    if (!setting)
      return;

    setting[key] = readFiniteNumber(value);
    node.accountSettings = settings;
    commitVisualNodes(nextNodes);
  }

  function startNodeDrag(event: DragEvent, nodeID: string) {
    if (nodeID === rootNodeID || isSpecialNodeID(nodeID)) {
      event.preventDefault();
      return;
    }

    draggedNodeID = nodeID;
    dragOverNodeID = nodeID;
    event.dataTransfer?.setData('text/plain', nodeID);

    if (event.dataTransfer)
      event.dataTransfer.effectAllowed = 'move';
  }

  function allowDrop(event: DragEvent, targetNodeID: string, mode: 'after' | 'as-child' | 'before') {
    if (!draggedNodeID)
      return;

    const allowed = mode === 'as-child'
      ? canMoveAsChild(draggedNodeID, targetNodeID)
      : canMoveBeside(draggedNodeID, targetNodeID);

    if (!allowed)
      return;

    event.preventDefault();
    dragOverNodeID = targetNodeID;

    if (event.dataTransfer)
      event.dataTransfer.dropEffect = 'move';
  }

  function dropNode(event: DragEvent, targetNodeID: string, mode: 'after' | 'as-child' | 'before') {
    event.preventDefault();
    const sourceNodeID = event.dataTransfer?.getData('text/plain') || draggedNodeID;
    draggedNodeID = '';
    dragOverNodeID = '';

    if (!sourceNodeID)
      return;

    if (mode === 'as-child') {
      moveNodeAsChild(sourceNodeID, targetNodeID);
      return;
    }

    moveNodeBeside(sourceNodeID, targetNodeID, mode);
  }

  function endNodeDrag() {
    draggedNodeID = '';
    dragOverNodeID = '';
  }

  function moveNodeAsChild(sourceNodeID: string, targetNodeID: string) {
    if (!canMoveAsChild(sourceNodeID, targetNodeID))
      return;

    const nextNodes = detachNode(sourceNodeID, cloneNodes(nodes));
    const targetNode = nextNodes.find((node) => node.nodeID === targetNodeID);

    if (!targetNode)
      return;

    targetNode.nodes = [...targetNode.nodes, sourceNodeID];
    commitVisualNodes(nextNodes);
  }

  function moveNodeBeside(sourceNodeID: string, targetNodeID: string, mode: 'after' | 'before') {
    if (!canMoveBeside(sourceNodeID, targetNodeID))
      return;

    const targetParentID = parentIDFor(targetNodeID, nodes);

    if (!targetParentID)
      return;

    const nextNodes = detachNode(sourceNodeID, cloneNodes(nodes));
    const targetParent = nextNodes.find((node) => node.nodeID === targetParentID);

    if (!targetParent)
      return;

    const nextSiblingIDs = targetParent.nodes.filter((nodeID) => nodeID !== sourceNodeID);
    const targetIndex = nextSiblingIDs.indexOf(targetNodeID);

    if (targetIndex === -1)
      return;

    nextSiblingIDs.splice(mode === 'before' ? targetIndex : targetIndex + 1, 0, sourceNodeID);
    targetParent.nodes = nextSiblingIDs;
    commitVisualNodes(nextNodes);
  }

  function canMoveAsChild(sourceNodeID: string, targetNodeID: string) {
    return sourceNodeID !== rootNodeID
      && !isSpecialNodeID(sourceNodeID)
      && !isSpecialNodeID(targetNodeID)
      && sourceNodeID !== targetNodeID
      && !descendantIDs(sourceNodeID, nodeMap(nodes)).has(targetNodeID);
  }

  function canMoveBeside(sourceNodeID: string, targetNodeID: string) {
    return sourceNodeID !== rootNodeID
      && !isSpecialNodeID(sourceNodeID)
      && !isSpecialNodeID(targetNodeID)
      && targetNodeID !== rootNodeID
      && sourceNodeID !== targetNodeID
      && !descendantIDs(sourceNodeID, nodeMap(nodes)).has(targetNodeID);
  }

  function detachNode(nodeID: string, sourceNodes: EditableNode[]) {
    return sourceNodes.map((node) => ({
      ...node,
      nodes: node.nodes.filter((childNodeID) => childNodeID !== nodeID)
    }));
  }

  function descendantIDs(nodeID: string, byID: Map<string, EditableNode>) {
    const descendants = new SvelteSet<string>();
    const node = byID.get(nodeID);

    if (!node)
      return descendants;

    for (const childNodeID of node.nodes) {
      descendants.add(childNodeID);
      for (const descendantID of descendantIDs(childNodeID, byID))
        descendants.add(descendantID);
    }

    return descendants;
  }

  function validateNodes(sourceNodes: EditableNode[]) {
    const messages: string[] = [];
    const byID = nodeMap(sourceNodes);

    if (!byID.has(rootNodeID))
      messages.push('Root node is missing.');

    for (const node of sourceNodes) {
      if (node.colour && !isHexColour(node.colour))
        messages.push(`${node.name} has an invalid colour.`);

      for (const [index, childNodeID] of node.nodes.entries()) {
        if (node.nodes.indexOf(childNodeID) === index && node.nodes.lastIndexOf(childNodeID) !== index)
          messages.push(`${node.name} references the same child node more than once.`);
      }

      for (const childNodeID of node.nodes) {
        if (!byID.has(childNodeID))
          messages.push(`${node.name} references a missing child node.`);
      }
    }

    const parentIDsByChildID = new SvelteMap<string, string[]>();

    for (const node of sourceNodes) {
      for (const childNodeID of node.nodes) {
        if (!byID.has(childNodeID))
          continue;

        parentIDsByChildID.set(childNodeID, [...(parentIDsByChildID.get(childNodeID) ?? []), node.nodeID]);
      }
    }

    if (parentIDsByChildID.has(rootNodeID))
      messages.push('Root node cannot be a child node.');

    for (const [childNodeID, parentIDs] of parentIDsByChildID) {
      if (new SvelteSet(parentIDs).size > 1)
        messages.push(`${byID.get(childNodeID)?.name ?? childNodeID} has multiple parents.`);
    }

    if (byID.has(rootNodeID)) {
      const visited = new SvelteSet<string>();
      const visiting = new SvelteSet<string>();
      visitNode(rootNodeID, byID, visited, visiting, messages);

      for (const node of sourceNodes) {
        if (!visited.has(node.nodeID))
          messages.push(`${node.name} is not reachable from the root node.`);
      }
    }

    return messages[0] ?? '';
  }

  function visitNode(nodeID: string, byID: Map<string, EditableNode>, visited: Set<string>, visiting: Set<string>, messages: string[]) {
    if (visited.has(nodeID))
      return;

    if (visiting.has(nodeID)) {
      messages.push(`${byID.get(nodeID)?.name ?? nodeID} creates a cycle.`);
      return;
    }

    const node = byID.get(nodeID);
    if (!node)
      return;

    visiting.add(nodeID);

    for (const childNodeID of node.nodes) {
      if (byID.has(childNodeID))
        visitNode(childNodeID, byID, visited, visiting, messages);
    }

    visiting.delete(nodeID);
    visited.add(nodeID);
  }

  function normaliseAccountSettings(sourceNodes: EditableNode[]) {
    return sourceNodes.map((node) => ({
      ...node,
      accountSettings: settingsForNode(node)
    }));
  }

  function normaliseNodes(sourceNodes: EditableNode[]) {
    const nextNodes = sourceNodes.map((node) => ({
      ...node,
      accountSettings: node.accountSettings.map((setting) => ({ ...setting })),
      nodes: [...node.nodes]
    }));
    const rootNode = nextNodes.find((node) => node.nodeID === rootNodeID);

    if (!rootNode)
      return normaliseAccountSettings(nextNodes);

    let specialNode = nextNodes.find(isSpecialNodeShape);
    if (!specialNode) {
      specialNode = {
        accountSettings: [],
        colour: SPECIAL_NODE_COLOUR,
        hidden: false,
        name: SPECIAL_NODE_NAME,
        nodeID: crypto.randomUUID(),
        nodes: [],
        subtotal: false
      };
      nextNodes.push(specialNode);
    }

    const specialChildNodes = specialNode.nodes.filter((nodeID) => nodeID !== rootNodeID && nodeID !== specialNode.nodeID);

    for (const node of nextNodes) {
      if (node.nodeID !== rootNodeID)
        node.nodes = node.nodes.filter((nodeID) => nodeID !== specialNode.nodeID);
    }

    rootNode.nodes = [
      specialNode.nodeID,
      ...specialChildNodes,
      ...rootNode.nodes.filter((nodeID) => nodeID !== rootNodeID && nodeID !== specialNode.nodeID)
    ];

    return normaliseAccountSettings(nextNodes).map((node) => {
      if (node.nodeID === rootNodeID)
        return { ...node, name: currentRootNodeName() };

      if (node.nodeID === specialNode.nodeID)
        return {
          ...node,
          accountSettings: [],
          colour: SPECIAL_NODE_COLOUR,
          hidden: false,
          name: SPECIAL_NODE_NAME,
          nodes: [],
          subtotal: false
        };

      return node;
    });
  }

  function currentRootNodeName() {
    return rootNodeName.trim() || 'Root';
  }

  function isSpecialNodeID(nodeID: string) {
    return nodeID === specialNodeID;
  }

  function specialNodeIDFor(sourceNodes: EditableNode[]) {
    const rootNode = sourceNodes.find((node) => node.nodeID === rootNodeID);

    if (rootNode?.nodes[0])
      return rootNode.nodes[0];

    return sourceNodes.find(isSpecialNodeShape)?.nodeID ?? '';
  }

  function isSpecialNodeShape(node: EditableNode) {
    return node.name.trim().toLocaleLowerCase() === SPECIAL_NODE_NAME.toLocaleLowerCase();
  }

  function hasMetricValues(node: EditableNode) {
    if (isSpecialNodeShape(node))
      return false;

    return node.accountSettings.some((setting) =>
      setting.targetWeight !== 0 ||
      setting.targetWeightMin !== 0 ||
      setting.targetWeightMax !== 0 ||
      setting.targetYield !== 0
    );
  }

  function settingsForNode(node: EditableNode) {
    if (isSpecialNodeID(node.nodeID) || isSpecialNodeShape(node))
      return [];

    const settingByAccountID = new SvelteMap(node.accountSettings.map((setting) => [setting.accountID, setting]));
    return allocationAccounts.map((account) => ({
      ...defaultAccountSetting(account.accountID),
      ...(settingByAccountID.get(account.accountID) ?? {})
    }));
  }

  function defaultAccountSetting(accountID: string): AssetAllocationNodeAccountSetting {
    return {
      accountID,
      targetWeight: 0,
      targetWeightMax: 0,
      targetWeightMin: 0,
      targetYield: 0
    };
  }

  function parentIDFor(nodeID: string, sourceNodes: EditableNode[]) {
    return sourceNodes.find((node) => node.nodes.includes(nodeID))?.nodeID ?? null;
  }

  function nodeMap(sourceNodes: EditableNode[]) {
    return new SvelteMap(sourceNodes.map((node) => [node.nodeID, node]));
  }

  function cloneNodes(sourceNodes: EditableNode[]) {
    return sourceNodes.map((node) => ({
      ...node,
      accountSettings: node.accountSettings.map((setting) => ({ ...setting })),
      nodes: [...node.nodes]
    }));
  }

  function toJsonNode(node: EditableNode) {
    const special = isSpecialNodeID(node.nodeID) || isSpecialNodeShape(node);

    return {
      accountSettings: special ? [] : node.accountSettings,
      colour: special ? SPECIAL_NODE_COLOUR : isHexColour(node.colour) ? node.colour : null,
      hidden: special ? false : node.hidden,
      name: node.nodeID === rootNodeID ? currentRootNodeName() : special ? SPECIAL_NODE_NAME : node.name,
      nodeID: node.nodeID,
      nodes: special ? [] : node.nodes,
      subtotal: special ? false : node.subtotal
    };
  }

  function readAccountSettings(source: Record<string, unknown>) {
    const value = source.accountSettings ?? source.AccountSettings;
    if (!Array.isArray(value))
      return null;

    return value
      .map((setting) => {
        if (!setting || typeof setting !== 'object')
          return null;

        const record = setting as Record<string, unknown>;
        const accountID = readString(record, 'accountID', 'AccountID');

        if (!accountID)
          return null;

        return {
          accountID,
          targetWeight: readNumber(record, 'targetWeight', 'TargetWeight'),
          targetWeightMax: readNumber(record, 'targetWeightMax', 'TargetWeightMax'),
          targetWeightMin: readNumber(record, 'targetWeightMin', 'TargetWeightMin'),
          targetYield: readNumber(record, 'targetYield', 'TargetYield')
        };
      })
      .filter((setting): setting is AssetAllocationNodeAccountSetting => setting !== null);
  }

  function readString(source: Record<string, unknown>, ...keys: string[]) {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'string')
        return value.trim();
    }

    return '';
  }

  function readStringArray(source: Record<string, unknown>, ...keys: string[]) {
    for (const key of keys) {
      const value = source[key];
      if (Array.isArray(value))
        return value.filter((item): item is string => typeof item === 'string' && item.trim().length > 0).map((item) => item.trim());
    }

    return [];
  }

  function readBoolean(source: Record<string, unknown>, ...keys: string[]) {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'boolean')
        return value;
    }

    return false;
  }

  function readNumber(source: Record<string, unknown>, ...keys: string[]) {
    for (const key of keys) {
      const value = source[key];

      if (typeof value === 'number')
        return Number.isFinite(value) ? value : 0;

      if (typeof value === 'string')
        return readFiniteNumber(value);
    }

    return 0;
  }

  function readFiniteNumber(value: string) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  function readColour(source: Record<string, unknown>) {
    const colour = 'colour' in source ? source.colour : source.Colour;

    if (colour === null)
      return null;

    if (typeof colour === 'string')
      return colour.trim().length > 0 ? safeColour(colour) : null;

    return DEFAULT_NODE_COLOUR;
  }

  function displayColour(colour: string | null | undefined) {
    return isHexColour(colour) ? colour : DEFAULT_NODE_COLOUR;
  }

  function hasNodeColour(node: EditableNode) {
    return isHexColour(node.colour);
  }

  function safeColour(colour: string | null | undefined) {
    return colour && isHexColour(colour) ? colour : DEFAULT_NODE_COLOUR;
  }

  function isHexColour(colour: string | null | undefined): colour is string {
    return typeof colour === 'string' && /^#[0-9a-fA-F]{6}$/.test(colour);
  }
</script>

<div class="valuation-node-editor">
  {#if editorMessage}
    <div class="valuation-node-editor-message" role="status">{editorMessage}</div>
  {/if}

  <div class="valuation-node-grid" role="tree">
    {#each rows as row (row.node.nodeID)}
      <section class="valuation-node-row" style={`--node-colour: ${displayColour(row.effectiveColour)}`}>
        <div class="valuation-node-tree-cell" style={`margin-left: ${row.depth * 1.25}rem`}>
          {#if row.node.nodeID !== rootNodeID && !isSpecialNodeID(row.node.nodeID)}
            <div
              aria-hidden="true"
              class={`valuation-node-drop-zone ${dragOverNodeID === row.node.nodeID ? 'valuation-node-drop-zone-active' : ''}`}
              ondragover={(event) => allowDrop(event, row.node.nodeID, 'before')}
              ondrop={(event) => dropNode(event, row.node.nodeID, 'before')}
            ></div>
          {:else if isSpecialNodeID(row.node.nodeID)}
            <div aria-hidden="true" class="valuation-node-drop-zone valuation-node-drop-zone-static"></div>
          {/if}

          <div
            aria-level={row.depth + 1}
            aria-selected="false"
            class={`valuation-node-pill ${isSpecialNodeID(row.node.nodeID) ? 'valuation-node-pill-special' : ''} ${!row.effectiveColour ? 'valuation-node-pill-none' : ''} ${dragOverNodeID === row.node.nodeID ? 'valuation-node-pill-over' : ''}`}
            ondragover={(event) => allowDrop(event, row.node.nodeID, 'as-child')}
            ondrop={(event) => dropNode(event, row.node.nodeID, 'as-child')}
            role="treeitem"
            tabindex="0"
          >
            {#if isSpecialNodeID(row.node.nodeID)}
              <span class="valuation-node-name valuation-node-name-static valuation-node-special-name" title={SPECIAL_NODE_NAME}>{SPECIAL_NODE_NAME}</span>
            {:else}
              <button
                aria-label={`Drag ${row.node.name}`}
                class="valuation-node-icon-button valuation-node-drag-button"
                draggable={row.node.nodeID !== rootNodeID}
                ondragend={endNodeDrag}
                ondragstart={(event) => startNodeDrag(event, row.node.nodeID)}
                title="Drag"
                type="button"
              >
                <span aria-hidden="true">::</span>
              </button>

              <span class={`valuation-node-colour-shell ${!hasNodeColour(row.node) ? 'valuation-node-colour-shell-empty' : ''}`}>
                <input aria-label="Node colour" class="valuation-node-colour" disabled={!hasNodeColour(row.node)} type="color" value={displayColour(row.effectiveColour)} oninput={(event) => setNodeColour(row.node.nodeID, (event.currentTarget as HTMLInputElement).value)} />
              </span>
              <label aria-label="No colour" class="valuation-node-no-colour-toggle" title="No colour">
                <input checked={!hasNodeColour(row.node)} onchange={(event) => setNodeNoColour(row.node.nodeID, (event.currentTarget as HTMLInputElement).checked, row.effectiveColour)} type="checkbox" />
              </label>
              {#if row.node.nodeID === rootNodeID}
                <span class="valuation-node-name valuation-node-name-static" title={currentRootNodeName()}>{currentRootNodeName()}</span>
              {:else}
                <input aria-label="Node name" class="valuation-node-name" type="text" value={row.node.name} onchange={(event) => renameNode(row.node.nodeID, (event.currentTarget as HTMLInputElement).value)} />
              {/if}

              <button aria-label={`Add child to ${row.node.name}`} class="valuation-node-icon-button valuation-node-action-button" onclick={() => addChild(row.node.nodeID)} title="Add child" type="button">+</button>

              {#if row.node.nodeID !== rootNodeID}
                <button aria-label={`Delete ${row.node.name}`} class="valuation-node-icon-button valuation-node-action-button valuation-node-delete-button" onclick={() => deleteNode(row.node.nodeID)} title="Delete" type="button">x</button>
              {/if}

              <label class="valuation-node-metrics-toggle" title="Metrics">
                <span>Metrics</span>
                <input checked={isMetricsOpen(row.node.nodeID)} onchange={() => toggleMetrics(row.node.nodeID)} type="checkbox" />
              </label>
            {/if}
          </div>

          {#if row.node.nodeID !== rootNodeID && !isSpecialNodeID(row.node.nodeID)}
            <div
              aria-hidden="true"
              class={`valuation-node-drop-zone ${dragOverNodeID === row.node.nodeID ? 'valuation-node-drop-zone-active' : ''}`}
              ondragover={(event) => allowDrop(event, row.node.nodeID, 'after')}
              ondrop={(event) => dropNode(event, row.node.nodeID, 'after')}
            ></div>
          {:else if isSpecialNodeID(row.node.nodeID)}
            <div aria-hidden="true" class="valuation-node-drop-zone valuation-node-drop-zone-static"></div>
          {/if}
        </div>

        {#if isMetricsOpen(row.node.nodeID) && !isSpecialNodeID(row.node.nodeID)}
          <div class="valuation-node-metrics-cell" style={`margin-left: ${row.depth * 1.25}rem`}>
            <div class="valuation-node-account-grid">
              {#if allocationAccounts.length > 0}
                <div class="valuation-node-account-header" aria-hidden="true">
                  <span>Account</span>
                  <span>Target</span>
                  <span>Min</span>
                  <span>Max</span>
                  <span>Yield</span>
                </div>
              {/if}

              {#each allocationAccounts as account (account.accountID)}
                {@const setting = settingsForNode(row.node).find((item) => item.accountID === account.accountID) ?? defaultAccountSetting(account.accountID)}
                <div class="valuation-node-account-row">
                  <div class="valuation-node-account-name">{account.name}</div>
                  <input aria-label={`${account.name} target`} type="number" step="0.0001" value={setting.targetWeight} onchange={(event) => setAccountSettingValue(row.node.nodeID, account.accountID, 'targetWeight', (event.currentTarget as HTMLInputElement).value)} />
                  <input aria-label={`${account.name} minimum`} type="number" step="0.0001" value={setting.targetWeightMin} onchange={(event) => setAccountSettingValue(row.node.nodeID, account.accountID, 'targetWeightMin', (event.currentTarget as HTMLInputElement).value)} />
                  <input aria-label={`${account.name} maximum`} type="number" step="0.0001" value={setting.targetWeightMax} onchange={(event) => setAccountSettingValue(row.node.nodeID, account.accountID, 'targetWeightMax', (event.currentTarget as HTMLInputElement).value)} />
                  <input aria-label={`${account.name} yield`} type="number" step="0.0001" value={setting.targetYield} onchange={(event) => setAccountSettingValue(row.node.nodeID, account.accountID, 'targetYield', (event.currentTarget as HTMLInputElement).value)} />
                </div>
              {:else}
                <div class="valuation-node-metrics-empty">No accounts assigned</div>
              {/each}
            </div>
          </div>
        {/if}
      </section>
    {:else}
      <div class="valuation-node-editor-message" role="status">Root node is missing.</div>
    {/each}
  </div>

  <label class="grid gap-1 text-sm font-medium text-slate-700">
    Nodes JSON
    <textarea class="min-h-72 rounded-md border border-slate-300 bg-white px-3 py-2 font-mono text-xs text-slate-950 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20" name="nodesJson" oninput={rawJsonChanged} required spellcheck="false" bind:value={nodesJson}></textarea>
  </label>
</div>

<style>
  .valuation-node-editor {
    display: grid;
    gap: 1rem;
    min-width: 0;
  }

  .valuation-node-editor-message {
    border: 1px solid var(--warning);
    border-radius: 0.375rem;
    background: color-mix(in srgb, var(--warning-soft) 62%, var(--panel));
    padding: 0.65rem 0.75rem;
    color: var(--warning-text);
    font-size: 0.875rem;
    font-weight: 650;
  }

  .valuation-node-grid {
    display: grid;
    gap: 0.35rem;
    overflow-x: auto;
    border: 1px solid var(--line);
    border-radius: 0.5rem;
    background: color-mix(in srgb, var(--panel-muted) 54%, var(--panel));
    padding: 0.75rem;
  }

  .valuation-node-row {
    display: grid;
    grid-template-columns: minmax(16rem, 28rem) minmax(26rem, 1fr);
    gap: 0.35rem;
    align-items: start;
    min-width: min(66rem, 100%);
  }

  .valuation-node-tree-cell {
    display: grid;
    gap: 0.2rem;
    justify-items: start;
    min-width: 0;
  }

  .valuation-node-pill {
    display: inline-flex;
    align-items: center;
    gap: 0.22rem;
    width: auto;
    max-width: 100%;
    min-width: 11rem;
    border: 1px solid color-mix(in srgb, var(--node-colour) 58%, var(--line));
    border-radius: 999px;
    background: color-mix(in srgb, var(--node-colour) 13%, var(--panel));
    padding: 0.18rem 0.3rem;
  }

  .valuation-node-pill-none {
    border-color: var(--line);
    background: var(--panel);
  }

  .valuation-node-pill-special {
    border-color: color-mix(in srgb, var(--node-colour) 68%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 16%, var(--panel));
  }

  .valuation-node-pill-over {
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--node-colour) 24%, transparent);
  }

  .valuation-node-icon-button {
    display: inline-grid;
    width: 1.45rem;
    height: 1.45rem;
    place-items: center;
    border: 0;
    border-radius: 999px;
    background: transparent;
    color: color-mix(in srgb, var(--node-colour) 72%, var(--muted));
    font-size: 0.78rem;
    font-weight: 800;
  }

  .valuation-node-icon-button:hover {
    background: color-mix(in srgb, var(--node-colour) 18%, transparent);
    color: var(--node-colour);
  }

  .valuation-node-drag-button {
    cursor: grab;
  }

  .valuation-node-drag-button:active {
    cursor: grabbing;
  }

  .valuation-node-action-button {
    border: 1px solid color-mix(in srgb, var(--node-colour) 42%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 12%, var(--panel));
    box-shadow: 0 1px 2px color-mix(in srgb, var(--ink) 14%, transparent);
  }

  .valuation-node-action-button:hover {
    border-color: color-mix(in srgb, var(--node-colour) 68%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 22%, var(--panel));
  }

  .valuation-node-action-button:active {
    box-shadow: inset 0 1px 2px color-mix(in srgb, var(--ink) 18%, transparent);
    transform: translateY(1px);
  }

  .valuation-node-delete-button {
    border-color: color-mix(in srgb, var(--danger) 42%, var(--line));
    background: color-mix(in srgb, var(--danger) 8%, var(--panel));
    color: color-mix(in srgb, var(--danger) 72%, var(--muted));
  }

  .valuation-node-delete-button:hover {
    border-color: color-mix(in srgb, var(--danger) 68%, var(--line));
    background: color-mix(in srgb, var(--danger) 14%, var(--panel));
    color: var(--danger);
  }

  .valuation-node-colour-shell {
    display: inline-grid;
    width: 1.28rem;
    height: 1.28rem;
    place-items: center;
    overflow: hidden;
    border: 0;
    border-radius: 999px;
    background: var(--node-colour);
    padding: 0;
  }

  .valuation-node-colour-shell-empty {
    border: 1px solid var(--line);
    background: var(--panel);
  }

  .valuation-node-colour {
    width: 100%;
    height: 100%;
    appearance: none;
    border: 0;
    color: transparent;
    background: transparent;
    font-size: 0;
    padding: 0;
  }

  .valuation-node-colour::-webkit-color-swatch-wrapper {
    padding: 0;
  }

  .valuation-node-colour::-webkit-color-swatch {
    border: 0;
  }

  .valuation-node-colour::-moz-color-swatch {
    border: 0;
  }

  .valuation-node-colour:disabled {
    cursor: not-allowed;
    opacity: 0;
  }

  .valuation-node-no-colour-toggle {
    display: inline-flex;
    align-items: center;
    width: 1rem;
    height: 1rem;
  }

  .valuation-node-no-colour-toggle input {
    width: 1rem;
    height: 1rem;
    margin: 0;
    accent-color: var(--node-colour);
  }

  .valuation-node-name {
    min-width: 5rem;
    max-width: 14rem;
    border: 0;
    border-radius: 999px;
    background: transparent;
    padding: 0.12rem 0.25rem;
    color: var(--ink);
    font-size: 0.85rem;
    font-weight: 750;
    outline: none;
    text-overflow: ellipsis;
  }

  .valuation-node-name:focus {
    background: var(--panel);
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--node-colour) 34%, transparent);
  }

  .valuation-node-special-name {
    color: var(--node-colour);
  }

  .valuation-node-metrics-toggle {
    display: inline-flex;
    align-items: center;
    gap: 0.28rem;
    border-radius: 999px;
    padding: 0.05rem 0.2rem;
    color: color-mix(in srgb, var(--node-colour) 72%, var(--muted));
    cursor: pointer;
    font-size: 0.72rem;
    font-weight: 800;
    white-space: nowrap;
  }

  .valuation-node-metrics-toggle input {
    position: relative;
    width: 1.55rem;
    height: 0.9rem;
    flex: 0 0 auto;
    appearance: none;
    border: 1px solid color-mix(in srgb, var(--node-colour) 34%, var(--line));
    border-radius: 999px;
    background: color-mix(in srgb, var(--node-colour) 12%, var(--panel));
  }

  .valuation-node-metrics-toggle input::before {
    position: absolute;
    top: 0.12rem;
    left: 0.12rem;
    width: 0.56rem;
    height: 0.56rem;
    border-radius: 999px;
    background: color-mix(in srgb, var(--node-colour) 52%, var(--muted));
    content: '';
    transition: transform 120ms ease, background 120ms ease;
  }

  .valuation-node-metrics-toggle input:checked {
    border-color: var(--node-colour);
    background: color-mix(in srgb, var(--node-colour) 26%, var(--panel));
  }

  .valuation-node-metrics-toggle input:checked::before {
    background: var(--node-colour);
    transform: translateX(0.64rem);
  }

  .valuation-node-drop-zone {
    width: 100%;
    height: 0.45rem;
    border-radius: 999px;
  }

  .valuation-node-drop-zone-active {
    background: color-mix(in srgb, var(--accent) 56%, transparent);
  }

  .valuation-node-metrics-cell {
    grid-column: 1 / -1;
    min-width: 0;
  }

  .valuation-node-account-grid {
    --valuation-node-account-columns: minmax(8rem, 1fr) repeat(4, minmax(4.2rem, 5.4rem));
    display: grid;
    gap: 0;
    width: 100%;
    border-left: 2px solid color-mix(in srgb, var(--node-colour) 36%, var(--line));
    padding-left: 0.35rem;
  }

  .valuation-node-account-header,
  .valuation-node-account-row {
    display: grid;
    grid-template-columns: var(--valuation-node-account-columns);
    gap: 0.18rem;
    align-items: center;
  }

  .valuation-node-account-header {
    border-bottom: 1px solid color-mix(in srgb, var(--line) 56%, transparent);
    padding: 0 0.2rem 0.12rem;
    color: color-mix(in srgb, var(--muted) 84%, var(--panel-muted));
    font-size: 0.62rem;
    font-weight: 500;
    line-height: 1.1;
  }

  .valuation-node-account-header span:not(:first-child) {
    text-align: right;
  }

  .valuation-node-account-row {
    border-bottom: 1px solid color-mix(in srgb, var(--line) 42%, transparent);
    padding: 0.12rem 0.2rem;
  }

  .valuation-node-account-row:last-of-type {
    border-bottom: 0;
  }

  .valuation-node-account-name {
    min-width: 0;
    overflow-wrap: anywhere;
    color: color-mix(in srgb, var(--ink) 74%, var(--muted));
    font-size: 0.7rem;
    font-weight: 500;
    line-height: 1.15;
  }

  .valuation-node-metrics-empty {
    border: 1px dashed var(--line);
    border-radius: 0.375rem;
    background: var(--panel);
    padding: 0.45rem 0.6rem;
    color: var(--muted);
    font-size: 0.82rem;
    font-weight: 650;
  }

  .valuation-node-account-row input {
    width: 100%;
    min-width: 0;
    border: 1px solid var(--line);
    border-radius: 0.18rem;
    background: var(--panel);
    padding: 0.08rem 0.18rem;
    color: var(--ink);
    font-size: 0.68rem;
    line-height: 1.15;
    text-align: right;
  }

  @media (max-width: 900px) {
    .valuation-node-row {
      grid-template-columns: minmax(18rem, 1fr);
    }

    .valuation-node-account-grid {
      --valuation-node-account-columns: minmax(8rem, 1fr) repeat(4, minmax(4.1rem, 1fr));
    }
  }

  @media (max-width: 640px) {
    .valuation-node-pill {
      min-width: min(14rem, 100%);
    }
  }
</style>
