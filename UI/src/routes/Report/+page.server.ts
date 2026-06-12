import { clampFutureInputDateTime, endOfDayForInput, nowForInput, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getAccounts, getUserValuationPreferences, getValuationSettings } from '$lib/server/api';
import { requireCurrentUser } from '$lib/server/auth';
import { defaultUserValuationPreferences, normalizeHoldingDateBasis } from '$lib/valuationPreferences';
import type { Account, InstrumentPriceBasis, UserValuationDateOption, ValuationSetting } from '$lib/types';

const instrumentPriceBasisOptions: InstrumentPriceBasis[] = ['Mid', 'Bid', 'Ask', 'NAV'];

export const load = async ({ fetch, locals, url }) => {
  const currentUser = requireCurrentUser(locals);
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  const valuationPreferences = await loadValuationPreferences(fetch, currentUser.userID, apiAuditDateTime);
  const valuationDate = url.searchParams.get('valuationDate') || valuationDateFromPreference(valuationPreferences.valuationDateOption);
  const apiValuationDate = toApiDateTime(valuationDate);
  const holdingDateBasis = normalizeHoldingDateBasis(url.searchParams.get('holdingDateBasis') || valuationPreferences.holdingDateBasis);
  const instrumentPriceBasis = normalizeInstrumentPriceBasis(url.searchParams.get('instrumentPriceBasis'));

  try {
    const [accounts, valuationSettings] = await Promise.all([
      getAccounts(fetch, apiValuationDate, apiAuditDateTime),
      getValuationSettings(fetch, apiValuationDate, apiAuditDateTime)
    ]);
    const activeAccounts = sortAccounts(accounts.items.filter((account) => account.active));
    const activeValuationSettings = sortValuationSettings(valuationSettings.items.filter((setting) => setting.active));
    const accountID = selectedID(url.searchParams.get('accountID'), activeAccounts, 'accountID');
    const valuationSettingID = selectedID(url.searchParams.get('valuationSettingID'), activeValuationSettings, 'assetAllocationID');

    return {
      accountID,
      accounts: activeAccounts,
      auditDateTime,
      error: '',
      holdingDateBasis,
      instrumentPriceBasis,
      instrumentPriceBasisOptions,
      valuationDate,
      valuationPreferences,
      valuationSettingID,
      valuationSettings: activeValuationSettings
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
      valuationDate,
      valuationPreferences,
      valuationSettingID: '',
      valuationSettings: []
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

function sortValuationSettings(settings: ValuationSetting[]) {
  return [...settings].sort((left, right) => left.name.localeCompare(right.name));
}
