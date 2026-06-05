import type { HoldingDateBasis, UserValuationDateOption, UserValuationPreferences } from '$lib/types';

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
    holdingDateBasis: defaultHoldingDateBasis,
    showZeroBalances: defaultShowZeroBalances,
    hasStoredPreferences: false,
    valuationDateTime: '',
    asOfDateTime: '',
    lastEventID: '',
    lastAuditDateTime: ''
  };
}

export function normalizeValuationDateOption(value: string | null | undefined): UserValuationDateOption {
  return valuationDateOptions.some((option) => option.value === value) ? value as UserValuationDateOption : defaultValuationDateOption;
}

export function normalizeHoldingDateBasis(value: string | null | undefined): HoldingDateBasis {
  return value === 'SettlementDateTime' ? 'SettlementDateTime' : defaultHoldingDateBasis;
}
