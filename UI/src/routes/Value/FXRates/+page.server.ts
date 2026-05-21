import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import {
  getFXRates,
  getFXs,
  postFXRateSetEvent,
  type FXRateSetRequest
} from '$lib/server/api';

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiValuationDate = toApiDateTime(valuationDate);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const [fxRates, fxs] = await Promise.all([
      getFXRates(fetch, apiValuationDate, apiAuditDateTime),
      getFXs(fetch, apiValuationDate, apiAuditDateTime)
    ]);

    return {
      auditDateTime,
      error: '',
      fxRates,
      fxs,
      valuationDate
    };
  } catch (error) {
    return {
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load FX rates.',
      fxRates: null,
      fxs: null,
      valuationDate
    };
  }
};

export const actions = {
  setFXRate: async ({ fetch, request }) => postRateEvent(fetch, request)
};

async function postRateEvent(fetch: typeof globalThis.fetch, request: Request) {
  const formData = await request.formData();
  const pair = getFormString(formData, 'pair').toUpperCase();
  const eventDateTime = getFormString(formData, 'eventDateTime');
  const bidText = getFormString(formData, 'bid');
  const midText = getFormString(formData, 'mid');
  const askText = getFormString(formData, 'ask');
  const bid = Number.parseFloat(bidText);
  const mid = Number.parseFloat(midText);
  const ask = Number.parseFloat(askText);
  const values = { ask: askText, bid: bidText, eventDateTime, mid: midText, pair };

  if (!pair || !eventDateTime || !bidText || !midText || !askText)
    return fail(400, {
      intent: 'setFXRate',
      message: 'Pair, bid, mid, ask, and event date are required.',
      pair,
      status: 'failure',
      values
    });

  if (![bid, mid, ask].every(Number.isFinite) || bid > mid || mid > ask)
    return fail(400, {
      intent: 'setFXRate',
      message: 'Bid, mid, and ask must be valid numbers ordered bid <= mid <= ask.',
      pair,
      status: 'failure',
      values
    });

  try {
    const rateRequest: FXRateSetRequest = {
      ask,
      bid,
      eventDateTime: toApiDateTime(eventDateTime),
      mid,
      pair,
      reason: `Set FX rate ${pair}`
    };

    const result = await postFXRateSetEvent(fetch, rateRequest, systemUserID);

    return {
      eventID: result.eventID,
      intent: 'setFXRate',
      message: `${pair} rate was set successfully.`,
      pair,
      status: 'success'
    };
  } catch (error) {
    return fail(502, {
      intent: 'setFXRate',
      message: error instanceof Error ? error.message : 'Unable to save FX rate.',
      pair,
      status: 'failure',
      values
    });
  }
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
