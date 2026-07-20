import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getFormString } from '$lib/server/forms';
import { fail } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getAccounts,
  getApiBaseUrl,
  getHoldings,
  getInstruments,
  getTransactionEvents,
  postHoldingActiveModifiedEvent,
  postHoldingCreatedEvent,
  postHoldingModifiedEvent,
  postAccountActiveModifiedEvent,
  postAccountCreatedEvent,
  postAccountIdentifierSetEvent,
  postAccountIdentifierUnsetEvent,
  postAccountModifiedEvent,
  postTransactionCancellation,
  postTransactionSet,
  type AccountActiveModifiedRequest,
  type AccountCreatedRequest,
  type AccountIdentifierSetRequest,
  type AccountModifiedRequest,
  type HoldingActiveModifiedRequest,
  type HoldingCreatedRequest,
  type HoldingModifiedRequest,
  type TransactionCancellationRequest,
  type TransactionSetRequest
} from '$lib/server/api';
import type { Holding, HoldingKind, Instrument, ProfitLossMethod } from '$lib/types';

const identifierTypes = ['Ticker', 'Sedol', 'ISIN', 'CUSIP', 'FIGI', 'RIC'] as const;

export const load: PageServerLoad = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const valuationDateTime = toApiDateTime(valuationDate);
    const asAtDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    const [accounts, holdings, instruments, transactionEvents] = await Promise.all([
      getAccounts(fetch, valuationDateTime, asAtDateTime),
      getHoldings(fetch, valuationDateTime, asAtDateTime),
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

export const actions: Actions = {
  createAccount: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readAccountForm(formData);

    if (!values.name || !values.formalName || !values.bookCurrency || !values.eventDateTime)
      return fail(400, failure('createAccount', 'Name, formal name, book currency, and event date are required.', values));

    try {
      const accountCreatedRequest: AccountCreatedRequest = {
        accountID: values.accountID,
        active: values.active,
        bookCurrency: values.bookCurrency,
        bookCostBasis: values.bookCostBasis,
        eventDateTime: toApiDateTime(values.eventDateTime),
        formalName: values.formalName,
        name: values.name,
        reason: `Create account ${values.name}`
      };

      const result = await postAccountCreatedEvent(fetch, accountCreatedRequest, currentUser.userID);
      return { accountID: values.accountID, eventID: result.eventID, intent: 'createAccount', message: `${values.name} was created successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('createAccount', error instanceof Error ? error.message : 'Unable to create account.', values));
    }
  },

  modifyAccount: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readAccountForm(formData);

    if (!values.accountID || !values.name || !values.formalName || !values.eventDateTime)
      return fail(400, failure('modifyAccount', 'Account, name, formal name, and event date are required.', values));

    try {
      const accountModifiedRequest: AccountModifiedRequest = {
        accountID: values.accountID,
        bookCostBasis: values.bookCostBasis,
        eventDateTime: toApiDateTime(values.eventDateTime),
        formalName: values.formalName,
        name: values.name,
        reason: `Modify account ${values.name}`
      };

      const result = await postAccountModifiedEvent(fetch, accountModifiedRequest, currentUser.userID);
      return { accountID: values.accountID, eventID: result.eventID, intent: 'modifyAccount', message: `${values.name} was updated successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('modifyAccount', error instanceof Error ? error.message : 'Unable to update account.', values));
    }
  },

  modifyAccountActive: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
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

      const result = await postAccountActiveModifiedEvent(fetch, accountActiveModifiedRequest, currentUser.userID);
      return { accountID, eventID: result.eventID, intent: 'modifyAccountActive', message: `${name || 'Account'} was ${active ? 'activated' : 'deactivated'} successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, { accountID, intent: 'modifyAccountActive', message: error instanceof Error ? error.message : 'Unable to update account status.', status: 'failure' });
    }
  },

  setAccountIdentifier: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const accountID = getFormString(formData, 'accountID');
    const identifierType = normaliseIdentifierType(getFormString(formData, 'identifierType'));
    const identifierValue = getFormString(formData, 'identifierValue').toUpperCase();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    if (!accountID || !identifierType || !identifierValue || !eventDateTime)
      return fail(400, { accountID, intent: 'setAccountIdentifier', message: 'Identifier type, value, and event date are required.', status: 'failure' });
    try {
      const result = await postAccountIdentifierSetEvent(fetch, { accountID, eventDateTime: toApiDateTime(eventDateTime), identifierType, identifierValue, reason: `Set ${identifierType} ${identifierValue}` }, currentUser.userID);
      return { accountID, eventID: result.eventID, intent: 'setAccountIdentifier', message: `${identifierType} was set successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, { accountID, intent: 'setAccountIdentifier', message: error instanceof Error ? error.message : 'Unable to set identifier.', status: 'failure' });
    }
  },

  unsetAccountIdentifier: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const accountID = getFormString(formData, 'accountID');
    const identifierType = normaliseIdentifierType(getFormString(formData, 'identifierType'));
    const eventDateTime = getFormString(formData, 'eventDateTime');
    if (!accountID || !identifierType || !eventDateTime)
      return fail(400, { accountID, intent: 'unsetAccountIdentifier', message: 'Identifier type and event date are required.', status: 'failure' });
    try {
      const result = await postAccountIdentifierUnsetEvent(fetch, { accountID, eventDateTime: toApiDateTime(eventDateTime), identifierType, reason: `Unset ${identifierType}` }, currentUser.userID);
      return { accountID, eventID: result.eventID, intent: 'unsetAccountIdentifier', message: `${identifierType} was removed successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, { accountID, intent: 'unsetAccountIdentifier', message: error instanceof Error ? error.message : 'Unable to remove identifier.', status: 'failure' });
    }
  },

  modifyHoldingCard: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readHoldingCardForm(formData);

    if (!values.holdingID || !values.holdingKind || !values.name || !values.eventDateTime)
      return fail(400, holdingCardFailure('Holding, kind, name, and event date are required.', values));

    const eventIDs: string[] = [];

    try {
      if (values.name !== values.originalName || values.default !== values.originalDefault) {
        const holdingModifiedRequest: HoldingModifiedRequest = {
          accountName: values.accountName,
          bankName: values.bankName,
          default: values.default,
          eventDateTime: toApiDateTime(values.eventDateTime),
          holdingID: values.holdingID,
          holdingKind: values.holdingKind,
          name: values.name,
          sortCode: values.sortCode,
          accountNumber: values.accountNumber,
          bic: values.bic,
          iban: values.iban,
          reason: `Modify holding ${values.name}`
        };

        const result = await postHoldingModifiedEvent(fetch, holdingModifiedRequest, currentUser.userID);
        eventIDs.push(result.eventID);
      }

      if (values.active !== values.originalActive) {
        const holdingActiveModifiedRequest: HoldingActiveModifiedRequest = {
          active: values.active,
          eventDateTime: toApiDateTime(values.eventDateTime),
          holdingID: values.holdingID,
          reason: `${values.active ? 'Activate' : 'Deactivate'} holding ${values.name || values.holdingID}`
        };

        const result = await postHoldingActiveModifiedEvent(fetch, holdingActiveModifiedRequest, currentUser.userID);
        eventIDs.push(result.eventID);
      }

      return {
        eventID: eventIDs.at(-1) ?? '',
        eventIDs,
        holdingID: values.holdingID,
        intent: 'modifyHoldingCard',
        message: eventIDs.length ? `${values.name} was updated successfully.` : `${values.name} was unchanged.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, holdingCardFailure(error instanceof Error ? error.message : 'Unable to update holding.', values));
    }
  },

  cashIn: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
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
        holding.holdingKind === 'NominalInflow' &&
        holding.active
      );

      if (!inflowHolding)
        return fail(400, cashInFailure('No active Inflow holding was found for the selected Investable holding.', values));

      const account = accounts.items.find((item) => item.accountID === investableHolding.accountID);
      const amount = amountResult.amount;
      const amountLabel = `${formatAmount(amount)} ${account?.bookCurrency ?? ''}`.trim();
      const transactionSetRequest: TransactionSetRequest = {
        userID: currentUser.userID,
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

  cashOut: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
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
        holding.holdingKind === 'NominalOutflow' &&
        holding.active
      );

      if (!outflowHolding)
        return fail(400, cashOutFailure('No active Outflow holding was found for the selected Investable holding.', values));

      const account = accounts.items.find((item) => item.accountID === investableHolding.accountID);
      const amount = amountResult.amount;
      const amountLabel = `${formatAmount(amount)} ${account?.bookCurrency ?? ''}`.trim();
      const transactionSetRequest: TransactionSetRequest = {
        userID: currentUser.userID,
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

  feesIn: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readFeeForm(formData);

    if (!values.accountID || !values.cashHoldingID || !values.feeHoldingID || !values.eventDateTime || !values.amount)
      return fail(400, feeInFailure('Account, cash holding, fee holding, event date, and amount are required.', values));

    const amountResult = parseCashAmount(values.amount);
    if (!amountResult.valid)
      return fail(400, feeInFailure(amountResult.message, values));

    try {
      const eventDateTime = toApiDateTime(values.eventDateTime);
      const settlementDateTime = toApiDateTime(nextWorkingDayForInput(values.eventDateTime));
      const [holdings, accounts] = await Promise.all([
        getHoldings(fetch, eventDateTime, null, false),
        getAccounts(fetch, eventDateTime, null)
      ]);
      const cashHolding = holdings.items.find((holding) =>
        holding.holdingID === values.cashHoldingID &&
        holding.accountID === values.accountID &&
        isFeeCashHoldingKind(holding.holdingKind) &&
        holding.active
      );

      if (!cashHolding)
        return fail(400, feeInFailure('Select an active Investable or Non-investable cash holding.', values));

      const feeHolding = holdings.items.find((holding) =>
        holding.holdingID === values.feeHoldingID &&
        holding.accountID === values.accountID &&
        isFeeHoldingKind(holding.holdingKind) &&
        holding.active
      );

      if (!feeHolding)
        return fail(400, feeInFailure('Select an active fee holding.', values));

      const account = accounts.items.find((item) => item.accountID === values.accountID);
      const amount = amountResult.amount;
      const amountLabel = `${formatAmount(amount)} ${account?.bookCurrency ?? ''}`.trim();
      const transactionSetRequest: TransactionSetRequest = {
        userID: currentUser.userID,
        eventDateTime,
        settlementDateTime,
        reason: `Fees in ${amountLabel} from ${feeHolding.name || feeHolding.holdingKind} to ${cashHolding.name || cashHolding.holdingKind}`,
        credits: [
          {
            accountID: cashHolding.accountID,
            bookCost: amount,
            holdingID: cashHolding.holdingID,
            instrumentID: cashHolding.instrumentID,
            quantity: amount
          }
        ],
        debits: [
          {
            accountID: feeHolding.accountID,
            bookCost: amount,
            holdingID: feeHolding.holdingID,
            instrumentID: feeHolding.instrumentID,
            quantity: amount
          }
        ]
      };

      const result = await postTransactionSet(fetch, transactionSetRequest);
      return {
        eventIDs: result.eventIDs,
        intent: 'feesIn',
        message: `Fees in ${amountLabel} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, feeInFailure(error instanceof Error ? error.message : 'Unable to create fees-in transaction.', values));
    }
  },

  feesOut: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readFeeForm(formData);

    if (!values.accountID || !values.cashHoldingID || !values.feeHoldingID || !values.eventDateTime || !values.amount)
      return fail(400, feeOutFailure('Account, cash holding, fee holding, event date, and amount are required.', values));

    const amountResult = parseCashAmount(values.amount);
    if (!amountResult.valid)
      return fail(400, feeOutFailure(amountResult.message, values));

    try {
      const eventDateTime = toApiDateTime(values.eventDateTime);
      const settlementDateTime = toApiDateTime(nextWorkingDayForInput(values.eventDateTime));
      const [holdings, accounts] = await Promise.all([
        getHoldings(fetch, eventDateTime, null, false),
        getAccounts(fetch, eventDateTime, null)
      ]);
      const cashHolding = holdings.items.find((holding) =>
        holding.holdingID === values.cashHoldingID &&
        holding.accountID === values.accountID &&
        isFeeCashHoldingKind(holding.holdingKind) &&
        holding.active
      );

      if (!cashHolding)
        return fail(400, feeOutFailure('Select an active Investable or Non-investable cash holding.', values));

      const feeHolding = holdings.items.find((holding) =>
        holding.holdingID === values.feeHoldingID &&
        holding.accountID === values.accountID &&
        isFeeHoldingKind(holding.holdingKind) &&
        holding.active
      );

      if (!feeHolding)
        return fail(400, feeOutFailure('Select an active fee holding.', values));

      const account = accounts.items.find((item) => item.accountID === values.accountID);
      const amount = amountResult.amount;
      const amountLabel = `${formatAmount(amount)} ${account?.bookCurrency ?? ''}`.trim();
      const transactionSetRequest: TransactionSetRequest = {
        userID: currentUser.userID,
        eventDateTime,
        settlementDateTime,
        reason: `Fees out ${amountLabel} from ${cashHolding.name || cashHolding.holdingKind} to ${feeHolding.name || feeHolding.holdingKind}`,
        credits: [
          {
            accountID: feeHolding.accountID,
            bookCost: amount,
            holdingID: feeHolding.holdingID,
            instrumentID: feeHolding.instrumentID,
            quantity: amount
          }
        ],
        debits: [
          {
            accountID: cashHolding.accountID,
            bookCost: amount,
            holdingID: cashHolding.holdingID,
            instrumentID: cashHolding.instrumentID,
            quantity: amount
          }
        ]
      };

      const result = await postTransactionSet(fetch, transactionSetRequest);
      return {
        eventIDs: result.eventIDs,
        intent: 'feesOut',
        message: `Fees out ${amountLabel} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, feeOutFailure(error instanceof Error ? error.message : 'Unable to create fees-out transaction.', values));
    }
  },

  inSpecieIn: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readInSpecieForm(formData);

    if (!values.accountID || !values.instrumentID || !values.eventDateTime || !values.quantity)
      return fail(400, inSpecieInFailure('Account, instrument, event date, and quantity are required.', values));

    const quantityResult = parsePositiveDecimal(values.quantity, 'Quantity');
    if (!quantityResult.valid)
      return fail(400, inSpecieInFailure(quantityResult.message, values));

    const bookCostResult = parseBookCost(values.bookCost);
    if (!bookCostResult.valid)
      return fail(400, inSpecieInFailure(bookCostResult.message, values));

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
        return fail(400, inSpecieInFailure('Select a valid account.', values));
      if (!instrument)
        return fail(400, inSpecieInFailure('Select a valid instrument.', values));

      const positionHolding = await ensureMovementHolding(fetch, holdings.items, eventDateTime, values.accountID, instrument, 'PositionAsset', instrument.name || 'Asset', currentUser.userID);
      const inSpecieHolding = await ensureMovementHolding(fetch, holdings.items, eventDateTime, values.accountID, instrument, 'NominalInSpecieIn', 'InSpecie In', currentUser.userID);
      const quantity = quantityResult.amount;
      const bookCost = bookCostResult.amount;
      const transactionSetRequest: TransactionSetRequest = {
        userID: currentUser.userID,
        eventDateTime,
        settlementDateTime,
        reason: `InSpecie in ${formatAmount(quantity)} ${instrument.name} to ${account.name}`,
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
            accountID: inSpecieHolding.accountID,
            bookCost,
            holdingID: inSpecieHolding.holdingID,
            instrumentID: inSpecieHolding.instrumentID,
            quantity
          }
        ]
      };

      const result = await postTransactionSet(fetch, transactionSetRequest);
      return {
        eventIDs: result.eventIDs,
        intent: 'inSpecieIn',
        message: `InSpecie in ${formatAmount(quantity)} ${instrument.name} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, inSpecieInFailure(error instanceof Error ? error.message : 'Unable to create InSpecie-in transaction.', values));
    }
  },

  inSpecieOut: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readInSpecieForm(formData);

    if (!values.accountID || !values.instrumentID || !values.eventDateTime || !values.quantity)
      return fail(400, inSpecieOutFailure('Account, instrument, event date, and quantity are required.', values));

    const quantityResult = parsePositiveDecimal(values.quantity, 'Quantity');
    if (!quantityResult.valid)
      return fail(400, inSpecieOutFailure(quantityResult.message, values));

    const bookCostResult = parseBookCost(values.bookCost);
    if (!bookCostResult.valid)
      return fail(400, inSpecieOutFailure(bookCostResult.message, values));

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
        return fail(400, inSpecieOutFailure('Select a valid account.', values));
      if (!instrument)
        return fail(400, inSpecieOutFailure('Select a valid instrument.', values));

      const positionHolding = await ensureMovementHolding(fetch, holdings.items, eventDateTime, values.accountID, instrument, 'PositionAsset', instrument.name || 'Asset', currentUser.userID);
      const inSpecieHolding = await ensureMovementHolding(fetch, holdings.items, eventDateTime, values.accountID, instrument, 'NominalInSpecieOut', 'InSpecie Out', currentUser.userID);
      const quantity = quantityResult.amount;
      const bookCost = bookCostResult.amount;
      const transactionSetRequest: TransactionSetRequest = {
        userID: currentUser.userID,
        eventDateTime,
        settlementDateTime,
        reason: `InSpecie out ${formatAmount(quantity)} ${instrument.name} from ${account.name}`,
        credits: [
          {
            accountID: inSpecieHolding.accountID,
            bookCost,
            holdingID: inSpecieHolding.holdingID,
            instrumentID: inSpecieHolding.instrumentID,
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
        intent: 'inSpecieOut',
        message: `InSpecie out ${formatAmount(quantity)} ${instrument.name} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, inSpecieOutFailure(error instanceof Error ? error.message : 'Unable to create InSpecie-out transaction.', values));
    }
  },

  cancelTransactionSet: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const eventSetID = getFormString(formData, 'eventSetID');
    const accountID = getFormString(formData, 'accountID');

    if (!eventSetID)
      return fail(400, { accountID, eventSetID, intent: 'cancelTransactionSet', message: 'Transaction set is required.', status: 'failure' });

    try {
      const cancellationRequest: TransactionCancellationRequest = {
        userID: currentUser.userID,
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

function normaliseIdentifierType(value: string): AccountIdentifierSetRequest['identifierType'] | '' {
  return identifierTypes.find((type) => type.toLocaleLowerCase() === value.toLocaleLowerCase()) ?? '';
}

function readAccountForm(formData: FormData) {
  return {
    accountID: getFormString(formData, 'accountID'),
    active: getFormString(formData, 'active') !== 'false',
    bookCostBasis: normaliseProfitLossMethod(getFormString(formData, 'bookCostBasis')),
    bookCurrency: getFormString(formData, 'bookCurrency').toUpperCase(),
    eventDateTime: getFormString(formData, 'eventDateTime'),
    formalName: getFormString(formData, 'formalName'),
    name: getFormString(formData, 'name')
  };
}

function normaliseProfitLossMethod(value: string): ProfitLossMethod {
  return value === 'LIFO' || value === 'RunningAverage' ? value : 'FIFO';
}

function failure(intent: string, message: string, values: ReturnType<typeof readAccountForm>) {
  return { accountID: values.accountID, intent, message, status: 'failure', values };
}

function readHoldingCardForm(formData: FormData) {
  return {
    accountName: getFormString(formData, 'accountName'),
    accountNumber: getFormString(formData, 'accountNumber'),
    active: getFormString(formData, 'active') === 'true',
    bankName: getFormString(formData, 'bankName'),
    bic: getFormString(formData, 'bic'),
    default: getFormString(formData, 'default') === 'true',
    eventDateTime: getFormString(formData, 'eventDateTime'),
    holdingID: getFormString(formData, 'holdingID'),
    holdingKind: getFormString(formData, 'holdingKind') as HoldingKind,
    iban: getFormString(formData, 'iban'),
    name: getFormString(formData, 'name'),
    originalActive: getFormString(formData, 'originalActive') === 'true',
    originalDefault: getFormString(formData, 'originalDefault') === 'true',
    originalName: getFormString(formData, 'originalName'),
    sortCode: getFormString(formData, 'sortCode')
  };
}

function holdingCardFailure(message: string, values: ReturnType<typeof readHoldingCardForm>) {
  return { holdingID: values.holdingID, intent: 'modifyHoldingCard', message, status: 'failure', values };
}

function readCashInForm(formData: FormData) {
  return {
    accountID: getFormString(formData, 'accountID'),
    amount: getFormString(formData, 'amount'),
    eventDateTime: getFormString(formData, 'eventDateTime'),
    holdingID: getFormString(formData, 'holdingID')
  };
}

function readFeeForm(formData: FormData) {
  return {
    accountID: getFormString(formData, 'accountID'),
    amount: getFormString(formData, 'amount'),
    cashHoldingID: getFormString(formData, 'cashHoldingID'),
    eventDateTime: getFormString(formData, 'eventDateTime'),
    feeHoldingID: getFormString(formData, 'feeHoldingID')
  };
}

function readInSpecieForm(formData: FormData) {
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

function feeInFailure(message: string, values: ReturnType<typeof readFeeForm>) {
  return { intent: 'feesIn', message, status: 'failure', values };
}

function feeOutFailure(message: string, values: ReturnType<typeof readFeeForm>) {
  return { intent: 'feesOut', message, status: 'failure', values };
}

function inSpecieInFailure(message: string, values: ReturnType<typeof readInSpecieForm>) {
  return { intent: 'inSpecieIn', message, status: 'failure', values };
}

function inSpecieOutFailure(message: string, values: ReturnType<typeof readInSpecieForm>) {
  return { intent: 'inSpecieOut', message, status: 'failure', values };
}

function parseCashAmount(value: string) {
  return parsePositiveDecimal(value, 'Amount');
}

function isFeeCashHoldingKind(holdingKind: HoldingKind) {
  return holdingKind === 'CashInvestable' || holdingKind === 'CashNonInvestable';
}

function isFeeHoldingKind(holdingKind: HoldingKind) {
  return holdingKind === 'NominalFeesCustodian' || holdingKind === 'NominalFeesAdministrator' || holdingKind === 'NominalFeesBank';
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
  name: string,
  userID: string
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

  await postHoldingCreatedEvent(fetch, request, userID);

  const created: Holding = {
    accountID,
    active: true,
    asOfDateTime: eventDateTime,
    default: false,
    holdingID,
    holdingKind,
    includeInValuation: holdingKind === 'PositionMemo' || holdingKind === 'PositionCash' || holdingKind === 'PositionAsset',
    instrumentID: instrument.instrumentID,
    lastAuditDateTime: eventDateTime,
    lastEventID: '',
    name,
    valuationDateTime: eventDateTime
  };

  holdings.push(created);
  return created;
}
