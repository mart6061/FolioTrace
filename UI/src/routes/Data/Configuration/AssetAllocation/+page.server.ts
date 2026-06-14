import { clampFutureInputDateTime, endOfDayForInput, nowForInput, todayEndForInput, toApiDateTime } from '$lib/dates';
import {
  getAccounts,
  getAssetAllocationMappings,
  getHoldingPositions,
  getUserValuationPreferences,
  getValuationSettings,
  postAssetAllocationMappingSetEvent,
  type AssetAllocationMappingSetRequest
} from '$lib/server/api';
import { requireCurrentUser } from '$lib/server/auth';
import { defaultUserValuationPreferences, normalizeHoldingDateBasis } from '$lib/valuationPreferences';
import { fail } from '@sveltejs/kit';
import type { Account, AssetAllocationMapping, AssetAllocationNode, HoldingDateBasis, UserValuationDateOption, ValuationSetting } from '$lib/types';

type MappingChange = {
  holdingID: string;
  nodeID: string;
};

export const load = async ({ fetch, locals, url }) => {
  const currentUser = requireCurrentUser(locals);
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  const valuationPreferences = await loadValuationPreferences(fetch, currentUser.userID, apiAuditDateTime);
  const valuationDate = url.searchParams.get('valuationDate') || valuationDateFromPreference(valuationPreferences.valuationDateOption);
  const apiValuationDate = toApiDateTime(valuationDate);
  const holdingDateBasis = normalizeHoldingDateBasis(url.searchParams.get('holdingDateBasis') || valuationPreferences.holdingDateBasis);

  try {
    const [accounts, valuationSettings] = await Promise.all([
      getAccounts(fetch, apiValuationDate, apiAuditDateTime),
      getValuationSettings(fetch, apiValuationDate, apiAuditDateTime)
    ]);
    const activeAccounts = sortAccounts(accounts.items.filter((account) => account.active));
    const activeValuationSettings = sortValuationSettings(valuationSettings.items.filter((setting) => setting.active));
    const valuationSettingID = selectedID(url.searchParams.get('valuationSettingID'), activeValuationSettings, 'assetAllocationID');
    const selectedValuationSetting = activeValuationSettings.find((setting) => setting.assetAllocationID === valuationSettingID) ?? null;
    const accountIDs = selectedAccountIDs(url.searchParams, activeAccounts, selectedValuationSetting);
    const selectedAccountIDSet = new Set(accountIDs);
    const allocationEffectiveDateTime = selectedValuationSetting?.effectiveDateTime || apiValuationDate;

    const [holdingPositions, assetAllocationMappings] = selectedAccountIDSet.size && valuationSettingID
      ? await Promise.all([
          getHoldingPositions(fetch, allocationEffectiveDateTime, apiAuditDateTime, holdingDateBasis, null, true),
          getAssetAllocationMappings(fetch, allocationEffectiveDateTime, apiAuditDateTime, valuationSettingID, null)
        ])
      : [null, null] as const;

    return {
      accountIDs,
      accounts: activeAccounts,
      assetAllocationMappings,
      auditDateTime,
      error: '',
      holdingDateBasis,
      holdingPositions: holdingPositions
        ? { ...holdingPositions, items: holdingPositions.items.filter((holding) => selectedAccountIDSet.has(holding.accountID)) }
        : null,
      selectedValuationSetting,
      valuationDate,
      valuationPreferences,
      valuationSettingID,
      valuationSettings: activeValuationSettings
    };
  } catch (error) {
    return {
      accountIDs: [],
      accounts: [],
      assetAllocationMappings: null,
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load asset allocation mappings.',
      holdingDateBasis,
      holdingPositions: null,
      selectedValuationSetting: null,
      valuationDate,
      valuationPreferences,
      valuationSettingID: '',
      valuationSettings: []
    };
  }
};

