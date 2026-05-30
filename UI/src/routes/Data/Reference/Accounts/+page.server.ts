import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import {
  getAccounts,
  getApiBaseUrl,
  getHoldings,
  getInstruments,
  getTransactionEvents,
  postHoldingCreatedEvent,
  postAccountActiveModifiedEvent,
  postAccountCreatedEvent,
  postAccountModifiedEvent,
  postTransactionCancellation,
  postTransactionSet,
  type AccountActiveModifiedRequest,
  type AccountCreatedRequest,
  type AccountModifiedRequest,
  type HoldingCreatedRequest,
  type TransactionCancellationRequest,
  type TransactionSetRequest
} from '$lib/server/api';
import type { Holding, HoldingKind, Instrument } from '$lib/types';

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const valuationDateTime = toApiDateTime(valuationDate);
    const asAtDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    const [accounts, holdings, instruments, transactionEvents] = await Promise.all([
      getAccounts(fetch, valuationDateTime, asAtDateTime),
      getHoldings(fetch, valuationDateTime, asAtDateTime, false),
      getInstruments(fetch, valuationDateTime, asAtDateTime),
      getTransactionEvents(fetch)
    ]);

    return {
      accounts,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      holdings,
      instruments,
      transactionEvents,
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load accounts.',
      holdings: null,
      instruments: null,
      transactionEvents: null,
      valuationDate
    };
  }
};

