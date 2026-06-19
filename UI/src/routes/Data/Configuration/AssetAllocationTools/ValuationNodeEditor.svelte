<script lang="ts">
  import { SvelteMap, SvelteSet } from 'svelte/reactivity';
  import type { Account, AssetAllocationNode, AssetAllocationNodeAccountSetting } from '$lib/types';

  const DEFAULT_NODE_COLOUR = '#0f766e';
  const SPECIAL_NODE_NAME = 'Unallocated';
  const SPECIAL_NODE_COLOUR = '#dc2626';
  const EMPTY_SPECIAL_NODE_COLOUR = '#64748b';

  type EditableNode = AssetAllocationNode & { colour?: string | null };
  type NodeRow = { colourInherited: boolean; depth: number; displayedColour: string | null; hasChildren: boolean; node: EditableNode };
  type ParseResult = { message: string; nodes: EditableNode[] };

  let {
    addRequest = 0,
    accounts,
    allocationAccountIDs,
    nodeHoldingCounts = {},
    nodesJson = $bindable(''),
    rootNodeID
  }: {
    addRequest?: number;
    accounts: Account[];
    allocationAccountIDs: string[];
    nodeHoldingCounts?: Record<string, number>;
    nodesJson: string;
    rootNodeID: string;
  } = $props();

  let editorMessage = $state('');
  let draggedNodeID = $state('');
  let dragOverNodeID = $state('');
  let collapsedNodeIDs = $state<string[]>([]);
  let collapsedAccountNodeIDs = $state<string[]>([]);
  let editingAccountNodeIDs = $state<string[]>([]);
  let nodes = $state<EditableNode[]>([]);

  const allocationAccounts = $derived(accounts.filter((account) => allocationAccountIDs.includes(account.accountID)));
  const specialNodeID = $derived(specialNodeIDFor(nodes));
  let handledAddRequest = $state(0);
  let addRequestInitialised = $state(false);
  const initialParse = parseNodesJson(nodesJson);
  const initialNodes = initialParse.message ? initialParse.nodes : normaliseNodes(initialParse.nodes);
  editorMessage = initialParse.message || validateNodes(initialNodes);
  nodes = initialNodes;
  collapsedAccountNodeIDs = defaultCollapsedAccountNodeIDs(initialNodes);

  if (!initialParse.message)
    nodesJson = JSON.stringify(initialNodes.map(toJsonNode), null, 2);

  $effect(() => {
    if (!addRequestInitialised) {
      handledAddRequest = addRequest;
      addRequestInitialised = true;
      return;
    }

    if (addRequest === handledAddRequest)
      return;

    handledAddRequest = addRequest;
    addChild(null);
  });

  const rows = $derived.by(() => buildRows(nodes, collapsedNodeIDs));

  function buildRows(sourceNodes: EditableNode[], collapsedIDs: string[]) {
    const byID = nodeMap(sourceNodes);
    const nextRows: NodeRow[] = [];

    for (const topLevelNode of topLevelNodes(sourceNodes))
      appendRows(topLevelNode, 0, null, byID, new SvelteSet<string>(), nextRows, collapsedIDs);

    return nextRows;
  }

  function appendRows(node: EditableNode, depth: number, inheritedColour: string | null, byID: Map<string, EditableNode>, path: Set<string>, nextRows: NodeRow[], collapsedIDs: string[]) {
    if (path.has(node.nodeID))
      return;

    const nodeColour = isHexColour(node.colour) ? node.colour : null;
    const special = isSpecialNodeShape(node);
    const colourInherited = inheritedColour !== null && !special;
    const displayedColour = special ? nodeColour ?? inheritedColour : inheritedColour ?? nodeColour;
    const hasChildren = hasVisibleChildren(node, byID);
    nextRows.push({ colourInherited, depth, displayedColour, hasChildren, node });

    if (hasChildren && collapsedIDs.includes(node.nodeID))
      return;

    const nextPath = new SvelteSet(path);
    nextPath.add(node.nodeID);

    for (const childNodeID of node.nodes) {
      const childNode = byID.get(childNodeID);

      if (childNode)
        appendRows(childNode, depth + 1, special ? inheritedColour : displayedColour, byID, nextPath, nextRows, collapsedIDs);
    }
  }

  function toggleNodeCollapsed(nodeID: string) {
    collapsedNodeIDs = collapsedNodeIDs.includes(nodeID)
      ? collapsedNodeIDs.filter((collapsedNodeID) => collapsedNodeID !== nodeID)
      : [...collapsedNodeIDs, nodeID];
  }

  function toggleAccountNodeCollapsed(nodeID: string) {
    if (collapsedAccountNodeIDs.includes(nodeID)) {
      collapsedAccountNodeIDs = collapsedAccountNodeIDs.filter((collapsedNodeID) => collapsedNodeID !== nodeID);
      return;
    }

    collapsedAccountNodeIDs = [...collapsedAccountNodeIDs, nodeID];
    editingAccountNodeIDs = editingAccountNodeIDs.filter((editingNodeID) => editingNodeID !== nodeID);
  }

  function toggleAccountNodeEditing(nodeID: string) {
    if (collapsedAccountNodeIDs.includes(nodeID))
      return;

    editingAccountNodeIDs = editingAccountNodeIDs.includes(nodeID)
      ? editingAccountNodeIDs.filter((editingNodeID) => editingNodeID !== nodeID)
      : [...editingAccountNodeIDs, nodeID];
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

  function commitVisualNodes(nextNodes: EditableNode[]) {
    const normalisedNodes = normaliseNodes(nextNodes);
    const previousNodeIDs = new SvelteSet(nodes.map((node) => node.nodeID));
    const nextNodeIDs = new SvelteSet(normalisedNodes.map((node) => node.nodeID));
    const nextMetricNodeIDs = new SvelteSet(defaultCollapsedAccountNodeIDs(normalisedNodes));
    const retainedCollapsedAccountNodeIDs = collapsedAccountNodeIDs.filter((nodeID) => nextNodeIDs.has(nodeID));
    nodes = normalisedNodes;
    collapsedNodeIDs = collapsedNodeIDs.filter((nodeID) => nextNodeIDs.has(nodeID));
    collapsedAccountNodeIDs = [
      ...retainedCollapsedAccountNodeIDs,
      ...defaultCollapsedAccountNodeIDs(normalisedNodes).filter((nodeID) => !previousNodeIDs.has(nodeID) && !retainedCollapsedAccountNodeIDs.includes(nodeID))
    ];
    editingAccountNodeIDs = editingAccountNodeIDs.filter((nodeID) => nextMetricNodeIDs.has(nodeID) && !collapsedAccountNodeIDs.includes(nodeID));
    editorMessage = validateNodes(normalisedNodes);
    nodesJson = JSON.stringify(normalisedNodes.map(toJsonNode), null, 2);
  }

  function addChild(parentNodeID: string | null) {
    if (parentNodeID && isSpecialNodeID(parentNodeID))
      return;

    const nextNodes = cloneNodes(nodes);
    const parentNode = parentNodeID ? nextNodes.find((node) => node.nodeID === parentNodeID) : null;

    const childNodeID = crypto.randomUUID();

    if (parentNode)
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
    if (isSpecialNodeID(nodeID) || hasInheritedColour(nodeID))
      return;

    setNodeExplicitColour(nodeID, safeColour(value), true);
  }

  function setNodeColourEnabled(nodeID: string, colourEnabled: boolean, fallbackColour: string | null) {
    if (isSpecialNodeID(nodeID) || hasInheritedColour(nodeID))
      return;

    setNodeExplicitColour(nodeID, colourEnabled ? displayColour(fallbackColour) : null, colourEnabled);
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
    if (isSpecialNodeID(nodeID))
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

  function startNodeDrag(event: DragEvent, nodeID: string) {
    if (isSpecialNodeID(nodeID)) {
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

    if (!targetParentID) {
      moveTopLevelNodeBeside(sourceNodeID, targetNodeID, mode);
      return;
    }

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

  function moveTopLevelNodeBeside(sourceNodeID: string, targetNodeID: string, mode: 'after' | 'before') {
    const detachedNodes = detachNode(sourceNodeID, cloneNodes(nodes));
    const sourceNode = detachedNodes.find((node) => node.nodeID === sourceNodeID);

    if (!sourceNode)
      return;

    const nextNodes = detachedNodes.filter((node) => node.nodeID !== sourceNodeID);
    const targetIndex = nextNodes.findIndex((node) => node.nodeID === targetNodeID);

    if (targetIndex === -1)
      return;

    nextNodes.splice(mode === 'before' ? targetIndex : targetIndex + 1, 0, sourceNode);
    commitVisualNodes(nextNodes);
  }

  function moveNodeWithinSiblings(nodeID: string, direction: -1 | 1) {
    const parentID = parentIDFor(nodeID, nodes);

    if (isSpecialNodeID(nodeID))
      return;

    if (!parentID) {
      moveTopLevelNodeWithinSiblings(nodeID, direction);
      return;
    }

    const siblingIDs = movableSiblingIDs(nodeID, nodes);
    const siblingIndex = siblingIDs.indexOf(nodeID);
    const targetSiblingID = siblingIDs[siblingIndex + direction];

    if (siblingIndex === -1 || !targetSiblingID)
      return;

    const nextNodes = cloneNodes(nodes);
    const parent = nextNodes.find((node) => node.nodeID === parentID);

    if (!parent)
      return;

    const nodeIndex = parent.nodes.indexOf(nodeID);
    const targetIndex = parent.nodes.indexOf(targetSiblingID);

    if (nodeIndex === -1 || targetIndex === -1)
      return;

    const nextChildNodeIDs = [...parent.nodes];
    nextChildNodeIDs[nodeIndex] = targetSiblingID;
    nextChildNodeIDs[targetIndex] = nodeID;
    parent.nodes = nextChildNodeIDs;
    commitVisualNodes(nextNodes);
  }

  function moveTopLevelNodeWithinSiblings(nodeID: string, direction: -1 | 1) {
    const siblingIDs = movableSiblingIDs(nodeID, nodes);
    const siblingIndex = siblingIDs.indexOf(nodeID);
    const targetSiblingID = siblingIDs[siblingIndex + direction];

    if (siblingIndex === -1 || !targetSiblingID)
      return;

    const nextNodes = cloneNodes(nodes);
    const nodeIndex = nextNodes.findIndex((node) => node.nodeID === nodeID);
    const targetIndex = nextNodes.findIndex((node) => node.nodeID === targetSiblingID);

    if (nodeIndex === -1 || targetIndex === -1)
      return;

    [nextNodes[nodeIndex], nextNodes[targetIndex]] = [nextNodes[targetIndex], nextNodes[nodeIndex]];
    commitVisualNodes(nextNodes);
  }

  function siblingMoveState(nodeID: string) {
    const siblingIDs = movableSiblingIDs(nodeID, nodes);
    const index = siblingIDs.indexOf(nodeID);

    return {
      canMoveDown: index !== -1 && index < siblingIDs.length - 1,
      canMoveUp: index > 0,
      show: siblingIDs.length > 1 && index !== -1
    };
  }

  function movableSiblingIDs(nodeID: string, sourceNodes: EditableNode[]) {
    const parentID = parentIDFor(nodeID, sourceNodes);

    if (!parentID)
      return topLevelNodes(sourceNodes)
        .map((node) => node.nodeID)
        .filter((childNodeID) => !isSpecialNodeID(childNodeID));

    const parent = sourceNodes.find((node) => node.nodeID === parentID);

    if (!parent)
      return [];

    return parent.nodes.filter((childNodeID) => !isSpecialNodeID(childNodeID));
  }

  function canMoveAsChild(sourceNodeID: string, targetNodeID: string) {
    return !isSpecialNodeID(sourceNodeID)
      && !isSpecialNodeID(targetNodeID)
      && sourceNodeID !== targetNodeID
      && !descendantIDs(sourceNodeID, nodeMap(nodes)).has(targetNodeID);
  }

  function canMoveBeside(sourceNodeID: string, targetNodeID: string) {
    return !isSpecialNodeID(sourceNodeID)
      && !isSpecialNodeID(targetNodeID)
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

    for (const node of sourceNodes) {
      if (node.nodeID === rootNodeID)
        messages.push('Root node must not be included in nodes.');

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

    for (const [childNodeID, parentIDs] of parentIDsByChildID) {
      if (new SvelteSet(parentIDs).size > 1)
        messages.push(`${byID.get(childNodeID)?.name ?? childNodeID} has multiple parents.`);
    }

    if (sourceNodes.length) {
      const visited = new SvelteSet<string>();
      const visiting = new SvelteSet<string>();

      for (const topLevelNode of topLevelNodes(sourceNodes))
        visitNode(topLevelNode.nodeID, byID, visited, visiting, messages);

      for (const node of sourceNodes) {
        if (!visited.has(node.nodeID))
          messages.push(`${node.name} is not reachable from a top-level node.`);
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
    const legacyRootNode = sourceNodes.find((node) => node.nodeID === rootNodeID);
    const nextNodes = sourceNodes.filter((node) => node.nodeID !== rootNodeID).map((node) => ({
      ...node,
      accountSettings: node.accountSettings.map((setting) => ({ ...setting })),
      nodes: [...node.nodes]
    }));

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

    const legacyTopLevelNodeIDs = legacyRootNode?.nodes.filter((nodeID) => nodeID !== rootNodeID && nodeID !== specialNode.nodeID) ?? [];
    const specialChildNodes = specialNode.nodes.filter((nodeID) => nodeID !== rootNodeID && nodeID !== specialNode.nodeID);

    for (const node of nextNodes) {
      node.nodes = node.nodes.filter((nodeID) => nodeID !== rootNodeID && nodeID !== specialNode.nodeID);
    }

    return orderNodes(clearInheritedColourOverrides(normaliseAccountSettings(nextNodes).map((node) => {
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
    })), [
      ...specialChildNodes,
      ...legacyTopLevelNodeIDs
    ]);
  }

  function clearInheritedColourOverrides(sourceNodes: EditableNode[]) {
    const nextNodes = cloneNodes(sourceNodes);
    const byID = nodeMap(nextNodes);

    for (const topLevelNode of topLevelNodes(nextNodes))
      clearChildColourOverrides(topLevelNode.nodeID, false, byID, new SvelteSet<string>());

    return nextNodes;
  }

  function clearChildColourOverrides(nodeID: string, inheritedColour: boolean, byID: Map<string, EditableNode>, path: Set<string>) {
    if (path.has(nodeID))
      return;

    const node = byID.get(nodeID);
    if (!node)
      return;

    const nextPath = new SvelteSet(path);
    nextPath.add(nodeID);

    const special = isSpecialNodeShape(node);

    if (inheritedColour && !special)
      node.colour = null;

    const nextInheritedColour = special ? inheritedColour : inheritedColour || isHexColour(node.colour);

    for (const childNodeID of node.nodes)
      clearChildColourOverrides(childNodeID, nextInheritedColour, byID, nextPath);
  }

  function isSpecialNodeID(nodeID: string) {
    return nodeID === specialNodeID;
  }

  function specialNodeIDFor(sourceNodes: EditableNode[]) {
    const firstNode = sourceNodes[0];
    return firstNode && isSpecialNodeShape(firstNode) ? firstNode.nodeID : sourceNodes.find(isSpecialNodeShape)?.nodeID ?? '';
  }

  function isSpecialNodeShape(node: EditableNode) {
    return node.name.trim().toLocaleLowerCase() === SPECIAL_NODE_NAME.toLocaleLowerCase();
  }

  function hasVisibleChildren(node: EditableNode, byID: Map<string, EditableNode>) {
    return node.nodes.some((childNodeID) => byID.has(childNodeID));
  }

  function isLeafNode(node: EditableNode) {
    return node.nodes.length === 0;
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
      targetWeight: null,
      targetWeightMax: null,
      targetWeightMin: null,
      targetYield: null
    };
  }

  function accountSettingForNode(node: EditableNode, accountID: string) {
    return node.accountSettings.find((setting) => setting.accountID === accountID) ?? defaultAccountSetting(accountID);
  }

  function setAccountSetting(nodeID: string, accountID: string, key: keyof Omit<AssetAllocationNodeAccountSetting, 'accountID'>, value: string) {
    if (isSpecialNodeID(nodeID))
      return;

    const nextNodes = cloneNodes(nodes);
    const node = nextNodes.find((item) => item.nodeID === nodeID);

    if (!node)
      return;

    const settingIndex = node.accountSettings.findIndex((setting) => setting.accountID === accountID);
    const currentSetting = settingIndex === -1
      ? defaultAccountSetting(accountID)
      : node.accountSettings[settingIndex];
    const nextSetting = {
      ...currentSetting,
      [key]: readPercentageInput(value)
    };

    node.accountSettings = settingIndex === -1
      ? [...node.accountSettings, nextSetting]
      : node.accountSettings.map((setting, index) => index === settingIndex ? nextSetting : setting);

    commitVisualNodes(nextNodes);
  }

  function defaultCollapsedAccountNodeIDs(sourceNodes: EditableNode[]) {
    return sourceNodes
      .filter((node) => !isSpecialNodeShape(node))
      .map((node) => node.nodeID);
  }

  function readPercentageInput(value: string) {
    if (!value.trim())
      return null;

    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed / 100 : null;
  }

  function parentIDFor(nodeID: string, sourceNodes: EditableNode[]) {
    return sourceNodes.find((node) => node.nodes.includes(nodeID))?.nodeID ?? null;
  }

  function hasInheritedColour(nodeID: string) {
    let parentID = parentIDFor(nodeID, nodes);
    const byID = nodeMap(nodes);
    const seen = new SvelteSet<string>();

    while (parentID) {
      if (seen.has(parentID))
        return false;

      seen.add(parentID);

      const parent = byID.get(parentID);
      if (!parent)
        return false;

      if (!isSpecialNodeShape(parent) && isHexColour(parent.colour))
        return true;

      parentID = parentIDFor(parent.nodeID, nodes);
    }

    return false;
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
      name: special ? SPECIAL_NODE_NAME : node.name,
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
          targetWeight: readNullableNumber(record, 'targetWeight', 'TargetWeight'),
          targetWeightMax: readNullableNumber(record, 'targetWeightMax', 'TargetWeightMax'),
          targetWeightMin: readNullableNumber(record, 'targetWeightMin', 'TargetWeightMin'),
          targetYield: readNullableNumber(record, 'targetYield', 'TargetYield')
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

  function readNullableNumber(source: Record<string, unknown>, ...keys: string[]) {
    for (const key of keys) {
      const value = source[key];

      if (value === null)
        return null;

      if (typeof value === 'number')
        return Number.isFinite(value) ? value : null;

      if (typeof value === 'string')
        return value.trim() ? readFiniteNumber(value) : null;
    }

    return null;
  }

  function readFiniteNumber(value: string) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
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

  function displayRowColour(row: NodeRow) {
    if (isSpecialNodeID(row.node.nodeID) && !nodeHasChildHoldings(row.node.nodeID))
      return EMPTY_SPECIAL_NODE_COLOUR;

    return row.displayedColour;
  }

  function nodeHasChildHoldings(nodeID: string) {
    return (nodeHoldingCounts[nodeID] ?? 0) > 0;
  }

  function percentInputValue(value: number | null) {
    return value === null ? '' : Number((value * 100).toFixed(4));
  }

  function formatPercent(value: number | null) {
    return value === null ? '' : `${Number((value * 100).toFixed(2))}%`;
  }

  function hasExplicitColour(node: EditableNode) {
    return isHexColour(node.colour);
  }

  function topLevelNodes(sourceNodes: EditableNode[]) {
    const childNodeIDs = new SvelteSet(sourceNodes.flatMap((node) => node.nodes));
    return sourceNodes.filter((node) => !childNodeIDs.has(node.nodeID));
  }

  function orderNodes(sourceNodes: EditableNode[], preferredTopLevelNodeIDs: string[] = []) {
    const byID = nodeMap(sourceNodes);
    const childNodeIDs = new SvelteSet(sourceNodes.flatMap((node) => node.nodes));
    const orderedNodeIDs = [
      specialNodeIDFor(sourceNodes),
      ...preferredTopLevelNodeIDs,
      ...sourceNodes.filter((node) => !childNodeIDs.has(node.nodeID)).map((node) => node.nodeID)
    ].filter(Boolean);
    const addedNodeIDs = new SvelteSet<string>();
    const orderedNodes: EditableNode[] = [];

    for (const nodeID of orderedNodeIDs) {
      const node = byID.get(nodeID);

      if (node && !addedNodeIDs.has(nodeID)) {
        orderedNodes.push(node);
        addedNodeIDs.add(nodeID);
      }
    }

    for (const node of sourceNodes) {
      if (!addedNodeIDs.has(node.nodeID))
        orderedNodes.push(node);
    }

    return orderedNodes;
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
      <section class="valuation-node-row" style={`--node-colour: ${displayColour(displayRowColour(row))}`}>
        <div class="valuation-node-tree-cell" style={`margin-left: ${row.depth * 1.25}rem`}>
          {#if !isSpecialNodeID(row.node.nodeID)}
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
            class={`valuation-node-pill ${isSpecialNodeID(row.node.nodeID) ? 'valuation-node-pill-special' : ''} ${!row.displayedColour ? 'valuation-node-pill-none' : ''} ${dragOverNodeID === row.node.nodeID ? 'valuation-node-pill-over' : ''}`}
            ondragover={(event) => allowDrop(event, row.node.nodeID, 'as-child')}
            ondrop={(event) => dropNode(event, row.node.nodeID, 'as-child')}
            role="treeitem"
            tabindex="0"
          >
            {#if row.hasChildren}
              <button
                aria-expanded={!collapsedNodeIDs.includes(row.node.nodeID)}
                aria-label={`${collapsedNodeIDs.includes(row.node.nodeID) ? 'Expand' : 'Collapse'} ${isSpecialNodeID(row.node.nodeID) ? SPECIAL_NODE_NAME : row.node.name}`}
                class="valuation-node-icon-button valuation-node-expand-button"
                onclick={() => toggleNodeCollapsed(row.node.nodeID)}
                title={collapsedNodeIDs.includes(row.node.nodeID) ? 'Expand' : 'Collapse'}
                type="button"
              >
                <svg aria-hidden="true" class:valuation-node-expand-open={!collapsedNodeIDs.includes(row.node.nodeID)} viewBox="0 0 24 24"><path d="m9 6 6 6-6 6" /></svg>
              </button>
            {/if}

            {#if isSpecialNodeID(row.node.nodeID)}
              <span class="valuation-node-name valuation-node-name-static valuation-node-special-name" title={SPECIAL_NODE_NAME}>{SPECIAL_NODE_NAME}</span>
            {:else}
              <button
                aria-label={`Drag ${row.node.name}`}
                class="valuation-node-icon-button valuation-node-drag-button"
                draggable="true"
                ondragend={endNodeDrag}
                ondragstart={(event) => startNodeDrag(event, row.node.nodeID)}
                title="Drag"
                type="button"
              >
                <span aria-hidden="true">::</span>
              </button>

              <span class={`valuation-node-colour-shell ${!row.colourInherited && !hasExplicitColour(row.node) ? 'valuation-node-colour-shell-disabled' : ''} ${row.colourInherited ? 'valuation-node-colour-shell-inherited' : ''}`}>
                <input aria-label="Node colour" class="valuation-node-colour" disabled={row.colourInherited || !hasExplicitColour(row.node)} type="color" value={displayColour(row.displayedColour)} oninput={(event) => setNodeColour(row.node.nodeID, (event.currentTarget as HTMLInputElement).value)} />
              </span>
              <label aria-label="Use colour" class={`valuation-node-colour-toggle ${row.colourInherited ? 'valuation-node-colour-toggle-inherited' : ''}`} title={row.colourInherited ? 'Colour inherited from parent' : 'Use colour'}>
                <input checked={row.colourInherited || hasExplicitColour(row.node)} disabled={row.colourInherited} onchange={(event) => setNodeColourEnabled(row.node.nodeID, (event.currentTarget as HTMLInputElement).checked, row.displayedColour)} type="checkbox" />
              </label>
              <input aria-label="Node name" class="valuation-node-name" type="text" value={row.node.name} onchange={(event) => renameNode(row.node.nodeID, (event.currentTarget as HTMLInputElement).value)} />

              {@const moveState = siblingMoveState(row.node.nodeID)}
              {#if moveState.show}
                <span class="valuation-node-order-controls" aria-label={`Move ${row.node.name}`}>
                  <button
                    aria-label={`Move ${row.node.name} up`}
                    class="valuation-node-icon-button valuation-node-action-button valuation-node-order-button"
                    disabled={!moveState.canMoveUp}
                    onclick={() => moveNodeWithinSiblings(row.node.nodeID, -1)}
                    title="Move up"
                    type="button"
                  >
                    <svg aria-hidden="true" viewBox="0 0 24 24"><path d="m7 14 5-5 5 5" /></svg>
                  </button>
                  <button
                    aria-label={`Move ${row.node.name} down`}
                    class="valuation-node-icon-button valuation-node-action-button valuation-node-order-button"
                    disabled={!moveState.canMoveDown}
                    onclick={() => moveNodeWithinSiblings(row.node.nodeID, 1)}
                    title="Move down"
                    type="button"
                  >
                    <svg aria-hidden="true" viewBox="0 0 24 24"><path d="m7 10 5 5 5-5" /></svg>
                  </button>
                </span>
              {/if}

              <button aria-label={`Delete ${row.node.name}`} class="valuation-node-icon-button valuation-node-action-button valuation-node-delete-button" onclick={() => deleteNode(row.node.nodeID)} title="Delete" type="button">x</button>
            {/if}
          </div>

          {#if !isSpecialNodeID(row.node.nodeID) && allocationAccounts.length}
            {@const metricsCollapsed = collapsedAccountNodeIDs.includes(row.node.nodeID)}
            {@const metricsEditing = editingAccountNodeIDs.includes(row.node.nodeID)}
            <section class="valuation-node-account-panel">
              <div class="valuation-node-account-toolbar">
                <button
                  aria-expanded={!metricsCollapsed}
                  class="valuation-node-account-toggle"
                  onclick={() => toggleAccountNodeCollapsed(row.node.nodeID)}
                  type="button"
                >
                  <svg aria-hidden="true" class:valuation-node-account-open={!metricsCollapsed} viewBox="0 0 24 24"><path d="m9 6 6 6-6 6" /></svg>
                  <span>Metrics</span>
                </button>

                {#if !metricsCollapsed}
                  <button
                    aria-label={`${metricsEditing ? 'Finish editing' : 'Edit'} Metrics for ${row.node.name}`}
                    class={`valuation-node-icon-button valuation-node-account-edit-button ${metricsEditing ? 'valuation-node-account-edit-button-active' : ''}`}
                    onclick={() => toggleAccountNodeEditing(row.node.nodeID)}
                    title={metricsEditing ? 'Done' : 'Edit metrics'}
                    type="button"
                  >
                    {#if metricsEditing}
                      <svg aria-hidden="true" viewBox="0 0 24 24"><path d="m5 13 4 4L19 7" /></svg>
                    {:else}
                      <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M12 20h9" /><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" /></svg>
                    {/if}
                  </button>
                {/if}
              </div>

              {#if !metricsCollapsed}
                <div class={`valuation-node-account-grid ${metricsEditing ? 'valuation-node-account-grid-editing' : 'valuation-node-account-grid-readonly'}`}>
                  <div class="valuation-node-account-header">Target %</div>
                  <div class="valuation-node-account-header">Min %</div>
                  <div class="valuation-node-account-header">Max %</div>
                  <div class="valuation-node-account-header">Yield %</div>
                  {#each allocationAccounts as account (account.accountID)}
                    {@const accountSetting = accountSettingForNode(row.node, account.accountID)}
                    <div class="valuation-node-account-name" title={account.formalName}>{account.name}</div>
                    {#if metricsEditing}
                      <input
                        aria-label={`${account.name} target percentage`}
                        class="house-control house-control-compact valuation-node-account-input"
                        onchange={(event) => setAccountSetting(row.node.nodeID, account.accountID, 'targetWeight', event.currentTarget.value)}
                        step="0.01"
                        type="number"
                        value={percentInputValue(accountSetting.targetWeight)}
                      />
                      <input
                        aria-label={`${account.name} minimum percentage`}
                        class="house-control house-control-compact valuation-node-account-input"
                        onchange={(event) => setAccountSetting(row.node.nodeID, account.accountID, 'targetWeightMin', event.currentTarget.value)}
                        step="0.01"
                        type="number"
                        value={percentInputValue(accountSetting.targetWeightMin)}
                      />
                      <input
                        aria-label={`${account.name} maximum percentage`}
                        class="house-control house-control-compact valuation-node-account-input"
                        onchange={(event) => setAccountSetting(row.node.nodeID, account.accountID, 'targetWeightMax', event.currentTarget.value)}
                        step="0.01"
                        type="number"
                        value={percentInputValue(accountSetting.targetWeightMax)}
                      />
                      <input
                        aria-label={`${account.name} yield percentage`}
                        class="house-control house-control-compact valuation-node-account-input"
                        onchange={(event) => setAccountSetting(row.node.nodeID, account.accountID, 'targetYield', event.currentTarget.value)}
                        step="0.01"
                        type="number"
                        value={percentInputValue(accountSetting.targetYield)}
                      />
                    {:else}
                      <div class="valuation-node-account-value">{formatPercent(accountSetting.targetWeight)}</div>
                      <div class="valuation-node-account-value">{formatPercent(accountSetting.targetWeightMin)}</div>
                      <div class="valuation-node-account-value">{formatPercent(accountSetting.targetWeightMax)}</div>
                      <div class="valuation-node-account-value">{formatPercent(accountSetting.targetYield)}</div>
                    {/if}
                  {/each}
                </div>
              {/if}
            </section>
          {/if}

          {#if !isSpecialNodeID(row.node.nodeID)}
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
      </section>
    {:else}
      <div class="valuation-node-editor-message" role="status">No nodes configured.</div>
    {/each}
  </div>

  <input name="nodesJson" type="hidden" value={nodesJson} />
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
    grid-template-columns: max-content minmax(26rem, 1fr);
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
    width: 34rem;
    max-width: 34rem;
    min-width: 34rem;
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

  .valuation-node-expand-button {
    flex: 0 0 auto;
    border: 1px solid color-mix(in srgb, var(--node-colour) 42%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 10%, var(--panel));
    cursor: pointer;
  }

  .valuation-node-expand-button svg {
    width: 0.9rem;
    height: 0.9rem;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.5;
    stroke-linecap: round;
    stroke-linejoin: round;
    transition: transform 140ms ease;
  }

  .valuation-node-expand-open {
    transform: rotate(90deg);
  }

  .valuation-node-account-panel {
    display: grid;
    gap: 0.28rem;
    width: 34rem;
    max-width: 34rem;
    min-width: 34rem;
    border: 1px solid color-mix(in srgb, var(--node-colour) 28%, var(--line));
    border-radius: 0.375rem;
    background: color-mix(in srgb, var(--node-colour) 5%, var(--panel));
    padding: 0.4rem;
  }

  .valuation-node-account-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    min-width: 0;
  }

  .valuation-node-account-toggle {
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

  .valuation-node-account-toggle svg {
    width: 0.85rem;
    height: 0.85rem;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.5;
    stroke-linecap: round;
    stroke-linejoin: round;
    transition: transform 140ms ease;
  }

  .valuation-node-account-open {
    transform: rotate(90deg);
  }

  .valuation-node-account-edit-button {
    width: 1.32rem;
    height: 1.32rem;
    flex: 0 0 auto;
    border: 1px solid color-mix(in srgb, var(--node-colour) 34%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 8%, var(--panel));
    cursor: pointer;
  }

  .valuation-node-account-edit-button svg {
    width: 0.8rem;
    height: 0.8rem;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.2;
    stroke-linecap: round;
    stroke-linejoin: round;
  }

  .valuation-node-account-edit-button-active {
    background: color-mix(in srgb, var(--success) 12%, var(--panel));
    color: color-mix(in srgb, var(--success) 80%, var(--ink));
  }

  .valuation-node-account-grid {
    display: grid;
    grid-template-columns: minmax(8rem, 1fr) repeat(4, minmax(4.4rem, 1fr));
    gap: 0.25rem 0.35rem;
    align-items: center;
  }

  .valuation-node-account-grid-readonly {
    grid-template-columns: minmax(8rem, 1fr) repeat(4, minmax(3.7rem, 0.62fr));
    gap: 0.16rem 0.42rem;
  }

  .valuation-node-account-grid > .valuation-node-account-header:first-child {
    grid-column: 2;
  }

  .valuation-node-account-header {
    color: var(--ink);
    font-size: 0.66rem;
    font-weight: 800;
    text-align: right;
  }

  .valuation-node-account-name {
    min-width: 0;
    overflow: hidden;
    color: var(--ink);
    font-size: 0.76rem;
    font-weight: 500;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .valuation-node-account-input {
    width: 100%;
    min-width: 0;
  }

  .valuation-node-account-value {
    min-width: 0;
    overflow: hidden;
    color: var(--muted);
    font-size: 0.72rem;
    font-variant-numeric: tabular-nums;
    font-weight: 560;
    text-align: right;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .valuation-node-action-button {
    border: 1px solid color-mix(in srgb, var(--node-colour) 42%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 12%, var(--panel));
    box-shadow: 0 1px 2px color-mix(in srgb, var(--ink) 14%, transparent);
  }

  .valuation-node-add-button {
    display: inline-grid;
    height: 1.45rem;
    place-items: center;
    border-radius: 999px;
    padding: 0 0.6rem;
    color: color-mix(in srgb, var(--node-colour) 76%, var(--ink));
    font-size: 0.72rem;
    font-weight: 800;
    line-height: 1;
  }

  .valuation-node-order-controls {
    display: inline-flex;
    gap: 0.12rem;
    align-items: center;
  }

  .valuation-node-order-button svg {
    width: 0.9rem;
    height: 0.9rem;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.4;
    stroke-linecap: round;
    stroke-linejoin: round;
  }

  .valuation-node-order-button:disabled {
    cursor: not-allowed;
    opacity: 0.32;
    box-shadow: none;
  }

  .valuation-node-order-button:disabled:hover {
    border-color: color-mix(in srgb, var(--node-colour) 42%, var(--line));
    background: color-mix(in srgb, var(--node-colour) 12%, var(--panel));
    color: color-mix(in srgb, var(--node-colour) 72%, var(--muted));
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

  .valuation-node-colour-shell-disabled {
    border: 1px solid var(--line);
    background: var(--panel);
  }

  .valuation-node-colour-shell-inherited {
    box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--node-colour) 72%, var(--line));
    opacity: 0.72;
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

  .valuation-node-colour-toggle {
    display: inline-flex;
    align-items: center;
    width: 1rem;
    height: 1rem;
  }

  .valuation-node-colour-toggle input {
    width: 1rem;
    height: 1rem;
    margin: 0;
    accent-color: var(--node-colour);
  }

  .valuation-node-colour-toggle-inherited {
    cursor: not-allowed;
    opacity: 0.55;
  }

  .valuation-node-name {
    flex: 1 1 auto;
    min-width: 0;
    max-width: none;
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

  .valuation-node-drop-zone {
    width: 100%;
    height: 0.45rem;
    border-radius: 999px;
  }

  .valuation-node-drop-zone-active {
    background: color-mix(in srgb, var(--accent) 56%, transparent);
  }

  @media (max-width: 900px) {
    .valuation-node-row {
      grid-template-columns: minmax(18rem, 1fr);
    }
  }

  @media (max-width: 640px) {
    .valuation-node-pill,
    .valuation-node-account-panel {
      width: 28rem;
      max-width: 28rem;
      min-width: 28rem;
    }

    .valuation-node-account-grid {
      grid-template-columns: minmax(7rem, 1fr) repeat(4, minmax(3.8rem, 1fr));
    }
  }
</style>
