import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getAccounts, getBrokers, getCurrencies, getHoldings, getInputPolicies, getInstruments, getTickets } from '$lib/server/api';
import type { HoldingDateBasis, InstrumentPriceBasis } from '$lib/types';
import type { PageServerLoad } from './$types';

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

export const load: PageServerLoad = async ({ fetch, parent, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const { currentUser } = await parent();

  try {
    const valuationDateTime = toApiDateTime(valuationDate);
    const asAtDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    const [accounts, brokers, currencies, holdings, inputPolicies, instruments, tickets] = await Promise.all([
      getAccounts(fetch, valuationDateTime, asAtDateTime),
      getBrokers(fetch, valuationDateTime, asAtDateTime),
      getCurrencies(fetch, valuationDateTime, asAtDateTime),
      getHoldings(fetch, valuationDateTime, asAtDateTime),
      getInputPolicies(fetch, {
        auditDateTime: asAtDateTime,
        controlKinds: ['Quantity', 'Money'],
        currency: 'GBP',
        eventDateTime: valuationDateTime,
        userID: currentUser?.userID
      }),
      getInstruments(fetch, valuationDateTime, asAtDateTime),
      getTickets(fetch, valuationDateTime, asAtDateTime, true)
    ]);

    return {
      accounts,
      auditDateTime,
      brokers,
      currencies,
      error: '',
      holdingDateBasisOptions,
      holdings,
      inputPolicies,
      instrumentPriceBasisOptions,
      instruments,
      tickets,
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      auditDateTime,
      brokers: null,
      currencies: null,
      error: error instanceof Error ? error.message : 'Unable to load template page.',
      holdingDateBasisOptions,
      holdings: null,
      inputPolicies: [],
      instrumentPriceBasisOptions,
      instruments: null,
      tickets: null,
      valuationDate
    };
  }
};
