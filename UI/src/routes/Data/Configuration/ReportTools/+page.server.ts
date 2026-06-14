import { clampFutureInputDateTime, dateInputToApiStartOfDay, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getAccounts,
  getApiBaseUrl,
  getReportConfigs,
  getValuationSettings,
  postReportCreatedEvent,
  postReportModifiedEvent,
  type ReportCreatedRequest,
  type ReportModifiedRequest,
  type ReportNodeRequest
} from '$lib/server/api';
import type { ReportChartType, ReportNodeType, ReportValuationColumn, ReportValuationColumnKey } from '$lib/types';

const valuationColumnKeys: ReportValuationColumnKey[] = [
  'InstrumentName',
  'ISIN',
  'Sedol',
  'QuotePrice',
  'BookValue',
  'BookCost',
  'Weight',
  'Target',
  'Min',
  'Max'
];

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const showAll = url.searchParams.get('showAll') === 'true';

  try {
    const valuationDateTime = toApiDateTime(valuationDate);
    const asAtDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    const [accounts, reportConfigs, valuationSettings] = await Promise.all([
      getAccounts(fetch, valuationDateTime, asAtDateTime),
      getReportConfigs(fetch, valuationDateTime, asAtDateTime),
      getValuationSettings(fetch, valuationDateTime, asAtDateTime)
    ]);
    const selectedAccountIDs = selectedIDs(url.searchParams, 'accountID', accounts.items.map((account) => account.accountID));

    return {
      accounts,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      reportConfigs: {
        ...reportConfigs,
        items: reportConfigs.items.filter((report) => showAll || report.active)
      },
      selectedAccountIDs,
      showAll,
      valuationDate,
      valuationSettings: {
        ...valuationSettings,
        items: valuationSettings.items.filter((setting) => setting.active)
      }
    };
  } catch (error) {
    return {
      accounts: null,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load report tools.',
      reportConfigs: null,
      selectedAccountIDs: [],
      showAll,
      valuationDate,
      valuationSettings: null
    };
  }
};

