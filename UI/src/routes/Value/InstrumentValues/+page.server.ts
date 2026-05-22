import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import {
  getInstrumentValues,
  postInstrumentPriceSetEvent,
  type InstrumentPriceSetRequest
} from '$lib/server/api';

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiValuationDate = toApiDateTime(valuationDate);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const instrumentValues = await getInstrumentValues(fetch, apiValuationDate, apiAuditDateTime);

    return {
      auditDateTime,
      error: '',
      instrumentValues,
      valuationDate
    };
  } catch (error) {
    return {
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load instrument values.',
      instrumentValues: null,
      valuationDate
    };
  }
};

export const actions = {
  setInstrumentPrice: async ({ fetch, request }) => postPriceEvent(fetch, request)
};

async function postPriceEvent(fetch: typeof globalThis.fetch, request: Request) {
  const formData = await request.formData();
  const instrumentID = getFormString(formData, 'instrumentID');
  const eventDateTime = getFormString(formData, 'eventDateTime');
  const currency = getFormString(formData, 'currency').toUpperCase();
  const bidText = getFormString(formData, 'bid');
  const midText = getFormString(formData, 'mid');
  const askText = getFormString(formData, 'ask');
  const navText = getFormString(formData, 'nav');
  const bid = Number.parseFloat(bidText);
  const mid = Number.parseFloat(midText);
  const ask = Number.parseFloat(askText);
  const nav = Number.parseFloat(navText);

  if (!instrumentID || !eventDateTime || !currency || !bidText || !midText || !askText || !navText)
    return fail(400, { instrumentID, intent: 'setInstrumentPrice', message: 'Instrument, currency, bid, mid, ask, nav, and event date are required.', status: 'failure' });

  if (![bid, mid, ask, nav].every(Number.isFinite) || bid > mid || mid > ask)
    return fail(400, { instrumentID, intent: 'setInstrumentPrice', message: 'Bid, mid, ask, and nav must be valid numbers ordered bid <= mid <= ask.', status: 'failure' });

  try {
    const priceRequest: InstrumentPriceSetRequest = {
      ask,
      bid,
      currency,
      eventDateTime: toApiDateTime(eventDateTime),
      instrumentID,
      mid,
      nav,
      reason: `Set instrument price ${instrumentID}`
    };

    const result = await postInstrumentPriceSetEvent(fetch, priceRequest, systemUserID);

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

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