export const actions = {
  saveMappings: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const assetAllocationID = getFormString(formData, 'assetAllocationID');
    const accountIDs = getFormStrings(formData, 'accountIDs');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const auditDateTime = getFormString(formData, 'auditDateTime');
    const mappingsJson = getFormString(formData, 'mappingsJson');
    const values = { accountIDs, assetAllocationID, auditDateTime, eventDateTime, mappingsJson };

    if (!assetAllocationID || !accountIDs.length || !eventDateTime)
      return fail(400, failure('saveMappings', 'Asset allocation, accounts, and event date are required.', values));

    const changesResult = parseMappingChanges(mappingsJson);
    if (!changesResult.valid)
      return fail(400, failure('saveMappings', changesResult.message, values));

    try {
      const apiEventDateTime = toApiDateTime(eventDateTime);
      const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
      const [valuationSettings, currentMappings, holdingPositions] = await Promise.all([
        getValuationSettings(fetch, apiEventDateTime, apiAuditDateTime),
        getAssetAllocationMappings(fetch, apiEventDateTime, apiAuditDateTime, assetAllocationID, null),
        getHoldingPositions(fetch, apiEventDateTime, apiAuditDateTime, 'EventDateTime', null, true)
      ]);
      const valuationSetting = valuationSettings.items.find((setting) => setting.assetAllocationID === assetAllocationID);

      if (!valuationSetting)
        return fail(400, failure('saveMappings', 'Asset allocation was not found.', values));

      const selectedAccountIDSet = new Set(accountIDs);
      const selectedHoldingIDSet = new Set(
        holdingPositions.items
          .filter((holding) => selectedAccountIDSet.has(holding.accountID))
          .map((holding) => holding.holdingID)
      );
      const visibleMappings = currentMappings.items.filter((mapping) => selectedHoldingIDSet.has(mapping.holdingID));
      const changes = mergeMappingChanges([
        ...orphanedMappingChanges(visibleMappings, valuationSetting),
        ...changesResult.changes
      ]);

      if (!changes.length)
        return failure('saveMappings', 'No mapping changes to save.', values);

      const currentNodeByHoldingID = new Map(currentMappings.items.map((mapping) => [mapping.holdingID, mapping.nodeID]));
      const eventIDs: string[] = [];

      for (const change of changes) {
        if (currentNodeByHoldingID.get(change.holdingID) === change.nodeID)
          continue;

        const mappingRequest: AssetAllocationMappingSetRequest = {
          assetAllocationID,
          eventDateTime: apiEventDateTime,
          holdingID: change.holdingID,
          nodeID: change.nodeID,
          reason: 'Set asset allocation mapping'
        };
        const result = await postAssetAllocationMappingSetEvent(fetch, mappingRequest, currentUser.userID);
        eventIDs.push(result.eventID);
      }

      return {
        eventIDs,
        intent: 'saveMappings',
        message: eventIDs.length === 1
          ? '1 mapping was saved successfully.'
          : `${eventIDs.length} mappings were saved successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, failure('saveMappings', error instanceof Error ? error.message : 'Unable to save asset allocation mappings.', values));
    }
  }
};

async function loadValuationPreferences(fetchApi: typeof fetch, userID: string, auditDateTime: string | null) {
  const eventDateTime = toApiDateTime(nowForInput());

  try {
    return await getUserValuationPreferences(fetchApi, userID, eventDateTime, auditDateTime);
  } catch {
    return defaultUserValuationPreferences(userID);
  }
}

function valuationDateFromPreference(option: UserValuationDateOption) {
  const now = new Date();

  switch (option) {
    case 'Now':
      return nowForInput(now);
    case 'YesterdayEndOfDay':
      return endOfDayForInput(new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1));
    case 'LastWeekEndOfDay':
      return endOfDayForInput(new Date(now.getFullYear(), now.getMonth(), now.getDate() - 7));
    case 'LastMonthEndOfDay':
      return endOfDayForInput(new Date(now.getFullYear(), now.getMonth() - 1, now.getDate()));
    case 'LastQuarterEndOfDay':
      return endOfDayForInput(new Date(now.getFullYear(), now.getMonth() - 3, now.getDate()));
    case 'TodayEndOfDay':
    default:
      return todayEndForInput(now);
  }
}

function selectedID<T extends Record<K, string>, K extends keyof T>(value: string | null, items: T[], key: K) {
  if (value && items.some((item) => item[key] === value))
    return value;

  return items[0]?.[key] ?? '';
}

function selectedAccountIDs(searchParams: URLSearchParams, accounts: Account[], valuationSetting: ValuationSetting | null) {
  if (!valuationSetting)
    return [];

  const assignedAccountIDSet = new Set(valuationSetting.accountIDs);
  const allowedAccountIDs = accounts
    .filter((account) => assignedAccountIDSet.has(account.accountID))
    .map((account) => account.accountID);
  const allowedAccountIDSet = new Set(allowedAccountIDs);
  const requestedAccountIDs = [
    ...searchParams.getAll('accountIDs'),
    ...searchParams.getAll('accountID')
  ].filter((accountID) => allowedAccountIDSet.has(accountID));

  if (requestedAccountIDs.length)
    return [...new Set(requestedAccountIDs)];

  return allowedAccountIDs;
}

function sortAccounts(accounts: Account[]) {
  return [...accounts].sort((left, right) => {
    const displayOrder = left.displayOrder - right.displayOrder;
    return displayOrder || left.name.localeCompare(right.name);
  });
}

function sortValuationSettings(settings: ValuationSetting[]) {
  return [...settings].sort((left, right) => left.name.localeCompare(right.name));
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function getFormStrings(formData: FormData, key: string) {
  return formData
    .getAll(key)
    .filter((value): value is string => typeof value === 'string')
    .map((value) => value.trim())
    .filter(Boolean);
}

function parseMappingChanges(value: string): { valid: true; changes: MappingChange[] } | { valid: false; message: string } {
  if (!value)
    return { valid: true, changes: [] };

  try {
    const parsed = JSON.parse(value) as unknown;

    if (!Array.isArray(parsed))
      return { valid: false, message: 'Mapping changes must be an array.' };

    const changesByHoldingID = new Map<string, MappingChange>();

    for (const item of parsed) {
      if (!item || typeof item !== 'object')
        return { valid: false, message: 'Every mapping change requires holdingID and nodeID.' };

      const record = item as Record<string, unknown>;
      const holdingID = typeof record.holdingID === 'string' ? record.holdingID.trim() : '';
      const nodeID = typeof record.nodeID === 'string' ? record.nodeID.trim() : '';

      if (!holdingID || !nodeID)
        return { valid: false, message: 'Every mapping change requires holdingID and nodeID.' };

      changesByHoldingID.set(holdingID, { holdingID, nodeID });
    }

    return { valid: true, changes: [...changesByHoldingID.values()] };
  } catch {
    return { valid: false, message: 'Mapping changes are not valid JSON.' };
  }
}

function orphanedMappingChanges(mappings: AssetAllocationMapping[], valuationSetting: ValuationSetting) {
  const unallocatedNodeID = findUnallocatedNodeID(valuationSetting.nodes, valuationSetting.rootNodeID);

  if (!unallocatedNodeID)
    return [];

  const mappableNodeIDs = findMappableNodeIDs(valuationSetting.nodes, valuationSetting.rootNodeID);

  return mappings
    .filter((mapping) => !mappableNodeIDs.has(mapping.nodeID))
    .map((mapping) => ({
      holdingID: mapping.holdingID,
      nodeID: unallocatedNodeID
    }));
}

function mergeMappingChanges(changes: MappingChange[]) {
  const changesByHoldingID = new Map<string, MappingChange>();

  for (const change of changes)
    changesByHoldingID.set(change.holdingID, change);

  return [...changesByHoldingID.values()];
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
  const mappable = new Set<string>();

  if (!root)
    return mappable;

  visitReachableLeafNodes(root, byID, new Set<string>(), mappable);
  return mappable;
}

function visitReachableLeafNodes(node: AssetAllocationNode, byID: Map<string, AssetAllocationNode>, path: Set<string>, mappable: Set<string>) {
  if (path.has(node.nodeID))
    return;

  const nextPath = new Set(path);
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

function failure(intent: string, message: string, values: Record<string, unknown>) {
  return {
    intent,
    message,
    status: 'failure',
    values
  };
}
