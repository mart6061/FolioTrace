import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import {
  getCountries,
  getFXs,
  postFXActiveModifiedEvent,
  postFXCreatedEvent,
  type FXActiveModifiedRequest,
  type FXCreatedRequest
} from '$lib/server/api';

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const eventDateTime = toApiDateTime(valuationDate);
  const asOfDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const [fxs, countries] = await Promise.all([
      getFXs(fetch, eventDateTime, asOfDateTime),
      getCountries(fetch, eventDateTime, asOfDateTime)
    ]);

    return {
      auditDateTime,
      countryOptions: countries.items
        .map((country) => ({ alpha3: country.alpha3, name: country.name }))
        .sort((left, right) => left.alpha3.localeCompare(right.alpha3)),
      error: '',
      fxs,
      valuationDate
    };
  } catch (error) {
    return {
      auditDateTime,
      countryOptions: [],
      error: error instanceof Error ? error.message : 'Unable to load FXs.',
      fxs: null,
      valuationDate
    };
  }
};

export const actions = {
  createFX: async ({ fetch, request }) => {
    const formData = await request.formData();
    const baseCurrency = getFormString(formData, 'baseCurrency').toUpperCase();
    const quoteCurrency = getFormString(formData, 'quoteCurrency').toUpperCase();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const active = formData.get('active') === 'on';

    if (!baseCurrency || !quoteCurrency || !eventDateTime)
      return fail(400, {
        intent: 'createFX',
        message: 'Base currency, quote currency, and event date are required.',
        status: 'failure',
        values: { active, baseCurrency, eventDateTime, quoteCurrency }
      });

    try {
      const fxCreatedRequest: FXCreatedRequest = {
        active,
        baseCurrency,
        eventDateTime: toApiDateTime(eventDateTime),
        quoteCurrency,
        reason: `Create FX ${baseCurrency}${quoteCurrency}`
      };

      const result = await postFXCreatedEvent(fetch, fxCreatedRequest, systemUserID);

      return {
        eventID: result.eventID,
        intent: 'createFX',
        message: `${baseCurrency}${quoteCurrency} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'createFX',
        message: error instanceof Error ? error.message : 'Unable to create FX.',
        status: 'failure',
        values: { active, baseCurrency, eventDateTime, quoteCurrency }
      });
    }
  },

  modifyActive: async ({ fetch, request }) => {
    const formData = await request.formData();
    const pair = getFormString(formData, 'pair').toUpperCase();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const active = formData.get('active') === 'true';

    if (!pair || !eventDateTime)
      return fail(400, {
        intent: 'modifyActive',
        message: 'Pair and event date are required.',
        status: 'failure'
      });

    try {
      const fxActiveModifiedRequest: FXActiveModifiedRequest = {
        active,
        eventDateTime: toApiDateTime(eventDateTime),
        pair,
        reason: `${active ? 'Activate' : 'Deactivate'} FX ${pair}`
      };

      const result = await postFXActiveModifiedEvent(fetch, fxActiveModifiedRequest, systemUserID);

      return {
        eventID: result.eventID,
        intent: 'modifyActive',
        message: `${pair} was ${active ? 'activated' : 'deactivated'} successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'modifyActive',
        message: error instanceof Error ? error.message : 'Unable to update FX active state.',
        status: 'failure'
      });
    }
  }
};

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
