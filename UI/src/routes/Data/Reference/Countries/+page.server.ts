import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getApiBaseUrl,
  getCountries,
  postCountryCreatedEvent,
  postCountryModifiedEvent,
  type CountryCreatedRequest,
  type CountryModifiedRequest
} from '$lib/server/api';

export const load: PageServerLoad = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const countries = await getCountries(
      fetch,
      toApiDateTime(valuationDate),
      auditDateTime ? toApiDateTime(auditDateTime) : null
    );

    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      countries,
      error: '',
      valuationDate
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      countries: null,
      error: error instanceof Error ? error.message : 'Unable to load countries.',
      valuationDate
    };
  }
};

export const actions: Actions = {
  createCountry: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const alpha2 = getFormString(formData, 'alpha2').toUpperCase();
    const alpha3 = getFormString(formData, 'alpha3').toUpperCase();
    const name = getFormString(formData, 'name');
    const numericText = getFormString(formData, 'numeric');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const numeric = Number.parseInt(numericText, 10);

    if (!alpha2 || !alpha3 || !name || !numericText || !eventDateTime)
      return fail(400, {
        alpha2,
        intent: 'createCountry',
        message: 'Country, Alpha-2, Alpha-3, numeric code, name, and event date are required.',
        status: 'failure',
        values: { alpha2, alpha3, eventDateTime, name, numeric: numericText }
      });

    if (!Number.isInteger(numeric) || numeric < 0 || numeric > 999)
      return fail(400, {
        alpha2,
        intent: 'createCountry',
        message: 'Numeric code must be between 000 and 999.',
        status: 'failure',
        values: { alpha2, alpha3, eventDateTime, name, numeric: numericText }
      });

    try {
      const countryCreatedRequest: CountryCreatedRequest = {
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `Create country ${alpha2}`,
        alpha2,
        alpha3,
        numeric,
        name
      };

      const result = await postCountryCreatedEvent(fetch, countryCreatedRequest, currentUser.userID);

      return {
        alpha2,
        eventID: result.eventID,
        intent: 'createCountry',
        message: `${alpha2} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        alpha2,
        intent: 'createCountry',
        message: error instanceof Error ? error.message : 'Unable to create country.',
        status: 'failure',
        values: { alpha2, alpha3, eventDateTime, name, numeric: numericText }
      });
    }
  },

  modifyCountry: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const alpha2 = getFormString(formData, 'alpha2').toUpperCase();
    const alpha3 = getFormString(formData, 'alpha3').toUpperCase();
    const name = getFormString(formData, 'name');
    const numericText = getFormString(formData, 'numeric');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const numeric = Number.parseInt(numericText, 10);

    if (!alpha2 || !alpha3 || !name || !numericText || !eventDateTime)
      return fail(400, {
        alpha2,
        intent: 'modifyCountry',
        message: 'Country, Alpha-3, numeric code, name, and event date are required.',
        status: 'failure',
        values: { alpha2, alpha3, eventDateTime, name, numeric: numericText }
      });

    if (!Number.isInteger(numeric) || numeric < 0 || numeric > 999)
      return fail(400, {
        alpha2,
        intent: 'modifyCountry',
        message: 'Numeric code must be between 000 and 999.',
        status: 'failure',
        values: { alpha2, alpha3, eventDateTime, name, numeric: numericText }
      });

    try {
      const countryModifiedRequest: CountryModifiedRequest = {
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `Modify country ${alpha2}`,
        alpha2,
        alpha3,
        numeric,
        name
      };

      const result = await postCountryModifiedEvent(fetch, countryModifiedRequest, currentUser.userID);

      return {
        alpha2,
        eventID: result.eventID,
        intent: 'modifyCountry',
        message: `${alpha2} was updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        alpha2,
        intent: 'modifyCountry',
        message: error instanceof Error ? error.message : 'Unable to update country.',
        status: 'failure',
        values: { alpha2, alpha3, eventDateTime, name, numeric: numericText }
      });
    }
  }
};

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
