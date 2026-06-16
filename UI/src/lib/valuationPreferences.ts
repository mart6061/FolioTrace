import type { HoldingDateBasis, UserValuationDateOption, UserValuationPreferences } from '$lib/types';
import { endOfDayForInput, nowForInput, startOfDayForInput } from '$lib/dates';

export type ValuationDateOptionDefinition = {
  value: UserValuationDateOption;
  label: string;
};

export type HoldingDateBasisDefinition = {
  value: HoldingDateBasis;
  label: string;
};

export const valuationDateOptions: ValuationDateOptionDefinition[] = [
  { value: 'TodayEndOfDay', label: 'Today' },
  { value: 'Now', label: 'Now' },
  { value: 'YesterdayEndOfDay', label: 'Yesterday' },
  { value: 'LastWeekEndOfDay', label: 'End of Week' },
  { value: 'LastMonthEndOfDay', label: 'End of Month' },
  { value: 'LastQuarterEndOfDay', label: 'End of Quarter' }
];

export const defaultValuationDateOption: UserValuationDateOption = 'TodayEndOfDay';
export const defaultStartValuationDateOption: UserValuationDateOption = 'TodayEndOfDay';
export const defaultEndValuationDateOption: UserValuationDateOption = 'TodayEndOfDay';
export const defaultHoldingDateBasis: HoldingDateBasis = 'EventDateTime';
export const defaultShowZeroBalances = false;

export const holdingDateBasisOptions: HoldingDateBasisDefinition[] = [
  { value: 'EventDateTime', label: 'Execution' },
  { value: 'SettlementDateTime', label: 'Settlement' }
];

export function defaultUserValuationPreferences(userID = ''): UserValuationPreferences {
  return {
    userID,
    valuationDateOption: defaultValuationDateOption,
    startValuationDateOption: defaultStartValuationDateOption,
    endValuationDateOption: defaultEndValuationDateOption,
    holdingDateBasis: defaultHoldingDateBasis,
    showZeroBalances: defaultShowZeroBalances,
    hasStoredPreferences: false,
    valuationDateTime: '',
    asOfDateTime: '',
    lastEventID: '',
    lastAuditDateTime: ''
  };
}

export function normalizeValuationDateOption(value: string | null | undefined, fallback: UserValuationDateOption = defaultValuationDateOption): UserValuationDateOption {
  return valuationDateOptions.some((option) => option.value === value) ? value as UserValuationDateOption : fallback;
}

export function normalizeHoldingDateBasis(value: string | null | undefined): HoldingDateBasis {
  return value === 'SettlementDateTime' ? 'SettlementDateTime' : defaultHoldingDateBasis;
}

export function valuationStartDateFromOption(option: UserValuationDateOption, now = new Date()) {
  return option === 'Now' ? nowForInput(now) : startOfDayForInput(relativeValuationDate(option, now));
}

export function valuationEndDateFromOption(option: UserValuationDateOption, now = new Date()) {
  return option === 'Now' ? nowForInput(now) : endOfDayForInput(relativeValuationDate(option, now), '0.001');
}

function relativeValuationDate(option: UserValuationDateOption, now: Date) {
  switch (option) {
    case 'YesterdayEndOfDay':
      return new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1);
    case 'LastWeekEndOfDay':
      return new Date(now.getFullYear(), now.getMonth(), now.getDate() - 7);
    case 'LastMonthEndOfDay':
      return new Date(now.getFullYear(), now.getMonth() - 1, now.getDate());
    case 'LastQuarterEndOfDay':
      return new Date(now.getFullYear(), now.getMonth() - 3, now.getDate());
    case 'TodayEndOfDay':
    case 'Now':
    default:
      return now;
  }
}
