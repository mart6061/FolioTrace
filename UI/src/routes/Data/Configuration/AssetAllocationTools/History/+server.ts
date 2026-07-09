import { getValuationSettingEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ fetch, url }) => {
  const assetAllocationID = (url.searchParams.get('assetAllocationID') || '').trim();

  if (!assetAllocationID)
    return json({ message: 'Asset allocation ID is required.' }, { status: 400 });

  const history = (await getValuationSettingEvents(fetch, {
    assetAllocationID,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  })).map(normalizeValuationSettingEvent);

  return json(history);
};

function normalizeValuationSettingEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    accountIDs: readStringArray(event, 'accountIDs', 'AccountIDs'),
    active: readOptionalBoolean(event, 'active', 'Active'),
    applicationStatus: readApplicationStatus(event),
    assetAllocationID: readString(event, 'assetAllocationID', 'assetAllocationId', 'AssetAllocationID'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    name: readOptionalString(event, 'name', 'Name'),
    nodes: readNodes(event),
    propertyDetails: readEventPropertyDetails(event),
    reason: readString(event, 'reason', 'Reason'),
    rootNodeID: readOptionalString(event, 'rootNodeID', 'rootNodeId', 'RootNodeID'),
    userID: readString(event, 'userID', 'userId', 'UserID')
  };
}

function readNodes(source: Record<string, unknown>) {
  const value = source.nodes ?? source.Nodes;
  if (!Array.isArray(value))
    return undefined;

  return value
    .map((node) => {
      if (!node || typeof node !== 'object')
        return null;

      const record = node as Record<string, unknown>;
      return {
        accountSettings: readAccountSettings(record),
        colour: readOptionalColour(record),
        hidden: readBoolean(record, 'hidden', 'Hidden'),
        name: readString(record, 'name', 'Name'),
        nodeID: readString(record, 'nodeID', 'nodeId', 'NodeID'),
        nodes: readStringArray(record, 'nodes', 'Nodes'),
        subtotal: readBoolean(record, 'subtotal', 'Subtotal')
      };
    })
    .filter((node) => node !== null);
}

function readAccountSettings(source: Record<string, unknown>) {
  const value = source.accountSettings ?? source.AccountSettings;
  if (!Array.isArray(value))
    return [];

  return value
    .map((setting) => {
      if (!setting || typeof setting !== 'object')
        return null;

      const record = setting as Record<string, unknown>;
      return {
        accountID: readString(record, 'accountID', 'accountId', 'AccountID'),
        targetWeight: readNullableNumber(record, 'targetWeight', 'TargetWeight'),
        targetWeightMax: readNullableNumber(record, 'targetWeightMax', 'TargetWeightMax'),
        targetWeightMin: readNullableNumber(record, 'targetWeightMin', 'TargetWeightMin'),
        targetYield: readNullableNumber(record, 'targetYield', 'TargetYield')
      };
    })
    .filter((setting) => setting !== null);
}

function readString(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'string')
      return value;
  }

  return '';
}

function readOptionalString(source: Record<string, unknown>, ...keys: string[]) {
  const value = readString(source, ...keys);
  return value || undefined;
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
      return value.filter((item): item is string => typeof item === 'string');
  }

  return [];
}

function readBoolean(source: Record<string, unknown>, ...keys: string[]) {
  return readOptionalBoolean(source, ...keys) ?? false;
}

function readOptionalBoolean(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'boolean')
      return value;
  }

  return undefined;
}

function readNullableNumber(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (value === null)
      return null;

    if (typeof value === 'number')
      return Number.isFinite(value) ? value : null;

    if (typeof value === 'string' && value.trim() !== '') {
      const parsed = Number(value);
      if (Number.isFinite(parsed))
        return parsed;
    }
  }

  return null;
}
