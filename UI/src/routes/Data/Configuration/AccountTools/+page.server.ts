import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getAccounts,
  getApiBaseUrl,
  getCurrencies,
  getHoldings,
  getInstruments,
  getTransactionEvents,
  getValuationSettings,
  postAccountActiveModifiedEvent,
  postAccountCreatedEvent,
  postAccountModifiedEvent,
  postAssetAllocationAccountIDsSetEvent,
  postHoldingActiveModifiedEvent,
  postHoldingCreatedEvent,
  postHoldingModifiedEvent,
  type AccountActiveModifiedRequest,
  type AccountCreatedRequest,
  type AccountModifiedRequest,
  type AssetAllocationAccountIDsSetRequest,
  type HoldingCreatedRequest,
  type HoldingModifiedRequest
} from '$lib/server/api';
import type { Holding, HoldingKind, Instrument, ProfitLossMethod } from '$lib/types';

type NominalHoldingKind = Extract<
  HoldingKind,
  'NominalInflow' | 'NominalOutflow' | 'NominalInSpecieIn' | 'NominalInSpecieOut' | 'NominalFeesCustodian' | 'NominalFeesAdministrator' | 'NominalFeesBank' | 'NominalIncome' | 'NominalInterest'
>;

const REQUIRED_NOMINAL_HOLDINGS: { kind: NominalHoldingKind; name: string }[] = [
  { kind: 'NominalInflow', name: 'Inflow' },
  { kind: 'NominalOutflow', name: 'Outflow' },
  { kind: 'NominalInSpecieIn', name: 'In Specie In' },
  { kind: 'NominalInSpecieOut', name: 'In Specie Out' },
  { kind: 'NominalFeesCustodian', name: 'Custodian Fees' },
  { kind: 'NominalFeesAdministrator', name: 'Administrator Fees' },
  { kind: 'NominalFeesBank', name: 'Bank Fees' },
  { kind: 'NominalIncome', name: 'Income' },
  { kind: 'NominalInterest', name: 'Interest' }
];

const BANK_HOLDING_KINDS = new Set<HoldingKind>(['CashDebt', 'CashInvestable', 'CashNonInvestable']);
const NOMINAL_HOLDING_KIND_SET = new Set<HoldingKind>(REQUIRED_NOMINAL_HOLDINGS.map((holding) => holding.kind));

function normaliseProfitLossMethod(value: string): ProfitLossMethod {
  return value === 'LIFO' || value === 'RunningAverage' ? value : 'FIFO';
}

export const load: PageServerLoad = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const valuationDateTime = toApiDateTime(valuationDate);
    const asAtDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    const [accounts, currencies, holdings, instruments, transactionEvents, valuationSettings] = await Promise.all([
      getAccounts(fetch, valuationDateTime, asAtDateTime),
      getCurrencies(fetch, valuationDateTime, asAtDateTime),
      getHoldings(fetch, valuationDateTime, asAtDateTime, true),
      getInstruments(fetch, valuationDateTime, asAtDateTime),
      getTransactionEvents(fetch, { valuationDateTime, auditDateTime: asAtDateTime ?? undefined }),
      getValuationSettings(fetch, valuationDateTime, asAtDateTime)
    ]);

    return {
      accounts,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      currencies,
      error: '',
      holdings,
      instruments,
      transactionAccountIDs: [...new Set(transactionEvents.filter((event) => event.accountID && event.applicationStatus !== 'omitted').map((event) => event.accountID!))],
      valuationDate,
      valuationSettings
    };
  } catch (error) {
    return {
      accounts: null,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      currencies: null,
      error: error instanceof Error ? error.message : 'Unable to load account tools.',
      holdings: null,
      instruments: null,
      transactionAccountIDs: [],
      valuationDate,
      valuationSettings: null
    };
  }
};