export const actions = {
  createAccount: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readAccountForm(formData);

    if (!values.name || !values.formalName || !values.bookCurrency || !values.eventDateTime)
      return fail(400, failure('createAccount', 'Name, formal name, book currency, and event date are required.', values));

    try {
      const accountCreatedRequest: AccountCreatedRequest = {
        accountID: values.accountID,
        active: values.active,
        bookCurrency: values.bookCurrency,
        eventDateTime: toApiDateTime(values.eventDateTime),
        formalName: values.formalName,
        name: values.name,
        reason: `Create account ${values.name}`
      };

      const result = await postAccountCreatedEvent(fetch, accountCreatedRequest, systemUserID);
      return { accountID: values.accountID, eventID: result.eventID, intent: 'createAccount', message: `${values.name} was created successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('createAccount', error instanceof Error ? error.message : 'Unable to create account.', values));
    }
  },

  modifyAccount: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readAccountForm(formData);

    if (!values.accountID || !values.name || !values.formalName || !values.eventDateTime)
      return fail(400, failure('modifyAccount', 'Account, name, formal name, and event date are required.', values));

    try {
      const accountModifiedRequest: AccountModifiedRequest = {
        accountID: values.accountID,
        eventDateTime: toApiDateTime(values.eventDateTime),
        formalName: values.formalName,
        name: values.name,
        reason: `Modify account ${values.name}`
      };

      const result = await postAccountModifiedEvent(fetch, accountModifiedRequest, systemUserID);
      return { accountID: values.accountID, eventID: result.eventID, intent: 'modifyAccount', message: `${values.name} was updated successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('modifyAccount', error instanceof Error ? error.message : 'Unable to update account.', values));
    }
  },

  modifyAccountActive: async ({ fetch, request }) => {
    const formData = await request.formData();
    const accountID = getFormString(formData, 'accountID');
    const name = getFormString(formData, 'name');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const active = getFormString(formData, 'active') === 'true';

    if (!accountID || !eventDateTime)
      return fail(400, { accountID, intent: 'modifyAccountActive', message: 'Account and event date are required.', status: 'failure' });

    try {
      const accountActiveModifiedRequest: AccountActiveModifiedRequest = {
        accountID,
        active,
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `${active ? 'Activate' : 'Deactivate'} account ${name || accountID}`
      };

      const result = await postAccountActiveModifiedEvent(fetch, accountActiveModifiedRequest, systemUserID);
      return { accountID, eventID: result.eventID, intent: 'modifyAccountActive', message: `${name || 'Account'} was ${active ? 'activated' : 'deactivated'} successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, { accountID, intent: 'modifyAccountActive', message: error instanceof Error ? error.message : 'Unable to update account status.', status: 'failure' });
    }
  },

  cashIn: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readCashInForm(formData);

    if (!values.accountID || !values.holdingID || !values.eventDateTime || !values.amount)
      return fail(400, cashInFailure('Account, holding, event date, and amount are required.', values));

    const amountResult = parseCashAmount(values.amount);
    if (!amountResult.valid)
      return fail(400, cashInFailure(amountResult.message, values));

    try {
      const eventDateTime = toApiDateTime(values.eventDateTime);
      const settlementDateTime = toApiDateTime(nextWorkingDayForInput(values.eventDateTime));
      const [holdings, accounts] = await Promise.all([
        getHoldings(fetch, eventDateTime, null, false),
        getAccounts(fetch, eventDateTime, null)
      ]);
      const investableHolding = holdings.items.find((holding) =>
        holding.holdingID === values.holdingID &&
        holding.accountID === values.accountID &&
        holding.holdingKind === 'CashInvestable' &&
        holding.active
      );

      if (!investableHolding)
        return fail(400, cashInFailure('Select an active Investable cash holding.', values));

      const inflowHolding = holdings.items.find((holding) =>
        holding.accountID === investableHolding.accountID &&
        holding.instrumentID === investableHolding.instrumentID &&
        holding.holdingKind === 'Inflow' &&
        holding.active
      );

      if (!inflowHolding)
        return fail(400, cashInFailure('No active Inflow holding was found for the selected Investable holding.', values));

      const account = accounts.items.find((item) => item.accountID === investableHolding.accountID);
      const amount = amountResult.amount;
      const amountLabel = `${formatAmount(amount)} ${account?.bookCurrency ?? ''}`.trim();
      const transactionSetRequest: TransactionSetRequest = {
        userID: systemUserID,
        eventDateTime,
        settlementDateTime,
        reason: `Cash in ${amountLabel} to ${account?.name ?? 'Investable'}`,
        credits: [
          {
            accountID: investableHolding.accountID,
            bookCost: amount,
            holdingID: investableHolding.holdingID,
            instrumentID: investableHolding.instrumentID,
            quantity: amount
          }
        ],
        debits: [
          {
            accountID: inflowHolding.accountID,
            bookCost: amount,
            holdingID: inflowHolding.holdingID,
            instrumentID: inflowHolding.instrumentID,
            quantity: amount
          }
        ]
      };

      const result = await postTransactionSet(fetch, transactionSetRequest);
      return {
        eventIDs: result.eventIDs,
        intent: 'cashIn',
        message: `Cash in ${amountLabel} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, cashInFailure(error instanceof Error ? error.message : 'Unable to create cash-in transaction.', values));
    }
  },

  cashOut: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readCashInForm(formData);

    if (!values.accountID || !values.holdingID || !values.eventDateTime || !values.amount)
      return fail(400, cashOutFailure('Account, holding, event date, and amount are required.', values));

    const amountResult = parseCashAmount(values.amount);
    if (!amountResult.valid)
      return fail(400, cashOutFailure(amountResult.message, values));

    try {
      const eventDateTime = toApiDateTime(values.eventDateTime);
      const settlementDateTime = toApiDateTime(nextWorkingDayForInput(values.eventDateTime));
      const [holdings, accounts] = await Promise.all([
        getHoldings(fetch, eventDateTime, null, false),
        getAccounts(fetch, eventDateTime, null)
      ]);
      const investableHolding = holdings.items.find((holding) =>
        holding.holdingID === values.holdingID &&
        holding.accountID === values.accountID &&
        holding.holdingKind === 'CashInvestable' &&
        holding.active
      );

      if (!investableHolding)
        return fail(400, cashOutFailure('Select an active Investable cash holding.', values));

      const outflowHolding = holdings.items.find((holding) =>
        holding.accountID === investableHolding.accountID &&
        holding.instrumentID === investableHolding.instrumentID &&
        holding.holdingKind === 'Outflow' &&
        holding.active
      );

      if (!outflowHolding)
        return fail(400, cashOutFailure('No active Outflow holding was found for the selected Investable holding.', values));

      const account = accounts.items.find((item) => item.accountID === investableHolding.accountID);
      const amount = amountResult.amount;
      const amountLabel = `${formatAmount(amount)} ${account?.bookCurrency ?? ''}`.trim();
      const transactionSetRequest: TransactionSetRequest = {
        userID: systemUserID,
        eventDateTime,
        settlementDateTime,
        reason: `Cash out ${amountLabel} from ${account?.name ?? 'Investable'}`,
        credits: [
          {
            accountID: outflowHolding.accountID,
            bookCost: amount,
            holdingID: outflowHolding.holdingID,
            instrumentID: outflowHolding.instrumentID,
            quantity: amount
          }
        ],
        debits: [
          {
            accountID: investableHolding.accountID,
            bookCost: amount,
            holdingID: investableHolding.holdingID,
            instrumentID: investableHolding.instrumentID,
            quantity: amount
          }
        ]
      };

      const result = await postTransactionSet(fetch, transactionSetRequest);
      return {
        eventIDs: result.eventIDs,
        intent: 'cashOut',
        message: `Cash out ${amountLabel} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, cashOutFailure(error instanceof Error ? error.message : 'Unable to create cash-out transaction.', values));
    }
  },

  inspecieIn: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readInspecieForm(formData);

    if (!values.accountID || !values.instrumentID || !values.eventDateTime || !values.quantity)
      return fail(400, inspecieInFailure('Account, instrument, event date, and quantity are required.', values));

    const quantityResult = parsePositiveDecimal(values.quantity, 'Quantity');
    if (!quantityResult.valid)
      return fail(400, inspecieInFailure(quantityResult.message, values));

    const bookCostResult = parseBookCost(values.bookCost);
    if (!bookCostResult.valid)
      return fail(400, inspecieInFailure(bookCostResult.message, values));

    try {
      const eventDateTime = toApiDateTime(values.eventDateTime);
      const settlementDateTime = toApiDateTime(nextWorkingDayForInput(values.eventDateTime));
      const [holdings, accounts, instruments] = await Promise.all([
        getHoldings(fetch, eventDateTime, null, false),
        getAccounts(fetch, eventDateTime, null),
        getInstruments(fetch, eventDateTime, null)
      ]);
      const account = accounts.items.find((item) => item.accountID === values.accountID);
      const instrument = resolveInstrument(instruments.items, values.instrumentID);

      if (!account)
        return fail(400, inspecieInFailure('Select a valid account.', values));
      if (!instrument)
        return fail(400, inspecieInFailure('Select a valid instrument.', values));

      const positionHolding = await ensureMovementHolding(fetch, holdings.items, eventDateTime, values.accountID, instrument, 'PositionMemo', instrument.name || 'Position');
      const inspecieHolding = await ensureMovementHolding(fetch, holdings.items, eventDateTime, values.accountID, instrument, 'InspecieIn', 'Inspecie In');
      const quantity = quantityResult.amount;
      const bookCost = bookCostResult.amount;
      const transactionSetRequest: TransactionSetRequest = {
        userID: systemUserID,
        eventDateTime,
        settlementDateTime,
        reason: `Inspecie in ${formatAmount(quantity)} ${instrument.name} to ${account.name}`,
        credits: [
          {
            accountID: positionHolding.accountID,
            bookCost,
            holdingID: positionHolding.holdingID,
            instrumentID: positionHolding.instrumentID,
            quantity
          }
        ],
        debits: [
          {
            accountID: inspecieHolding.accountID,
            bookCost,
            holdingID: inspecieHolding.holdingID,
            instrumentID: inspecieHolding.instrumentID,
            quantity
          }
        ]
      };

      const result = await postTransactionSet(fetch, transactionSetRequest);
      return {
        eventIDs: result.eventIDs,
        intent: 'inspecieIn',
        message: `Inspecie in ${formatAmount(quantity)} ${instrument.name} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, inspecieInFailure(error instanceof Error ? error.message : 'Unable to create inspecie-in transaction.', values));
    }
  },

  inspecieOut: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readInspecieForm(formData);

    if (!values.accountID || !values.instrumentID || !values.eventDateTime || !values.quantity)
      return fail(400, inspecieOutFailure('Account, instrument, event date, and quantity are required.', values));

    const quantityResult = parsePositiveDecimal(values.quantity, 'Quantity');
    if (!quantityResult.valid)
      return fail(400, inspecieOutFailure(quantityResult.message, values));

    const bookCostResult = parseBookCost(values.bookCost);
    if (!bookCostResult.valid)
      return fail(400, inspecieOutFailure(bookCostResult.message, values));

    try {
      const eventDateTime = toApiDateTime(values.eventDateTime);
      const settlementDateTime = toApiDateTime(nextWorkingDayForInput(values.eventDateTime));
      const [holdings, accounts, instruments] = await Promise.all([
        getHoldings(fetch, eventDateTime, null, false),
        getAccounts(fetch, eventDateTime, null),
        getInstruments(fetch, eventDateTime, null)
      ]);
      const account = accounts.items.find((item) => item.accountID === values.accountID);
      const instrument = resolveInstrument(instruments.items, values.instrumentID);

      if (!account)
        return fail(400, inspecieOutFailure('Select a valid account.', values));
      if (!instrument)
        return fail(400, inspecieOutFailure('Select a valid instrument.', values));

      const positionHolding = await ensureMovementHolding(fetch, holdings.items, eventDateTime, values.accountID, instrument, 'PositionMemo', instrument.name || 'Position');
      const inspecieHolding = await ensureMovementHolding(fetch, holdings.items, eventDateTime, values.accountID, instrument, 'InspecieOut', 'Inspecie Out');
      const quantity = quantityResult.amount;
      const bookCost = bookCostResult.amount;
      const transactionSetRequest: TransactionSetRequest = {
        userID: systemUserID,
        eventDateTime,
        settlementDateTime,
        reason: `Inspecie out ${formatAmount(quantity)} ${instrument.name} from ${account.name}`,
        credits: [
          {
            accountID: inspecieHolding.accountID,
            bookCost,
            holdingID: inspecieHolding.holdingID,
            instrumentID: inspecieHolding.instrumentID,
            quantity
          }
        ],
        debits: [
          {
            accountID: positionHolding.accountID,
            bookCost,
            holdingID: positionHolding.holdingID,
            instrumentID: positionHolding.instrumentID,
            quantity
          }
        ]
      };

      const result = await postTransactionSet(fetch, transactionSetRequest);
      return {
        eventIDs: result.eventIDs,
        intent: 'inspecieOut',
        message: `Inspecie out ${formatAmount(quantity)} ${instrument.name} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, inspecieOutFailure(error instanceof Error ? error.message : 'Unable to create inspecie-out transaction.', values));
    }
  },

  cancelTransactionSet: async ({ fetch, request }) => {
    const formData = await request.formData();
    const eventSetID = getFormString(formData, 'eventSetID');
    const accountID = getFormString(formData, 'accountID');

    if (!eventSetID)
      return fail(400, { accountID, eventSetID, intent: 'cancelTransactionSet', message: 'Transaction set is required.', status: 'failure' });

    try {
      const cancellationRequest: TransactionCancellationRequest = {
        userID: systemUserID,
        eventSetID,
        reason: `Cancel transaction set ${eventSetID}`
      };

      const result = await postTransactionCancellation(fetch, cancellationRequest);
      return {
        accountID,
        eventIDs: result.eventIDs,
        eventSetID,
        intent: 'cancelTransactionSet',
        message: 'Transaction set was cancelled successfully.',
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        accountID,
        eventSetID,
        intent: 'cancelTransactionSet',
        message: error instanceof Error ? error.message : 'Unable to cancel transaction set.',
        status: 'failure'
      });
    }
  }
};

function readAccountForm(formData: FormData) {
  return {
    accountID: getFormString(formData, 'accountID'),
    active: getFormString(formData, 'active') !== 'false',
    bookCurrency: getFormString(formData, 'bookCurrency').toUpperCase(),
    eventDateTime: getFormString(formData, 'eventDateTime'),
    formalName: getFormString(formData, 'formalName'),
    name: getFormString(formData, 'name')
  };
}

function failure(intent: string, message: string, values: ReturnType<typeof readAccountForm>) {
  return { accountID: values.accountID, intent, message, status: 'failure', values };
}

function readCashInForm(formData: FormData) {
  return {
    accountID: getFormString(formData, 'accountID'),
    amount: getFormString(formData, 'amount'),
    eventDateTime: getFormString(formData, 'eventDateTime'),
    holdingID: getFormString(formData, 'holdingID')
  };
}

function readInspecieForm(formData: FormData) {
  return {
    accountID: getFormString(formData, 'accountID'),
    bookCost: getFormString(formData, 'bookCost'),
    eventDateTime: getFormString(formData, 'eventDateTime'),
    instrumentID: getFormString(formData, 'instrumentID'),
    quantity: getFormString(formData, 'quantity')
  };
}

function cashInFailure(message: string, values: ReturnType<typeof readCashInForm>) {
  return { intent: 'cashIn', message, status: 'failure', values };
}

function cashOutFailure(message: string, values: ReturnType<typeof readCashInForm>) {
  return { intent: 'cashOut', message, status: 'failure', values };
}

function inspecieInFailure(message: string, values: ReturnType<typeof readInspecieForm>) {
  return { intent: 'inspecieIn', message, status: 'failure', values };
}

function inspecieOutFailure(message: string, values: ReturnType<typeof readInspecieForm>) {
  return { intent: 'inspecieOut', message, status: 'failure', values };
}

function parseCashAmount(value: string) {
  return parsePositiveDecimal(value, 'Amount');
}

function parsePositiveDecimal(value: string, label: string) {
  const result = parseDecimal(value, label);

  if (!result.valid)
    return result;

  if (result.amount <= 0)
    return { amount: 0, message: `${label} must be greater than zero.`, valid: false as const };

  return result;
}

function parseBookCost(value: string) {
  const result = parseDecimal(value || '0', 'Book cost');

  if (!result.valid)
    return result;

  if (result.amount < 0)
    return { amount: 0, message: 'Book cost must be zero or greater.', valid: false as const };

  return result;
}

function parseDecimal(value: string, label: string) {
  const normalizedValue = value.trim();
  const amount = Number(normalizedValue);

  if (!Number.isFinite(amount))
    return { amount: 0, message: `${label} must be a number.`, valid: false as const };

  const decimalPart = normalizedValue.includes('.') ? normalizedValue.split('.')[1] : '';
  if (decimalPart.length > 8)
    return { amount: 0, message: `${label} can have at most 8 decimal places.`, valid: false as const };

  return { amount, message: '', valid: true as const };
}

function formatAmount(value: number) {
  return value.toLocaleString(undefined, {
    maximumFractionDigits: 8,
    minimumFractionDigits: 0
  });
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function nextWorkingDayForInput(value: string) {
  const date = new Date(value);
  date.setDate(date.getDate() + 1);

  while (date.getDay() === 0 || date.getDay() === 6)
    date.setDate(date.getDate() + 1);

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

function pad(value: number) {
  return value.toString().padStart(2, '0');
}

function resolveInstrument(instruments: Instrument[], value: string) {
  return instruments.find((instrument) =>
    instrument.instrumentID === value ||
    instrumentOptionLabel(instrument) === value ||
    instrument.name === value
  );
}

function instrumentOptionLabel(instrument: Instrument) {
  const ticker = instrument.identifiers.find((identifier) => identifier.type === 'Ticker' || identifier.type === 0)?.value ?? '';
  return [ticker, instrument.name, instrument.exchange].filter(Boolean).join(' - ');
}

async function ensureMovementHolding(
  fetch: typeof globalThis.fetch,
  holdings: Holding[],
  eventDateTime: string,
  accountID: string,
  instrument: Instrument,
  holdingKind: HoldingKind,
  name: string
) {
  const existing = holdings.find((holding) =>
    holding.accountID === accountID &&
    holding.instrumentID === instrument.instrumentID &&
    holding.holdingKind === holdingKind &&
    holding.active
  );

  if (existing)
    return existing;

  const holdingID = crypto.randomUUID();
  const request: HoldingCreatedRequest = {
    accountID,
    active: true,
    default: false,
    eventDateTime,
    holdingID,
    holdingKind,
    instrumentID: instrument.instrumentID,
    name,
    reason: `Create ${name} holding for ${instrument.name}`
  };

  await postHoldingCreatedEvent(fetch, request, systemUserID);

  const created: Holding = {
    accountID,
    active: true,
    asOfDateTime: eventDateTime,
    default: false,
    holdingID,
    holdingKind,
    includeInValuation: holdingKind === 'PositionMemo' || holdingKind === 'PositionCash',
    instrumentID: instrument.instrumentID,
    lastAuditDateTime: eventDateTime,
    lastEventID: '',
    name,
    valuationDateTime: eventDateTime
  };

  holdings.push(created);
  return created;
}
