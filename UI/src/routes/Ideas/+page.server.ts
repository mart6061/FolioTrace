import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getAccounts, getCurrencies, getInstruments } from '$lib/server/api';
import type { HoldingDateBasis, InstrumentPriceBasis } from '$lib/types';

type EnumOption<TValue extends string> = {
  description: string;
  label: string;
  value: TValue;
};

const holdingDateBasisOptions: EnumOption<HoldingDateBasis>[] = [
  { description: 'Execution date and time', label: 'Event Date', value: 'EventDateTime' },
  { description: 'Settlement date and time', label: 'Settlement Date', value: 'SettlementDateTime' }
];
const instrumentPriceBasisOptions: EnumOption<InstrumentPriceBasis>[] = [
  { description: 'Mid price', label: 'Mid', value: 'Mid' },
  { description: 'Bid price', label: 'Bid', value: 'Bid' },
  { description: 'Ask price', label: 'Ask', value: 'Ask' },
  { description: 'Net Asset Value', label: 'NAV', value: 'NAV' }
];

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const valuationDateTime = toApiDateTime(valuationDate);
    const asAtDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    const [accounts, currencies, instruments] = await Promise.all([
      getAccounts(fetch, valuationDateTime, asAtDateTime),
      getCurrencies(fetch, valuationDateTime, asAtDateTime),
      getInstruments(fetch, valuationDateTime, asAtDateTime)
    ]);

    return {
      accounts,
      auditDateTime,
      currencies,
      error: '',
      holdingDateBasisOptions,
      instrumentPriceBasisOptions,
      instruments,
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      auditDateTime,
      currencies: null,
      error: error instanceof Error ? error.message : 'Unable to load ideas.',
      holdingDateBasisOptions,
      instrumentPriceBasisOptions,
      instruments: null,
      valuationDate
    };
  }
};
