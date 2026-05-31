import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getInstruments,
  postInstrumentCreatedEvent,
  postInstrumentIdentifierSetEvent,
  postInstrumentIdentifierUnsetEvent,
  postInstrumentModifiedEvent,
  postInstrumentTermsEquitySetEvent,
  type InstrumentCreatedRequest,
  type InstrumentIdentifierSetRequest,
  type InstrumentIdentifierUnsetRequest,
  type InstrumentModifiedRequest
} from '$lib/server/api';

const identifierTypes = ['Ticker', 'Sedol', 'ISIN', 'CUSIP', 'FIGI', 'RIC'] as const;

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiValuationDate = toApiDateTime(valuationDate);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const instruments = await getInstruments(fetch, apiValuationDate, apiAuditDateTime);

    return {
      auditDateTime,
      error: '',
      instruments,
      valuationDate
    };
  } catch (error) {
    return {
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load instruments.',
      instruments: null,
      valuationDate
    };
  }
};

export const actions = {
  createInstrument: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const name = getFormString(formData, 'name');
    const formalName = getFormString(formData, 'formalName') || name;
    const exchange = getFormString(formData, 'exchange').toUpperCase();
    const cfi = getFormString(formData, 'cfi').toUpperCase();
    const ticker = getFormString(formData, 'ticker').toUpperCase();
    const priceCountry = getFormString(formData, 'priceCountry').toUpperCase();
    const incomeCountry = getFormString(formData, 'incomeCountry').toUpperCase() || priceCountry;
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const active = formData.get('active') === 'on';
    const instrumentID = crypto.randomUUID();
    const values = { active, cfi, eventDateTime, exchange, formalName, incomeCountry, name, priceCountry, ticker };

    if (!name || !exchange || !cfi || !priceCountry || !eventDateTime)
      return fail(400, {
        intent: 'createInstrument',
        message: 'Name, exchange, CFI, price country, and event date are required.',
        status: 'failure',
        values
      });

    try {
      const instrumentRequest: InstrumentCreatedRequest = {
        active,
        cfi,
        eventDateTime: toApiDateTime(eventDateTime),
        exchange,
        formalName,
        incomeCountry,
        instrumentID,
        name,
        priceCountry,
        reason: `Create instrument ${name}`
      };

      const result = await postInstrumentCreatedEvent(fetch, instrumentRequest, currentUser.userID);

      if (ticker)
        await postInstrumentIdentifierSetEvent(fetch, {
          eventDateTime: instrumentRequest.eventDateTime,
          identifierType: 'Ticker',
          identifierValue: ticker,
          instrumentID,
          reason: `Set ticker ${ticker}`
        }, currentUser.userID);

      await postInstrumentTermsEquitySetEvent(fetch, instrumentRequest.eventDateTime, instrumentID, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'createInstrument',
        message: `${name} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'createInstrument',
        message: error instanceof Error ? error.message : 'Unable to create instrument.',
        status: 'failure',
        values
      });
    }
  },

  modifyInstrument: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const instrumentID = getFormString(formData, 'instrumentID');
    const name = getFormString(formData, 'name');
    const formalName = getFormString(formData, 'formalName') || name;
    const exchange = getFormString(formData, 'exchange').toUpperCase();
    const cfi = getFormString(formData, 'cfi').toUpperCase();
    const priceCountry = getFormString(formData, 'priceCountry').toUpperCase();
    const incomeCountry = getFormString(formData, 'incomeCountry').toUpperCase() || priceCountry;
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const logoSvg = getFormString(formData, 'logoSvg');
    const values = { cfi, eventDateTime, exchange, formalName, incomeCountry, name, priceCountry };

    if (!instrumentID || !name || !exchange || !cfi || !priceCountry || !eventDateTime)
      return fail(400, {
        instrumentID,
        intent: 'modifyInstrument',
        message: 'Name, exchange, CFI, price country, and event date are required.',
        status: 'failure',
        values
      });

    try {
      const instrumentRequest: InstrumentModifiedRequest = {
        cfi,
        eventDateTime: toApiDateTime(eventDateTime),
        exchange,
        formalName,
        incomeCountry,
        instrumentID,
        logo: logoSvg ? { svg: logoSvg } : null,
        name,
        priceCountry,
        reason: `Modify instrument ${instrumentID}`
      };

      const result = await postInstrumentModifiedEvent(fetch, instrumentRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        instrumentID,
        intent: 'modifyInstrument',
        message: `${name} was updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        instrumentID,
        intent: 'modifyInstrument',
        message: error instanceof Error ? error.message : 'Unable to update instrument.',
        status: 'failure',
        values
      });
    }
  },

  setIdentifier: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const instrumentID = getFormString(formData, 'instrumentID');
    const identifierType = normaliseIdentifierType(getFormString(formData, 'identifierType'));
    const identifierValue = getFormString(formData, 'identifierValue').toUpperCase();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const values = { eventDateTime, identifierType, identifierValue };

    if (!instrumentID || !identifierType || !identifierValue || !eventDateTime)
      return fail(400, {
        instrumentID,
        intent: 'setIdentifier',
        message: 'Identifier type, value, and event date are required.',
        status: 'failure',
        values
      });

    try {
      const identifierRequest: InstrumentIdentifierSetRequest = {
        eventDateTime: toApiDateTime(eventDateTime),
        identifierType,
        identifierValue,
        instrumentID,
        reason: `Set ${identifierType} ${identifierValue}`
      };

      const result = await postInstrumentIdentifierSetEvent(fetch, identifierRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        instrumentID,
        intent: 'setIdentifier',
        message: `${identifierType} was set successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        instrumentID,
        intent: 'setIdentifier',
        message: error instanceof Error ? error.message : 'Unable to set identifier.',
        status: 'failure',
        values
      });
    }
  },

  unsetIdentifier: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const instrumentID = getFormString(formData, 'instrumentID');
    const identifierType = normaliseIdentifierType(getFormString(formData, 'identifierType'));
    const eventDateTime = getFormString(formData, 'eventDateTime');

    if (!instrumentID || !identifierType || !eventDateTime)
      return fail(400, {
        instrumentID,
        intent: 'unsetIdentifier',
        message: 'Identifier type and event date are required.',
        status: 'failure',
        values: { eventDateTime, identifierType }
      });

    try {
      const identifierRequest: InstrumentIdentifierUnsetRequest = {
        eventDateTime: toApiDateTime(eventDateTime),
        identifierType,
        instrumentID,
        reason: `Unset ${identifierType}`
      };

      const result = await postInstrumentIdentifierUnsetEvent(fetch, identifierRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        instrumentID,
        intent: 'unsetIdentifier',
        message: `${identifierType} was removed successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        instrumentID,
        intent: 'unsetIdentifier',
        message: error instanceof Error ? error.message : 'Unable to remove identifier.',
        status: 'failure',
        values: { eventDateTime, identifierType }
      });
    }
  }
};

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function normaliseIdentifierType(value: string): InstrumentIdentifierSetRequest['identifierType'] | '' {
  const match = identifierTypes.find((identifierType) => identifierType.toLocaleLowerCase() === value.toLocaleLowerCase());
  return match ?? '';
}
