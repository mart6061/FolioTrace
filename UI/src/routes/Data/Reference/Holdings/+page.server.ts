import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getAccounts,
  getApiBaseUrl,
  getHoldings,
  getInstruments,
  postHoldingActiveModifiedEvent,
  postHoldingCreatedEvent,
  postHoldingModifiedEvent,
  type HoldingActiveModifiedRequest,
  type HoldingCreatedRequest,
  type HoldingModifiedRequest
} from '$lib/server/api';
import type { HoldingKind } from '$lib/types';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiValuationDate = toApiDateTime(valuationDate);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const [holdings, accounts, instruments] = await Promise.all([
      getHoldings(fetch, apiValuationDate, apiAuditDateTime),
      getAccounts(fetch, apiValuationDate, apiAuditDateTime),
      getInstruments(fetch, apiValuationDate, apiAuditDateTime)
    ]);

    return {
      accounts,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      holdings,
      instruments,
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load holdings.',
      holdings: null,
      instruments: null,
      valuationDate
    };
  }
};

export const actions = {
  createHolding: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readHoldingForm(formData);

    if (!values.accountID || !values.instrumentID || !isHoldingKind(values.holdingKind) || !values.eventDateTime)
      return fail(400, failure('createHolding', 'Account, instrument, holding kind, and event date are required.', values));

    try {
      const holdingCreatedRequest: HoldingCreatedRequest = {
        accountID: values.accountID,
        active: values.active,
        accountName: values.accountName,
        bankName: values.bankName,
        default: values.default,
        eventDateTime: toApiDateTime(values.eventDateTime),
        holdingID: values.holdingID || undefined,
        holdingKind: values.holdingKind,
        instrumentID: values.instrumentID,
        name: values.name,
        sortCode: values.sortCode,
        accountNumber: values.accountNumber,
        bic: values.bic,
        iban: values.iban,
        reason: `Create holding ${values.name || values.holdingKind}`
      };

      const result = await postHoldingCreatedEvent(fetch, holdingCreatedRequest, currentUser.userID);
      return { eventID: result.eventID, holdingID: values.holdingID, intent: 'createHolding', message: `${values.name || 'Holding'} was created successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('createHolding', error instanceof Error ? error.message : 'Unable to create holding.', values));
    }
  },

  modifyHolding: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const values = readHoldingForm(formData);

    if (!values.holdingID || !isHoldingKind(values.holdingKind) || !values.eventDateTime)
      return fail(400, failure('modifyHolding', 'Holding, holding kind, and event date are required.', values));

    try {
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
        reason: `Modify holding ${values.name || values.holdingID}`
      };

      const result = await postHoldingModifiedEvent(fetch, holdingModifiedRequest, currentUser.userID);
      return { eventID: result.eventID, holdingID: values.holdingID, intent: 'modifyHolding', message: `${values.name || 'Holding'} was updated successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('modifyHolding', error instanceof Error ? error.message : 'Unable to update holding.', values));
    }
  },

  modifyHoldingActive: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const holdingID = getFormString(formData, 'holdingID');
    const name = getFormString(formData, 'name');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const active = getFormString(formData, 'active') === 'true';

    if (!holdingID || !eventDateTime)
      return fail(400, { holdingID, intent: 'modifyHoldingActive', message: 'Holding and event date are required.', status: 'failure' });

    try {
      const holdingActiveModifiedRequest: HoldingActiveModifiedRequest = {
        active,
        eventDateTime: toApiDateTime(eventDateTime),
        holdingID,
        reason: `${active ? 'Activate' : 'Deactivate'} holding ${name || holdingID}`
      };

      const result = await postHoldingActiveModifiedEvent(fetch, holdingActiveModifiedRequest, currentUser.userID);
      return { eventID: result.eventID, holdingID, intent: 'modifyHoldingActive', message: `${name || 'Holding'} was ${active ? 'activated' : 'deactivated'} successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, { holdingID, intent: 'modifyHoldingActive', message: error instanceof Error ? error.message : 'Unable to update holding status.', status: 'failure' });
    }
  }
};

function readHoldingForm(formData: FormData) {
  return {
    accountID: getFormString(formData, 'accountID'),
    active: getFormString(formData, 'active') !== 'false',
    accountName: getFormString(formData, 'accountName'),
    bankName: getFormString(formData, 'bankName'),
    bic: getFormString(formData, 'bic'),
    default: getFormString(formData, 'default') === 'true',
    eventDateTime: getFormString(formData, 'eventDateTime'),
    holdingID: getFormString(formData, 'holdingID'),
    holdingKind: getFormString(formData, 'holdingKind'),
    iban: getFormString(formData, 'iban'),
    instrumentID: getFormString(formData, 'instrumentID'),
    name: getFormString(formData, 'name'),
    sortCode: getFormString(formData, 'sortCode'),
    accountNumber: getFormString(formData, 'accountNumber'),
  };
}

function failure(intent: string, message: string, values: ReturnType<typeof readHoldingForm>) {
  return { holdingID: values.holdingID, intent, message, status: 'failure', values };
}

function isHoldingKind(value: string): value is HoldingKind {
  return [
    'CashDebt',
    'CashInvestable',
    'CashNonInvestable',
    'PositionMemo',
    'PositionCash',
    'Inflow',
    'Outflow',
    'InspecieIn',
    'InspecieOut',
    'FeesCustodian',
    'FeesAdministrator',
    'FeesBank',
    'Income',
    'Interest'
  ].includes(value);
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