export const actions: Actions = {
  createAccount: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const eventDateTimeInput = getFormString(formData, 'eventDateTime');
    const accountID = crypto.randomUUID();
    const name = getFormString(formData, 'name');
    const formalName = getFormString(formData, 'formalName');
    const bookCurrency = getFormString(formData, 'bookCurrency');
    const bookCostBasis = normaliseProfitLossMethod(getFormString(formData, 'bookCostBasis'));
    const active = getFormString(formData, 'active') === 'true';
    const bankFields = getBankFields(formData);
    const values = { active, bankFields, bookCostBasis, bookCurrency, eventDateTime: eventDateTimeInput, formalName, name };

    if (!eventDateTimeInput || !name || !formalName || !bookCurrency)
      return fail(400, failure('createAccount', 'Start date, name, formal name, and book currency are required.', values));

    const bankValidation = validateBankFields(bankFields);
    if (bankValidation)
      return fail(400, failure('createAccount', bankValidation, values));

    try {
      const eventDateTime = toApiDateTime(eventDateTimeInput);
      const state = await loadReferenceState(fetch, eventDateTime);
      const instrument = findCurrencyInstrument(state.instruments.items, bookCurrency);
      if (!instrument)
        return fail(400, failure('createAccount', `No active instrument priced in ${bookCurrency} was found for the default holdings.`, values));

      const accountCreatedRequest: AccountCreatedRequest = {
        accountID,
        active,
        bookCostBasis,
        bookCurrency,
        eventDateTime,
        formalName,
        name,
        reason: `Create account ${name}`
      };

      const accountResult = await postAccountCreatedEvent(fetch, accountCreatedRequest, currentUser.userID);
      await createRequiredHoldings(fetch, currentUser.userID, eventDateTime, accountID, bookCurrency, instrument.instrumentID, [], state.instruments.items, bankFields);

      return {
        accountID,
        eventID: accountResult.eventID,
        intent: 'createAccount',
        message: `${name} was created with required holdings.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, failure('createAccount', error instanceof Error ? error.message : 'Unable to create account.', values));
    }
  },

  modifyAccount: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const accountID = getFormString(formData, 'accountID');
    const eventDateTimeInput = getFormString(formData, 'eventDateTime');
    const name = getFormString(formData, 'name');
    const formalName = getFormString(formData, 'formalName');
    const bookCostBasis = normaliseProfitLossMethod(getFormString(formData, 'bookCostBasis'));
    const values = { accountID, bookCostBasis, eventDateTime: eventDateTimeInput, formalName, name };

    if (!accountID || !eventDateTimeInput || !name || !formalName)
      return fail(400, failure('modifyAccount', 'Account, start date, name, and formal name are required.', values));

    try {
      const accountModifiedRequest: AccountModifiedRequest = {
        accountID,
        bookCostBasis,
        eventDateTime: toApiDateTime(eventDateTimeInput),
        formalName,
        name,
        reason: `Update account ${name}`
      };

      const result = await postAccountModifiedEvent(fetch, accountModifiedRequest, currentUser.userID);
      return success('modifyAccount', result.eventID, `${name} was updated.`);
    } catch (error) {
      return fail(502, failure('modifyAccount', error instanceof Error ? error.message : 'Unable to update account.', values));
    }
  },

  setAccountActive: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const accountID = getFormString(formData, 'accountID');
    const accountName = getFormString(formData, 'accountName');
    const eventDateTimeInput = getFormString(formData, 'eventDateTime');
    const active = getFormString(formData, 'active') === 'true';
    const values = { accountID, active, eventDateTime: eventDateTimeInput };

    if (!accountID || !eventDateTimeInput)
      return fail(400, failure('setAccountActive', 'Account and start date are required.', values));

    try {
      const accountActiveRequest: AccountActiveModifiedRequest = {
        accountID,
        active,
        eventDateTime: toApiDateTime(eventDateTimeInput),
        reason: `${active ? 'Set' : 'Unset'} active account ${accountName || accountID}`
      };

      const result = await postAccountActiveModifiedEvent(fetch, accountActiveRequest, currentUser.userID);
      return success('setAccountActive', result.eventID, `${accountName || 'Account'} was ${active ? 'activated' : 'deactivated'}.`);
    } catch (error) {
      return fail(502, failure('setAccountActive', error instanceof Error ? error.message : 'Unable to update account status.', values));
    }
  },

  ensureRequiredHoldings: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const accountID = getFormString(formData, 'accountID');
    const eventDateTimeInput = getFormString(formData, 'eventDateTime');
    const bankFields = getBankFields(formData);
    const values = { accountID, bankFields, eventDateTime: eventDateTimeInput };

    if (!accountID || !eventDateTimeInput)
      return fail(400, failure('ensureRequiredHoldings', 'Account and start date are required.', values));

    try {
      const eventDateTime = toApiDateTime(eventDateTimeInput);
      const state = await loadReferenceState(fetch, eventDateTime);
      const account = state.accounts.items.find((item) => item.accountID === accountID);
      if (!account)
        return fail(400, failure('ensureRequiredHoldings', 'Account was not found.', values));

      const instrument = findCurrencyInstrument(state.instruments.items, account.bookCurrency);
      if (!instrument)
        return fail(400, failure('ensureRequiredHoldings', `No active instrument priced in ${account.bookCurrency} was found for required holdings.`, values));

      const accountHoldings = state.holdings.items.filter((holding) => holding.accountID === accountID);
      const needsCash = !hasBookCurrencyCashInvestable(accountHoldings, state.instruments.items, account.bookCurrency);
      if (needsCash) {
        const bankValidation = validateBankFields(bankFields);
        if (bankValidation)
          return fail(400, failure('ensureRequiredHoldings', bankValidation, values));
      }

      const createdCount = await createRequiredHoldings(fetch, currentUser.userID, eventDateTime, accountID, account.bookCurrency, instrument.instrumentID, accountHoldings, state.instruments.items, bankFields);
      return success('ensureRequiredHoldings', '', createdCount > 0 ? `Created ${createdCount} required holding${createdCount === 1 ? '' : 's'}.` : 'Required holdings are already present.');
    } catch (error) {
      return fail(502, failure('ensureRequiredHoldings', error instanceof Error ? error.message : 'Unable to ensure required holdings.', values));
    }
  },

  createHolding: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const eventDateTimeInput = getFormString(formData, 'eventDateTime');
    const accountID = getFormString(formData, 'accountID');
    const holdingKind = getFormString(formData, 'holdingKind') as HoldingKind;
    const instrumentID = getFormString(formData, 'instrumentID');
    const name = getFormString(formData, 'name');
    const makeDefault = getFormString(formData, 'default') === 'true';
    const active = getFormString(formData, 'active') === 'true';
    const bankFields = getBankFields(formData);
    const values = { accountID, active, bankFields, default: makeDefault, eventDateTime: eventDateTimeInput, holdingKind, instrumentID, name };

    if (!accountID || !eventDateTimeInput || !holdingKind || !instrumentID || !name)
      return fail(400, failure('createHolding', 'Account, start date, holding kind, instrument, and name are required.', values));
    if (BANK_HOLDING_KINDS.has(holdingKind)) {
      const bankValidation = validateBankFields(bankFields);
      if (bankValidation)
        return fail(400, failure('createHolding', bankValidation, values));
    }

    try {
      const eventDateTime = toApiDateTime(eventDateTimeInput);
      const state = await loadReferenceState(fetch, eventDateTime);
      if (makeDefault)
        await unsetCurrentDefault(fetch, currentUser.userID, eventDateTime, accountID, holdingKind, instrumentID, state.holdings.items);

      const holdingCreatedRequest = withBankFields(
        {
          accountID,
          active,
          default: makeDefault,
          eventDateTime,
          holdingID: crypto.randomUUID(),
          holdingKind,
          instrumentID,
          name,
          reason: `Create ${holdingKind} holding ${name}`
        },
        bankFields
      );
      const result = await postHoldingCreatedEvent(fetch, holdingCreatedRequest, currentUser.userID);
      return success('createHolding', result.eventID, `${name} was created.`);
    } catch (error) {
      return fail(502, failure('createHolding', error instanceof Error ? error.message : 'Unable to create holding.', values));
    }
  },

  setHoldingDefault: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const holdingID = getFormString(formData, 'holdingID');
    const eventDateTimeInput = getFormString(formData, 'eventDateTime');
    const values = { eventDateTime: eventDateTimeInput, holdingID };

    if (!holdingID || !eventDateTimeInput)
      return fail(400, failure('setHoldingDefault', 'Holding and start date are required.', values));

    try {
      const eventDateTime = toApiDateTime(eventDateTimeInput);
      const state = await loadReferenceState(fetch, eventDateTime);
      const holding = state.holdings.items.find((item) => item.holdingID === holdingID);
      if (!holding)
        return fail(400, failure('setHoldingDefault', 'Holding was not found.', values));

      await unsetCurrentDefault(fetch, currentUser.userID, eventDateTime, holding.accountID, holding.holdingKind, holding.instrumentID, state.holdings.items, holding.holdingID);
      const result = await postHoldingModifiedEvent(fetch, holdingModifiedRequest(holding, eventDateTime, true), currentUser.userID);
      return success('setHoldingDefault', result.eventID, `${holding.name || holding.holdingKind} is now the default.`);
    } catch (error) {
      return fail(502, failure('setHoldingDefault', error instanceof Error ? error.message : 'Unable to set holding default.', values));
    }
  },

  setHoldingActive: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const holdingID = getFormString(formData, 'holdingID');
    const holdingName = getFormString(formData, 'holdingName');
    const eventDateTimeInput = getFormString(formData, 'eventDateTime');
    const active = getFormString(formData, 'active') === 'true';
    const values = { active, eventDateTime: eventDateTimeInput, holdingID };

    if (!holdingID || !eventDateTimeInput)
      return fail(400, failure('setHoldingActive', 'Holding and start date are required.', values));

    try {
      const result = await postHoldingActiveModifiedEvent(
        fetch,
        {
          active,
          eventDateTime: toApiDateTime(eventDateTimeInput),
          holdingID,
          reason: `${active ? 'Set' : 'Unset'} active holding ${holdingName || holdingID}`
        },
        currentUser.userID
      );
      return success('setHoldingActive', result.eventID, `${holdingName || 'Holding'} was ${active ? 'activated' : 'deactivated'}.`);
    } catch (error) {
      return fail(502, failure('setHoldingActive', error instanceof Error ? error.message : 'Unable to update holding status.', values));
    }
  },

  linkAllocation: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const assetAllocationID = getFormString(formData, 'assetAllocationID');
    const accountID = getFormString(formData, 'accountID');
    const eventDateTimeInput = getFormString(formData, 'eventDateTime');
    const link = getFormString(formData, 'link') === 'true';
    const values = { accountID, assetAllocationID, eventDateTime: eventDateTimeInput, link };

    if (!assetAllocationID || !accountID || !eventDateTimeInput)
      return fail(400, failure('linkAllocation', 'Allocation, account, and start date are required.', values));

    try {
      const eventDateTime = toApiDateTime(eventDateTimeInput);
      const state = await loadReferenceState(fetch, eventDateTime);
      const allocation = state.valuationSettings.items.find((item) => item.assetAllocationID === assetAllocationID);
      if (!allocation)
        return fail(400, failure('linkAllocation', 'Asset allocation was not found.', values));

      const accountIDs = new Set(allocation.accountIDs);
      if (link)
        accountIDs.add(accountID);
      else
        accountIDs.delete(accountID);

      const accountIDsSetRequest: AssetAllocationAccountIDsSetRequest = {
        accountIDs: [...accountIDs],
        assetAllocationID
      };
      const result = await postAssetAllocationAccountIDsSetEvent(fetch, accountIDsSetRequest, currentUser.userID);
      return success('linkAllocation', result.eventID, `Allocation was ${link ? 'linked' : 'unlinked'}.`);
    } catch (error) {
      return fail(502, failure('linkAllocation', error instanceof Error ? error.message : 'Unable to update allocation link.', values));
    }
  }
};

async function loadReferenceState(fetchApi: typeof fetch, eventDateTime: string) {
  const [accounts, holdings, instruments, valuationSettings] = await Promise.all([
    getAccounts(fetchApi, eventDateTime, null),
    getHoldings(fetchApi, eventDateTime, null, true),
    getInstruments(fetchApi, eventDateTime, null),
    getValuationSettings(fetchApi, eventDateTime, null)
  ]);

  return { accounts, holdings, instruments, valuationSettings };
}

async function createRequiredHoldings(
  fetchApi: typeof fetch,
  userID: string,
  eventDateTime: string,
  accountID: string,
  bookCurrency: string,
  currencyInstrumentID: string,
  existingHoldings: Holding[],
  instruments: Instrument[],
  bankFields: BankFields
) {
  let createdCount = 0;
  const accountHoldings = existingHoldings.filter((holding) => holding.accountID === accountID);

  if (!hasBookCurrencyCashInvestable(accountHoldings, instruments, bookCurrency)) {
    await postHoldingCreatedEvent(fetchApi, withBankFields({
      accountID,
      active: true,
      default: true,
      eventDateTime,
      holdingID: crypto.randomUUID(),
      holdingKind: 'CashInvestable',
      instrumentID: currencyInstrumentID,
      name: 'Capital Account',
      reason: `Create Capital Account for ${accountID}`
    }, bankFields), userID);
    createdCount += 1;
  }

  for (const required of REQUIRED_NOMINAL_HOLDINGS) {
    const holdingsForKind = accountHoldings.filter((holding) => holding.holdingKind === required.kind);
    if (!holdingsForKind.length) {
      await postHoldingCreatedEvent(fetchApi, {
        accountID,
        active: true,
        default: true,
        eventDateTime,
        holdingID: crypto.randomUUID(),
        holdingKind: required.kind,
        instrumentID: currencyInstrumentID,
        name: required.name,
        reason: `Create required ${required.kind} holding`
      }, userID);
      createdCount += 1;
    } else if (!holdingsForKind.some((holding) => holding.default)) {
      await postHoldingModifiedEvent(fetchApi, holdingModifiedRequest(holdingsForKind[0], eventDateTime, true), userID);
    }
  }

  return createdCount;
}

async function unsetCurrentDefault(
  fetchApi: typeof fetch,
  userID: string,
  eventDateTime: string,
  accountID: string,
  holdingKind: HoldingKind,
  instrumentID: string,
  holdings: Holding[],
  excludeHoldingID = ''
) {
  const currentDefault = holdings.find((holding) =>
    holding.holdingID !== excludeHoldingID &&
    holding.accountID === accountID &&
    holding.holdingKind === holdingKind &&
    defaultScopeMatches(holding, holdingKind, instrumentID) &&
    holding.default
  );

  if (!currentDefault)
    return;

  await postHoldingModifiedEvent(fetchApi, holdingModifiedRequest(currentDefault, eventDateTime, false), userID);
}

function defaultScopeMatches(holding: Holding, holdingKind: HoldingKind, instrumentID: string) {
  return NOMINAL_HOLDING_KIND_SET.has(holdingKind) || holding.instrumentID === instrumentID;
}

function holdingModifiedRequest(holding: Holding, eventDateTime: string, nextDefault: boolean): HoldingModifiedRequest {
  return withBankFields({
    default: nextDefault,
    eventDateTime,
    holdingID: holding.holdingID,
    holdingKind: holding.holdingKind,
    name: holding.name,
    reason: `${nextDefault ? 'Set' : 'Unset'} default ${holding.holdingKind} holding ${holding.name || holding.holdingID}`
  }, {
    accountName: holding.accountName ?? '',
    accountNumber: holding.accountNumber ?? '',
    bankName: holding.bankName ?? '',
    bic: holding.bic ?? '',
    iban: holding.iban ?? '',
    sortCode: holding.sortCode ?? ''
  });
}

function hasBookCurrencyCashInvestable(holdings: Holding[], instruments: Instrument[], bookCurrency: string) {
  return holdings.some((holding) => {
    if (holding.holdingKind !== 'CashInvestable')
      return false;

    if (!instruments.length)
      return true;

    return instruments.find((instrument) => instrument.instrumentID === holding.instrumentID)?.priceCurrency === bookCurrency;
  });
}

function findCurrencyInstrument(instruments: Instrument[], bookCurrency: string) {
  const matches = instruments.filter((instrument) => instrument.active && instrument.priceCurrency === bookCurrency);
  return matches.find((instrument) => instrument.exchange === 'CASH' || instrument.cfi.startsWith('M')) ?? matches[0] ?? null;
}

type BankFields = {
  accountName: string;
  accountNumber: string;
  bankName: string;
  bic: string;
  iban: string;
  sortCode: string;
};

function getBankFields(formData: FormData): BankFields {
  return {
    accountName: getFormString(formData, 'accountName'),
    accountNumber: getFormString(formData, 'accountNumber'),
    bankName: getFormString(formData, 'bankName'),
    bic: getFormString(formData, 'bic'),
    iban: getFormString(formData, 'iban'),
    sortCode: getFormString(formData, 'sortCode')
  };
}

function validateBankFields(bankFields: BankFields) {
  if (!bankFields.bankName || !bankFields.accountName || !bankFields.sortCode || !bankFields.accountNumber || !bankFields.bic || !bankFields.iban)
    return 'Bank name, account name, sort code, account number, BIC, and IBAN are required for bank holdings.';

  return '';
}

function withBankFields<TRequest extends HoldingCreatedRequest | HoldingModifiedRequest>(request: TRequest, bankFields: BankFields): TRequest {
  if (!BANK_HOLDING_KINDS.has(request.holdingKind))
    return request;

  return {
    ...request,
    accountName: bankFields.accountName,
    accountNumber: bankFields.accountNumber,
    bankName: bankFields.bankName,
    bic: bankFields.bic,
    iban: bankFields.iban,
    sortCode: bankFields.sortCode
  };
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function success(intent: string, eventID: string, message: string) {
  return {
    eventID,
    intent,
    message,
    status: 'success'
  };
}

function failure(intent: string, message: string, values: Record<string, unknown>) {
  return {
    intent,
    message,
    status: 'failure',
    values
  };
}
