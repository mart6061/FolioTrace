import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getAccounts, getCurrencies, getValuations } from '$lib/server/api';
import { normalizeHoldingDateBasis } from '$lib/valuationPreferences';
import type { Account, Currency, InstrumentPriceBasis, Valuations } from '$lib/types';

const instrumentPriceBasisOptions: InstrumentPriceBasis[] = ['Mid', 'Bid', 'Ask', 'NAV'];

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const holdingDateBasis = normalizeHoldingDateBasis(url.searchParams.get('holdingDateBasis'));
  const instrumentPriceBasis = normalizeInstrumentPriceBasis(url.searchParams.get('instrumentPriceBasis'));
  const assetViewMode = normalizeAssetViewMode(url.searchParams.get('assetViewMode'));
  const apiValuationDate = toApiDateTime(valuationDate);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  let valuationCurrency = 'GBP';
  let accountID = '';
  let accountIDs: string[] = [];

  try {
    const [accounts, currencies] = await Promise.all([
      getAccounts(fetch, apiValuationDate, apiAuditDateTime),
      getCurrencies(fetch, apiValuationDate, apiAuditDateTime)
    ]);
    accountIDs = selectedAccountIDs(url.searchParams, accounts.items);
    accountID = accountIDs.length === 1 ? accountIDs[0] : '';
    const accountCurrency = accounts.items.find((account) => account.accountID === accountID)?.bookCurrency;
    valuationCurrency = normalizeValuationCurrency(
      url.searchParams.get('valuationCurrency') || accountCurrency || 'GBP',
      currencies.items
    );
    const valuations = selectValuationAccounts(
      await getValuations(fetch, apiValuationDate, apiAuditDateTime, holdingDateBasis, instrumentPriceBasis, valuationCurrency, accountID || null),
      accountIDs
    );

    return {
      accounts,
      accountID,
      accountIDs,
      assetViewMode,
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
      accountIDs,
      assetViewMode,
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

function normalizeAssetViewMode(value: string | null): 'Discrete' | 'Aggregate' {
  return value === 'Aggregate' ? 'Aggregate' : 'Discrete';
}

function normalizeValuationCurrency(value: string, currencies: Currency[]) {
  const normalized = value.trim().toUpperCase();

  if (currencies.some((currency) => currency.alphabeticCode === normalized))
    return normalized;

  if (currencies.some((currency) => currency.alphabeticCode === 'GBP'))
    return 'GBP';

  return currencies[0]?.alphabeticCode ?? 'GBP';
}

function selectedAccountIDs(searchParams: URLSearchParams, accounts: Account[]) {
  const allowedAccountIDs = new Set(accounts.map((account) => account.accountID));
  const selectedIDs = [
    ...searchParams.getAll('accountID'),
    ...searchParams.getAll('accountIDs')
  ].filter((accountID) => allowedAccountIDs.has(accountID));

  if (selectedIDs.length)
    return [...new Set(selectedIDs)];

  return accounts.map((account) => account.accountID);
}

function selectValuationAccounts(valuations: Valuations, accountIDs: string[]) {
  if (!accountIDs.length || accountIDs.length === valuations.accounts.length)
    return valuations;

  const selectedAccountIDSet = new Set(accountIDs);
  const accounts = valuations.accounts.filter((account) => selectedAccountIDSet.has(account.accountID));

  return {
    ...valuations,
    accountID: accountIDs.length === 1 ? accountIDs[0] : null,
    accounts,
    totals: accounts.reduce((totals, account) => ({
      bookValue: totals.bookValue + account.totals.bookValue,
      bookCost: totals.bookCost + account.totals.bookCost,
      incompleteCount: totals.incompleteCount + account.totals.incompleteCount
    }), { bookValue: 0, bookCost: 0, incompleteCount: 0 })
  };
}
