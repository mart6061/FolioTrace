import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getFormString } from '$lib/server/forms';
import { fail } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { requireCurrentUser } from '$lib/server/currentUser';
import {
  getInputPolicies,
  getInstrumentValues,
  postInstrumentAccruedInterestSetEvent,
  postInstrumentPriceSetEvent,
  type InstrumentAccruedInterestSetRequest,
  type InstrumentPriceSetRequest
} from '$lib/server/api';

export const load: PageServerLoad = async ({ fetch, parent, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiValuationDate = toApiDateTime(valuationDate);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  const { currentUser } = await parent();

  try {
    const [instrumentValues, inputPolicies] = await Promise.all([
      getInstrumentValues(fetch, apiValuationDate, apiAuditDateTime),
      getInputPolicies(fetch, {
        auditDateTime: apiAuditDateTime,
        controlKinds: ['Price'],
        eventDateTime: apiValuationDate,
        userID: currentUser?.userID
      })
    ]);

    return {
      auditDateTime,
      error: '',
      inputPolicies,
      instrumentValues,
      valuationDate
    };
  } catch (error) {
    return {
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load instrument values.',
      inputPolicies: [],
      instrumentValues: null,
      valuationDate
    };
  }
};

export const actions: Actions = {
  setInstrumentPrice: async ({ fetch, locals, request }) => postPriceEvent(fetch, request, requireCurrentUser(locals).userID),
  setAccruedInterest: async ({ fetch, locals, request }) => postAccruedInterestEvent(fetch, request, requireCurrentUser(locals).userID)
};

async function postAccruedInterestEvent(fetch: typeof globalThis.fetch, request: Request, userID: string) {
  const formData = await request.formData();
  const instrumentID = getFormString(formData, 'instrumentID');
  const eventDateTime = getFormString(formData, 'eventDateTime');
  const accruedText = getFormString(formData, 'accruedInterest');
  const accrued = Number.parseFloat(accruedText);

  if (!instrumentID || !eventDateTime)
    return fail(400, { instrumentID, intent: 'setAccruedInterest', message: 'Instrument and event date are required.', status: 'failure' });

  if (accruedText && !Number.isFinite(accrued))
    return fail(400, { instrumentID, intent: 'setAccruedInterest', message: 'Accrued interest must be a valid number.', status: 'failure' });

  try {
    const accruedRequest: InstrumentAccruedInterestSetRequest = {
      // An empty field clears the accrual rather than posting zero, which would be a real value.
      accruedInterest: accruedText ? accrued : null,
      eventDateTime: toApiDateTime(eventDateTime),
      instrumentID,
      reason: `Set accrued interest ${instrumentID}`
    };

    const result = await postInstrumentAccruedInterestSetEvent(fetch, accruedRequest, userID);

    return {
      eventID: result.eventID,
      instrumentID,
      intent: 'setAccruedInterest',
      message: 'Accrued interest was set successfully.',
      status: 'success'
    };
  } catch (error) {
    return fail(502, {
      instrumentID,
      intent: 'setAccruedInterest',
      message: error instanceof Error ? error.message : 'Unable to set accrued interest.',
      status: 'failure'
    });
  }
}

async function postPriceEvent(fetch: typeof globalThis.fetch, request: Request, userID: string) {
  const formData = await request.formData();
  const instrumentID = getFormString(formData, 'instrumentID');
  const eventDateTime = getFormString(formData, 'eventDateTime');
  const currency = getFormString(formData, 'currency').toUpperCase();
  const priceType = getFormString(formData, 'priceType');
  const bidText = getFormString(formData, 'bid');
  const midText = getFormString(formData, 'mid');
  const askText = getFormString(formData, 'ask');
  const lastText = getFormString(formData, 'last');
  const navText = getFormString(formData, 'nav');
  const bid = Number.parseFloat(bidText);
  const mid = Number.parseFloat(midText);
  const ask = Number.parseFloat(askText);
  const last = Number.parseFloat(lastText);
  const nav = Number.parseFloat(navText);

  if (!instrumentID || !eventDateTime || !currency || !priceType)
    return fail(400, { instrumentID, intent: 'setInstrumentPrice', message: 'Instrument, currency, price type, and event date are required.', status: 'failure' });

  // Quotes are optional individually, but whatever is supplied must be a number and correctly ordered.
  const supplied = [bidText, midText, askText].filter((text) => text !== '');
  if (supplied.length && ![bid, mid, ask].filter((value) => !Number.isNaN(value)).every(Number.isFinite))
    return fail(400, { instrumentID, intent: 'setInstrumentPrice', message: 'Bid, mid, and ask must be valid numbers.', status: 'failure' });

  if ((Number.isFinite(bid) && Number.isFinite(mid) && bid > mid) || (Number.isFinite(mid) && Number.isFinite(ask) && mid > ask))
    return fail(400, { instrumentID, intent: 'setInstrumentPrice', message: 'Quotes must be ordered bid <= mid <= ask.', status: 'failure' });

  if (priceType !== 'InstrumentPriceEquity' && priceType !== 'InstrumentPriceFixedIncome')
    return fail(400, { instrumentID, intent: 'setInstrumentPrice', message: 'Only equity and fixed income price edits are supported.', status: 'failure' });

  try {
    const priceRequest: InstrumentPriceSetRequest = {
      currency,
      eventDateTime: toApiDateTime(eventDateTime),
      instrumentID,
      priceType,
      reason: `Set instrument price ${instrumentID}`
    };

    const optional = (value: number) => (Number.isFinite(value) ? value : null);

    priceRequest.bid = optional(bid);
    priceRequest.mid = optional(mid);
    priceRequest.ask = optional(ask);

    if (priceType === 'InstrumentPriceEquity') {
      priceRequest.last = optional(last);
      priceRequest.nav = optional(nav);
    }

    const result = await postInstrumentPriceSetEvent(fetch, priceRequest, userID);

    return {
      eventID: result.eventID,
      instrumentID,
      intent: 'setInstrumentPrice',
      message: 'Instrument price was set successfully.',
      status: 'success'
    };
  } catch (error) {
    return fail(502, {
      instrumentID,
      intent: 'setInstrumentPrice',
      message: error instanceof Error ? error.message : 'Unable to save instrument price.',
      status: 'failure'
    });
  }
}

