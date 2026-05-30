import { systemUserID } from '$lib/menuPreferences';
import type { UserValuationDateOption, UserValuationPreferences, ValuationDateBasis } from '$lib/types';

export type ValuationDateOptionDefinition = {
  value: UserValuationDateOption;
  label: string;
};

export type ValuationDateBasisDefinition = {
  value: ValuationDateBasis;
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
export const defaultValuationDateBasis: ValuationDateBasis = 'EventDateTime';
export const defaultShowZeroBalances = false;

export const valuationDateBasisOptions: ValuationDateBasisDefinition[] = [
  { value: 'EventDateTime', label: 'Execution' },
  { value: 'SettlementDateTime', label: 'Settlement' }
];

export function defaultUserValuationPreferences(): UserValuationPreferences {
  return {
    userID: systemUserID,
    valuationDateOption: defaultValuationDateOption,
    valuationDateBasis: defaultValuationDateBasis,
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

export function normalizeValuationDateBasis(value: string | null | undefined): ValuationDateBasis {
  return value === 'SettlementDateTime' ? 'SettlementDateTime' : defaultValuationDateBasis;
}
