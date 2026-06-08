import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getAccounts, getCurrencies, getValuations } from '$lib/server/api';
import { normalizeHoldingDateBasis } from '$lib/valuationPreferences';
import type { Currency, InstrumentPriceBasis } from '$lib/types';

const instrumentPriceBasisOptions: InstrumentPriceBasis[] = ['Mid', 'Bid', 'Ask', 'NAV'];

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const holdingDateBasis = normalizeHoldingDateBasis(url.searchParams.get('holdingDateBasis'));
  const instrumentPriceBasis = normalizeInstrumentPriceBasis(url.searchParams.get('instrumentPriceBasis'));
  const accountID = url.searchParams.get('accountID') || '';
  const apiValuationDate = toApiDateTime(valuationDate);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  let valuationCurrency = 'GBP';

  try {
    const [accounts, currencies] = await Promise.all([
      getAccounts(fetch, apiValuationDate, apiAuditDateTime),
      getCurrencies(fetch, apiValuationDate, apiAuditDateTime)
    ]);
    const accountCurrency = accounts.items.find((account) => account.accountID === accountID)?.bookCurrency;
    valuationCurrency = normalizeValuationCurrency(
      url.searchParams.get('valuationCurrency') || accountCurrency || 'GBP',
      currencies.items
    );
    const valuations = await getValuations(fetch, apiValuationDate, apiAuditDateTime, holdingDateBasis, instrumentPriceBasis, valuationCurrency, accountID || null);

    return {
      accounts,
      accountID,
      auditDateTime,
      currencies,
      error: '',
      holdingDateBasis,
      instrumentPriceBasis,
      instrumentPriceBasisOptions,
      valuationCurrency,
      valuationDate,
      valuations
    };
  } catch (error) {
    return {
      accounts: null,
      accountID,
      auditDateTime,
      currencies: null,
      error: error instanceof Error ? error.message : 'Unable to load valuations.',
      holdingDateBasis,
      instrumentPriceBasis,
      instrumentPriceBasisOptions,
      valuationCurrency,
      valuationDate,
      valuations: null
    };
  }
};

function normalizeInstrumentPriceBasis(value: string | null): InstrumentPriceBasis {
  return instrumentPriceBasisOptions.includes(value as InstrumentPriceBasis) ? value as InstrumentPriceBasis : 'Mid';
}

function normalizeValuationCurrency(value: string, currencies: Currency[]) {
  const normalized = value.trim().toUpperCase();

  if (currencies.some((currency) => currency.alphabeticCode === normalized))
    return normalized;

  if (currencies.some((currency) => currency.alphabeticCode === 'GBP'))
    return 'GBP';

  return currencies[0]?.alphabeticCode ?? 'GBP';
}
