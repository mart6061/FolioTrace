import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getFormString } from '$lib/server/forms';
import { fail } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getFXRates,
  getFXs,
  getInputPolicies,
  postFXRateSetEvent,
  type FXRateSetRequest
} from '$lib/server/api';

export const load: PageServerLoad = async ({ fetch, parent, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiValuationDate = toApiDateTime(valuationDate);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  const { currentUser } = await parent();

  try {
    const [fxRates, fxs, inputPolicies] = await Promise.all([
      getFXRates(fetch, apiValuationDate, apiAuditDateTime),
      getFXs(fetch, apiValuationDate, apiAuditDateTime),
      getInputPolicies(fetch, {
        auditDateTime: apiAuditDateTime,
        controlKinds: ['Price'],
        // An FX rate is quoted in the pair's quote currency, which varies per row, so the policy is
        // resolved without one. Price precision comes from settings, never from the currency.
        eventDateTime: apiValuationDate,
        userID: currentUser?.userID
      })
    ]);

    return {
      auditDateTime,
      error: '',
      fxRates,
      fxs,
      inputPolicies,
      valuationDate
    };
  } catch (error) {
    return {
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load FX rates.',
      fxRates: null,
      fxs: null,
      inputPolicies: [],
      valuationDate
    };
  }
};

export const actions: Actions = {
  setFXRate: async ({ fetch, locals, request }) => postRateEvent(fetch, request, requireCurrentUser(locals).userID)
};

async function postRateEvent(fetch: typeof globalThis.fetch, request: Request, userID: string) {
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

    const result = await postFXRateSetEvent(fetch, rateRequest, userID);

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

