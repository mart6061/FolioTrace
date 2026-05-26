import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
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

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

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
  createHolding: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readHoldingForm(formData);

    if (!values.accountID || !values.instrumentID || !values.holdingType || !values.eventDateTime)
      return fail(400, failure('createHolding', 'Account, instrument, holding type, and event date are required.', values));

    try {
      const holdingCreatedRequest: HoldingCreatedRequest = {
        accountID: values.accountID,
        active: values.active,
        default: values.default,
        eventDateTime: toApiDateTime(values.eventDateTime),
        holdingID: values.holdingID || undefined,
        holdingType: values.holdingType,
        instrumentID: values.instrumentID,
        name: values.name,
        nominalType: values.nominalType || null,
        reason: `Create holding ${values.name || values.holdingType}`
      };

      const result = await postHoldingCreatedEvent(fetch, holdingCreatedRequest, systemUserID);
      return { eventID: result.eventID, holdingID: values.holdingID, intent: 'createHolding', message: `${values.name || 'Holding'} was created successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('createHolding', error instanceof Error ? error.message : 'Unable to create holding.', values));
    }
  },

  modifyHolding: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readHoldingForm(formData);

    if (!values.holdingID || !values.eventDateTime)
      return fail(400, failure('modifyHolding', 'Holding and event date are required.', values));

    try {
      const holdingModifiedRequest: HoldingModifiedRequest = {
        default: values.default,
        eventDateTime: toApiDateTime(values.eventDateTime),
        holdingID: values.holdingID,
        name: values.name,
        nominalType: values.nominalType || null,
        reason: `Modify holding ${values.name || values.holdingID}`
      };

      const result = await postHoldingModifiedEvent(fetch, holdingModifiedRequest, systemUserID);
      return { eventID: result.eventID, holdingID: values.holdingID, intent: 'modifyHolding', message: `${values.name || 'Holding'} was updated successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('modifyHolding', error instanceof Error ? error.message : 'Unable to update holding.', values));
    }
  },

  modifyHoldingActive: async ({ fetch, request }) => {
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

      const result = await postHoldingActiveModifiedEvent(fetch, holdingActiveModifiedRequest, systemUserID);
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
    default: getFormString(formData, 'default') === 'true',
    eventDateTime: getFormString(formData, 'eventDateTime'),
    holdingID: getFormString(formData, 'holdingID'),
    holdingType: getFormString(formData, 'holdingType'),
    instrumentID: getFormString(formData, 'instrumentID'),
    name: getFormString(formData, 'name'),
    nominalType: getFormString(formData, 'nominalType')
  };
}

function failure(intent: string, message: string, values: ReturnType<typeof readHoldingForm>) {
  return { holdingID: values.holdingID, intent, message, status: 'failure', values };
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
