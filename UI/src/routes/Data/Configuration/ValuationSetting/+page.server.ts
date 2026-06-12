import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getAccounts,
  getApiBaseUrl,
  getValuationSettings,
  postAssetAllocationAccountIDsSetEvent,
  postAssetAllocationActiveSetEvent,
  postAssetAllocationCreatedEvent,
  postAssetAllocationModifiedEvent,
  type AssetAllocationAccountIDsSetRequest,
  type AssetAllocationActiveSetRequest,
  type AssetAllocationCreatedRequest,
  type AssetAllocationModifiedRequest,
  type AssetAllocationNodeRequest
} from '$lib/server/api';

const SPECIAL_NODE_NAME = 'Unallocated';
const SPECIAL_NODE_COLOUR = '#dc2626';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const valuationDateTime = toApiDateTime(valuationDate);
    const asAtDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    const [accounts, valuationSettings] = await Promise.all([
      getAccounts(fetch, valuationDateTime, asAtDateTime),
      getValuationSettings(fetch, valuationDateTime, asAtDateTime)
    ]);

    return {
      accounts,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      valuationDate,
      valuationSettings
    };
  } catch (error) {
    return {
      accounts: null,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load valuation settings.',
      valuationDate,
      valuationSettings: null
    };
  }
};

export const actions = {
  createAllocation: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const name = getFormString(formData, 'name');
    const initialNodeName = getFormString(formData, 'initialNodeName') || name;
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const accountIDs = getFormStrings(formData, 'accountIDs');
    const active = getFormString(formData, 'active') === 'true';
    const values = { accountIDs, active, eventDateTime, initialNodeName, name };

    if (!name || !eventDateTime)
      return fail(400, failure('createAllocation', 'Name and event date are required.', values));

    const rootNodeID = crypto.randomUUID();
    const specialNodeID = crypto.randomUUID();
    const assetNodeID = crypto.randomUUID();
    const nodes: AssetAllocationNodeRequest[] = [
      {
        accountSettings: [],
        hidden: true,
        name,
        nodeID: rootNodeID,
        nodes: [specialNodeID, assetNodeID],
        subtotal: false,
        colour: '#0f766e'
      },
      {
        accountSettings: [],
        hidden: false,
        name: SPECIAL_NODE_NAME,
        nodeID: specialNodeID,
        nodes: [],
        subtotal: false,
        colour: SPECIAL_NODE_COLOUR
      },
      {
        accountSettings: accountIDs.map((accountID) => ({
          accountID,
          targetWeight: 0,
          targetWeightMax: 0,
          targetWeightMin: 0,
          targetYield: 0
        })),
        hidden: false,
        name: initialNodeName,
        nodeID: assetNodeID,
        nodes: [],
        subtotal: false,
        colour: '#0f766e'
      }
    ];

    try {
      const assetAllocationCreatedRequest: AssetAllocationCreatedRequest = {
        accountIDs,
        active,
        eventDateTime: toApiDateTime(eventDateTime),
        name,
        nodes,
        reason: `Create asset allocation ${name}`,
        rootNodeID
      };

      const result = await postAssetAllocationCreatedEvent(fetch, assetAllocationCreatedRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'createAllocation',
        message: `${name} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, failure('createAllocation', error instanceof Error ? error.message : 'Unable to create asset allocation.', values));
    }
  },

  modifyAllocation: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const assetAllocationID = getFormString(formData, 'assetAllocationID');
    const name = getFormString(formData, 'name');
    const rootNodeID = getFormString(formData, 'rootNodeID');
    const nodesJson = getFormString(formData, 'nodesJson');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const values = { assetAllocationID, eventDateTime, name, nodesJson, rootNodeID };

    if (!assetAllocationID || !name || !rootNodeID || !nodesJson || !eventDateTime)
      return fail(400, failure('modifyAllocation', 'Allocation ID, name, root node, node JSON, and event date are required.', values));

    const nodesResult = parseNodes(nodesJson);
    if (!nodesResult.valid)
      return fail(400, failure('modifyAllocation', nodesResult.message, values));

    try {
      const assetAllocationModifiedRequest: AssetAllocationModifiedRequest = {
        assetAllocationID,
        eventDateTime: toApiDateTime(eventDateTime),
        name,
        nodes: normaliseAllocationNodes(nodesResult.nodes, rootNodeID, name),
        reason: `Modify asset allocation ${name}`,
        rootNodeID
      };

      const result = await postAssetAllocationModifiedEvent(fetch, assetAllocationModifiedRequest, currentUser.userID);

      return {
        assetAllocationID,
        eventID: result.eventID,
        intent: 'modifyAllocation',
        message: `${name} was updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, failure('modifyAllocation', error instanceof Error ? error.message : 'Unable to update asset allocation.', values));
    }
  },

  setAccounts: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const assetAllocationID = getFormString(formData, 'assetAllocationID');
    const name = getFormString(formData, 'name');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const accountIDs = getFormStrings(formData, 'accountIDs');
    const values = { accountIDs, assetAllocationID, eventDateTime, name };

    if (!assetAllocationID || !eventDateTime)
      return fail(400, failure('setAccounts', 'Allocation ID and event date are required.', values));

    try {
      const assetAllocationAccountIDsSetRequest: AssetAllocationAccountIDsSetRequest = {
        accountIDs,
        assetAllocationID,
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `Set accounts for asset allocation ${name || assetAllocationID}`
      };

      const result = await postAssetAllocationAccountIDsSetEvent(fetch, assetAllocationAccountIDsSetRequest, currentUser.userID);

      return {
        assetAllocationID,
        eventID: result.eventID,
        intent: 'setAccounts',
        message: 'Allocation accounts were updated successfully.',
        status: 'success'
      };
    } catch (error) {
      return fail(502, failure('setAccounts', error instanceof Error ? error.message : 'Unable to set accounts.', values));
    }
  },

  setActive: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const assetAllocationID = getFormString(formData, 'assetAllocationID');
    const name = getFormString(formData, 'name');
    const active = getFormString(formData, 'active') === 'true';
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const values = { active, assetAllocationID, eventDateTime, name };

    if (!assetAllocationID || !eventDateTime)
      return fail(400, failure('setActive', 'Allocation ID and event date are required.', values));

    try {
      const assetAllocationActiveSetRequest: AssetAllocationActiveSetRequest = {
        active,
        assetAllocationID,
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `${active ? 'Activate' : 'Deactivate'} asset allocation ${name || assetAllocationID}`
      };

      const result = await postAssetAllocationActiveSetEvent(fetch, assetAllocationActiveSetRequest, currentUser.userID);

      return {
        assetAllocationID,
        eventID: result.eventID,
        intent: 'setActive',
        message: `Allocation was ${active ? 'activated' : 'deactivated'} successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, failure('setActive', error instanceof Error ? error.message : 'Unable to update allocation status.', values));
    }
  }
};

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function getFormStrings(formData: FormData, key: string) {
  return formData
    .getAll(key)
    .filter((value): value is string => typeof value === 'string' && value.trim().length > 0)
    .map((value) => value.trim());
}

function failure(intent: string, message: string, values: Record<string, unknown>) {
  return {
    intent,
    message,
    status: 'failure',
    values
  };
}

function parseNodes(nodesJson: string): { valid: true; nodes: AssetAllocationNodeRequest[] } | { valid: false; message: string } {
  try {
    const parsed = JSON.parse(nodesJson) as unknown;

    if (!Array.isArray(parsed))
      return { valid: false, message: 'Node JSON must be an array.' };

    const nodes = parsed.map(normalizeNode);
    if (nodes.some((node) => node === null))
      return { valid: false, message: 'Every node requires nodeID, nodes, name, subtotal, hidden, and accountSettings fields.' };

    return { valid: true, nodes: nodes as AssetAllocationNodeRequest[] };
  } catch {
    return { valid: false, message: 'Node JSON is not valid JSON.' };
  }
}

function normaliseAllocationNodes(nodes: AssetAllocationNodeRequest[], rootNodeID: string, name: string) {
  const nextNodes = nodes.map((node) => ({
    ...node,
    accountSettings: node.accountSettings.map((setting) => ({ ...setting })),
    nodes: [...node.nodes]
  }));
  const rootNode = nextNodes.find((node) => node.nodeID === rootNodeID);

  if (!rootNode)
    return nextNodes;

  rootNode.name = name;

  let specialNode = nextNodes.find((node) => isSpecialNode(node));
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
  specialNode.accountSettings = [];
  specialNode.colour = SPECIAL_NODE_COLOUR;
  specialNode.hidden = false;
  specialNode.name = SPECIAL_NODE_NAME;
  specialNode.nodes = [];
  specialNode.subtotal = false;

  for (const node of nextNodes) {
    if (node.nodeID !== rootNodeID)
      node.nodes = node.nodes.filter((nodeID) => nodeID !== specialNode.nodeID);
  }

  rootNode.nodes = [
    specialNode.nodeID,
    ...specialChildNodes,
    ...rootNode.nodes.filter((nodeID) => nodeID !== rootNodeID && nodeID !== specialNode.nodeID)
  ];

  return nextNodes;
}

function isSpecialNode(node: AssetAllocationNodeRequest) {
  return node.name.trim().toLocaleLowerCase() === SPECIAL_NODE_NAME.toLocaleLowerCase();
}

function normalizeNode(value: unknown): AssetAllocationNodeRequest | null {
  if (!value || typeof value !== 'object')
    return null;

  const record = value as Record<string, unknown>;
  const nodeID = readString(record, 'nodeID', 'NodeID');
  const name = readString(record, 'name', 'Name');
  const nodes = readStringArray(record, 'nodes', 'Nodes');
  const subtotal = readBoolean(record, 'subtotal', 'Subtotal');
  const hidden = readBoolean(record, 'hidden', 'Hidden');
  const colour = readOptionalColour(record);
  const accountSettings = readAccountSettings(record);

  if (!nodeID || !name || accountSettings === null)
    return null;

  return { accountSettings, colour, hidden, name, nodeID, nodes, subtotal };
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
    .filter((setting): setting is AssetAllocationNodeRequest['accountSettings'][number] => setting !== null);
}

function readString(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];
    if (typeof value === 'string')
      return value.trim();
  }

  return '';
}

function readOptionalColour(source: Record<string, unknown>) {
  const colour = 'colour' in source ? source.colour : source.Colour;

  if (colour === null)
    return null;

  if (typeof colour === 'string')
    return colour.trim() || null;

  return undefined;
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
      return value;
    if (typeof value === 'string' && value.trim() !== '') {
      const parsed = Number(value);
      if (Number.isFinite(parsed))
        return parsed;
    }
  }

  return 0;
}