export const actions = {
  createReport: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const name = getFormString(formData, 'name') || 'New Report';
    const effectiveDate = getFormString(formData, 'effectiveDate');
    const eventDateTime = effectiveDate ? dateInputToApiStartOfDay(effectiveDate) : '';
    const values = { effectiveDate, name };

    if (!effectiveDate)
      return fail(400, failure('createReport', 'Effective date is required.', values));

    try {
      const reportCreatedRequest: ReportCreatedRequest = {
        active: true,
        effectiveDateTime: eventDateTime,
        eventDateTime,
        name,
        nodes: [],
        reason: `Create report ${name}`
      };

      const result = await postReportCreatedEvent(fetch, reportCreatedRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'createReport',
        message: `${name} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, failure('createReport', error instanceof Error ? error.message : 'Unable to create report.', values));
    }
  },

  saveReport: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const reportID = getFormString(formData, 'reportID');
    const name = getFormString(formData, 'name');
    const active = getFormString(formData, 'active') === 'true';
    const effectiveDate = getFormString(formData, 'effectiveDate');
    const eventDateTime = effectiveDate ? dateInputToApiStartOfDay(effectiveDate) : '';
    const nodesJson = getFormString(formData, 'nodesJson');
    const values = { active, effectiveDate, name, nodesJson, reportID };

    if (!reportID || !name || !effectiveDate || !nodesJson)
      return fail(400, failure('saveReport', 'Report ID, name, effective date, and nodes are required.', values));

    const nodesResult = parseReportNodes(nodesJson);
    if (!nodesResult.valid)
      return fail(400, failure('saveReport', nodesResult.message, values));

    try {
      const reportModifiedRequest: ReportModifiedRequest = {
        active,
        effectiveDateTime: eventDateTime,
        eventDateTime,
        name,
        nodes: nodesResult.nodes,
        reason: `Modify report ${name}`,
        reportID
      };

      const result = await postReportModifiedEvent(fetch, reportModifiedRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'saveReport',
        message: `${name} was updated successfully.`,
        reportID,
        status: 'success'
      };
    } catch (error) {
      return fail(502, failure('saveReport', error instanceof Error ? error.message : 'Unable to save report.', values));
    }
  }
};

function selectedIDs(searchParams: URLSearchParams, key: string, defaults: string[]) {
  const selected = searchParams.getAll(key).filter(Boolean);
  return selected.length ? selected.filter((id) => defaults.includes(id)) : defaults;
}

function parseReportNodes(nodesJson: string): { valid: true; nodes: ReportNodeRequest[] } | { valid: false; message: string } {
  try {
    const parsed = JSON.parse(nodesJson) as unknown;
    if (!Array.isArray(parsed))
      return { valid: false, message: 'Report nodes must be an array.' };

    const nodes = parsed.map(normalizeReportNode);
    if (nodes.some((node) => node === null))
      return { valid: false, message: 'Every report node requires type, node ID, display order, name, and title fields.' };

    return { valid: true, nodes: nodes as ReportNodeRequest[] };
  } catch {
    return { valid: false, message: 'Report nodes are not valid JSON.' };
  }
}

function normalizeReportNode(value: unknown): ReportNodeRequest | null {
  if (!value || typeof value !== 'object')
    return null;

  const record = value as Record<string, unknown>;
  const type = readReportNodeType(record);
  const reportNodeID = readString(record, 'reportNodeID', 'ReportNodeID');
  const displayOrder = readNumber(record, 'displayOrder', 'DisplayOrder');
  const name = readString(record, 'name', 'Name');
  const title = readString(record, 'title', 'Title');
  const assetAllocationID = readString(record, 'assetAllocationID', 'AssetAllocationID');
  const chartType = readChartType(record);
  const columnsResult = type === 'ReportNodeValuation' ? readValuationColumns(record) : { valid: true as const, columns: undefined };

  if (!type || !reportNodeID || displayOrder < 1 || !name || !title || !columnsResult.valid)
    return null;

  const node: ReportNodeRequest = { type, reportNodeID, displayOrder, name, title };

  if (requiresAssetAllocation(type)) {
    if (!assetAllocationID)
      return null;
    node.assetAllocationID = assetAllocationID;
  }

  if (type === 'ReportNodeChart')
    node.chartType = chartType;

  if (type === 'ReportNodeValuation' && columnsResult.columns !== undefined)
    node.columns = columnsResult.columns;

  return node;
}

function requiresAssetAllocation(type: ReportNodeType) {
  return type === 'ReportNodeChart' || type === 'ReportNodeValuation' || type === 'ReportNodeTransactions' || type === 'ReportNodeCash';
}

function readReportNodeType(source: Record<string, unknown>): ReportNodeType | '' {
  const value = readString(source, 'type', 'Type', '$type');
  return isReportNodeType(value) ? value : '';
}

function isReportNodeType(value: string): value is ReportNodeType {
  return ['ReportNodeCoverPage', 'ReportNodeIndex', 'ReportNodeChart', 'ReportNodeValuation', 'ReportNodeTransactions', 'ReportNodeCash'].includes(value);
}

function readChartType(source: Record<string, unknown>): ReportChartType {
  const value = readString(source, 'chartType', 'ChartType');
  return value === 'Bar' ? 'Bar' : 'Pie';
}

function readValuationColumns(source: Record<string, unknown>): { valid: true; columns?: ReportValuationColumn[] | null } | { valid: false } {
  const value = source.columns ?? source.Columns;

  if (value === undefined)
    return { valid: true, columns: undefined };

  if (value === null)
    return { valid: true, columns: null };

  if (!Array.isArray(value))
    return { valid: false };

  const columns: ReportValuationColumn[] = [];
  for (const [index, item] of value.entries()) {
    if (!item || typeof item !== 'object')
      return { valid: false };

    const record = item as Record<string, unknown>;
    const columnKey = readString(record, 'columnKey', 'ColumnKey');
    const displayOrder = readNumber(record, 'displayOrder', 'DisplayOrder') || index + 1;

    if (!isValuationColumnKey(columnKey) || displayOrder < 1)
      return { valid: false };

    columns.push({ columnKey, displayOrder });
  }

  return {
    valid: true,
    columns: columns
      .sort((left, right) => left.displayOrder - right.displayOrder)
      .map((column, index) => ({ columnKey: column.columnKey, displayOrder: index + 1 }))
  };
}

function isValuationColumnKey(value: string): value is ReportValuationColumnKey {
  return valuationColumnKeys.includes(value as ReportValuationColumnKey);
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function readString(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];
    if (typeof value === 'string')
      return value.trim();
  }

  return '';
}

function readNumber(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];
    if (typeof value === 'number' && Number.isFinite(value))
      return value;
    if (typeof value === 'string' && value.trim() !== '') {
      const parsed = Number(value);
      if (Number.isFinite(parsed))
        return parsed;
    }
  }

  return 0;
}

function failure(intent: string, message: string, values: Record<string, unknown>) {
  return {
    intent,
    message,
    status: 'failure',
    values
  };
}
