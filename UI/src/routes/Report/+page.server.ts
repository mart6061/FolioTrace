import { clampFutureInputDateTime, endOfDayForInput, nowForInput, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getAccounts, getReportConfigs, getUserValuationPreferences, getValuationSettings } from '$lib/server/api';
import { requireCurrentUser } from '$lib/server/auth';
import { defaultUserValuationPreferences, normalizeHoldingDateBasis } from '$lib/valuationPreferences';
import type { Account, InstrumentPriceBasis, ReportConfig, ReportNodePageOrientation, UserValuationDateOption, ValuationSetting } from '$lib/types';

const instrumentPriceBasisOptions: InstrumentPriceBasis[] = ['Mid', 'Bid', 'Ask', 'NAV'];

type ReportDocumentSection = {
  reportNodeID: string;
  name: string;
  pageOrientation: ReportNodePageOrientation;
};

type ReportDocument = {
  reportID: string;
  name: string;
  valuationHeading: string;
  sections: ReportDocumentSection[];
};

export const load = async ({ fetch, locals, url }) => {
  const currentUser = requireCurrentUser(locals);
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  const valuationPreferences = await loadValuationPreferences(fetch, currentUser.userID, apiAuditDateTime);
  const valuationDate = url.searchParams.get('valuationDate') || valuationDateFromPreference(valuationPreferences.valuationDateOption);
  const apiValuationDate = toApiDateTime(valuationDate);
  const holdingDateBasis = normalizeHoldingDateBasis(url.searchParams.get('holdingDateBasis') || valuationPreferences.holdingDateBasis);
  const instrumentPriceBasis = normalizeInstrumentPriceBasis(url.searchParams.get('instrumentPriceBasis'));
  const shouldCreateDocument = url.searchParams.get('create') === 'true';

  try {
    const [accounts, valuationSettings, reportConfigs] = await Promise.all([
      getAccounts(fetch, apiValuationDate, apiAuditDateTime),
      getValuationSettings(fetch, apiValuationDate, apiAuditDateTime),
      getReportConfigs(fetch, apiValuationDate, apiAuditDateTime)
    ]);
    const activeAccounts = sortAccounts(accounts.items.filter((account) => account.active));
    const activeValuationSettings = valuationSettings.items.filter((setting) => setting.active);
    const accountID = selectedID(url.searchParams.get('accountID'), activeAccounts, 'accountID');
    const matchingReportConfigs = sortReportConfigs(filterReportConfigsForAccount(reportConfigs.items, activeValuationSettings, accountID));
    const reportID = selectedID(url.searchParams.get('reportID'), matchingReportConfigs, 'reportID');
    const selectedReportConfig = matchingReportConfigs.find((reportConfig) => reportConfig.reportID === reportID) ?? null;

    return {
      accountID,
      accounts: activeAccounts,
      auditDateTime,
      error: '',
      holdingDateBasis,
      instrumentPriceBasis,
      instrumentPriceBasisOptions,
      reportDocument: shouldCreateDocument && selectedReportConfig ? createReportDocument(selectedReportConfig, valuationDate) : null,
      reportConfigs: matchingReportConfigs,
      reportID,
      valuationDate,
      valuationPreferences
    };
  } catch (error) {
    return {
      accountID: '',
      accounts: [],
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load report options.',
      holdingDateBasis,
      instrumentPriceBasis,
      instrumentPriceBasisOptions,
      reportDocument: null,
      reportConfigs: [],
      reportID: '',
      valuationDate,
      valuationPreferences
    };
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

function normalizeInstrumentPriceBasis(value: string | null): InstrumentPriceBasis {
  return instrumentPriceBasisOptions.includes(value as InstrumentPriceBasis) ? value as InstrumentPriceBasis : 'Mid';
}

function selectedID<T extends Record<K, string>, K extends keyof T>(value: string | null, items: T[], key: K) {
  if (value && items.some((item) => item[key] === value))
    return value;

  return items[0]?.[key] ?? '';
}

function sortAccounts(accounts: Account[]) {
  return [...accounts].sort((left, right) => {
    const displayOrder = left.displayOrder - right.displayOrder;
    return displayOrder || left.name.localeCompare(right.name);
  });
}

function filterReportConfigsForAccount(reportConfigs: ReportConfig[], valuationSettings: ValuationSetting[], accountID: string) {
  if (!accountID)
    return [];

  const valuationSettingByID = new Map(valuationSettings.map((setting) => [setting.assetAllocationID, setting]));

  return reportConfigs.filter((reportConfig) => {
    if (!reportConfig.active)
      return false;

    const assetAllocationIDs = reportConfig.nodes
      .map((node) => node.assetAllocationID)
      .filter((assetAllocationID): assetAllocationID is string => Boolean(assetAllocationID));

    if (!assetAllocationIDs.length)
      return true;

    return assetAllocationIDs.some((assetAllocationID) => valuationSettingByID.get(assetAllocationID)?.accountIDs.includes(accountID));
  });
}

function sortReportConfigs(reportConfigs: ReportConfig[]) {
  return [...reportConfigs].sort((left, right) => left.name.localeCompare(right.name));
}

function createReportDocument(reportConfig: ReportConfig, valuationDate: string): ReportDocument {
  return {
    reportID: reportConfig.reportID,
    name: reportConfig.name,
    valuationHeading: formatReportValuationHeading(valuationDate),
    sections: [...reportConfig.nodes]
      .sort((left, right) => left.displayOrder - right.displayOrder || left.name.localeCompare(right.name))
      .map((node) => ({
        reportNodeID: node.reportNodeID,
        name: node.name,
        pageOrientation: node.pageOrientation ?? 'Portrait'
      }))
  };
}

function formatReportValuationHeading(valuationDate: string) {
  const datePart = valuationDate.split('T')[0];
  const date = new Date(`${datePart}T00:00:00`);

  if (Number.isNaN(date.getTime()))
    return 'Valuation';

  return `Valuation ${date.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}`;
}
