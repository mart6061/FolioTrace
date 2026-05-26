import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import {
  getAccounts,
  getApiBaseUrl,
  getHoldings,
  postAccountActiveModifiedEvent,
  postAccountCreatedEvent,
  postAccountModifiedEvent,
  postTransactionSet,
  type AccountActiveModifiedRequest,
  type AccountCreatedRequest,
  type AccountModifiedRequest,
  type TransactionSetRequest
} from '$lib/server/api';

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const valuationDateTime = toApiDateTime(valuationDate);
    const asAtDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    const [accounts, holdings] = await Promise.all([
      getAccounts(fetch, valuationDateTime, asAtDateTime),
      getHoldings(fetch, valuationDateTime, asAtDateTime, false)
    ]);

    return {
      accounts,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      holdings,
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load accounts.',
      holdings: null,
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
      const [holdings, accounts] = await Promise.all([
        getHoldings(fetch, eventDateTime, null, false),
        getAccounts(fetch, eventDateTime, null)
      ]);
      const capitalHolding = holdings.items.find((holding) =>
        holding.holdingID === values.holdingID &&
        holding.accountID === values.accountID &&
        holding.holdingType === 'CashOnHand' &&
        holding.name === 'Capital' &&
        holding.default &&
        holding.active
      );

      if (!capitalHolding)
        return fail(400, cashInFailure('Select an active Capital cash holding.', values));

      const inflowHolding = holdings.items.find((holding) =>
        holding.accountID === capitalHolding.accountID &&
        holding.instrumentID === capitalHolding.instrumentID &&
        holding.holdingType === 'Nominal' &&
        holding.nominalType === 'Inflow' &&
        holding.active
      );

      if (!inflowHolding)
        return fail(400, cashInFailure('No active Inflow holding was found for the selected Capital holding.', values));

      const account = accounts.items.find((item) => item.accountID === capitalHolding.accountID);
      const amount = amountResult.amount;
      const amountLabel = `${formatAmount(amount)} ${account?.bookCurrency ?? ''}`.trim();
      const transactionSetRequest: TransactionSetRequest = {
        userID: systemUserID,
        eventDateTime,
        reason: `Cash in ${amountLabel} to ${account?.name ?? 'Capital'}`,
        credits: [
          {
            accountID: capitalHolding.accountID,
            bookCost: amount,
            holdingID: capitalHolding.holdingID,
            instrumentID: capitalHolding.instrumentID,
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

function cashInFailure(message: string, values: ReturnType<typeof readCashInForm>) {
  return { intent: 'cashIn', message, status: 'failure', values };
}

function parseCashAmount(value: string) {
  const normalizedValue = value.trim();
  const amount = Number(normalizedValue);

  if (!Number.isFinite(amount) || amount <= 0)
    return { amount: 0, message: 'Amount must be greater than zero.', valid: false as const };

  const decimalPart = normalizedValue.includes('.') ? normalizedValue.split('.')[1] : '';
  if (decimalPart.length > 8)
    return { amount: 0, message: 'Amount can have at most 8 decimal places.', valid: false as const };

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
