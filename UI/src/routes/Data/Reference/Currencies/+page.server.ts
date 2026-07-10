import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getApiBaseUrl,
  getCurrencies,
  postCurrencyCreatedEvent,
  postCurrencyModifiedEvent,
  type CurrencyCreatedRequest,
  type CurrencyModifiedRequest
} from '$lib/server/api';

export const load: PageServerLoad = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const currencies = await getCurrencies(
      fetch,
      toApiDateTime(valuationDate),
      auditDateTime ? toApiDateTime(auditDateTime) : null
    );

    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      currencies,
      error: '',
      valuationDate
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      currencies: null,
      error: error instanceof Error ? error.message : 'Unable to load currencies.',
      valuationDate
    };
  }
};

export const actions: Actions = {
  createCurrency: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const alphabeticCode = getFormString(formData, 'alphabeticCode').toUpperCase();
    const name = getFormString(formData, 'name');
    const numericCodeText = getFormString(formData, 'numericCode');
    const decimalPlaceText = getFormString(formData, 'decimalPlace');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const numericCode = Number.parseInt(numericCodeText, 10);
    const decimalPlace = Number.parseInt(decimalPlaceText, 10);

    const values = { alphabeticCode, decimalPlace: decimalPlaceText, eventDateTime, name, numericCode: numericCodeText };

    if (!alphabeticCode || !name || !numericCodeText || !decimalPlaceText || !eventDateTime)
      return fail(400, {
        alphabeticCode,
        intent: 'createCurrency',
        message: 'Currency, alphabetic code, numeric code, decimal places, and event date are required.',
        status: 'failure',
        values
      });

    if (!Number.isInteger(numericCode) || numericCode < 0 || numericCode > 999)
      return fail(400, {
        alphabeticCode,
        intent: 'createCurrency',
        message: 'Numeric code must be between 000 and 999.',
        status: 'failure',
        values
      });

    if (!Number.isInteger(decimalPlace) || decimalPlace < 0)
      return fail(400, {
        alphabeticCode,
        intent: 'createCurrency',
        message: 'Decimal places must be zero or greater.',
        status: 'failure',
        values
      });

    try {
      const currencyCreatedRequest: CurrencyCreatedRequest = {
        alphabeticCode,
        decimalPlace,
        eventDateTime: toApiDateTime(eventDateTime),
        name,
        numericCode,
        reason: `Create currency ${alphabeticCode}`
      };

      const result = await postCurrencyCreatedEvent(fetch, currencyCreatedRequest, currentUser.userID);

      return {
        alphabeticCode,
        eventID: result.eventID,
        intent: 'createCurrency',
        message: `${alphabeticCode} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        alphabeticCode,
        intent: 'createCurrency',
        message: error instanceof Error ? error.message : 'Unable to create currency.',
        status: 'failure',
        values
      });
    }
  },

  modifyCurrency: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const alphabeticCode = getFormString(formData, 'alphabeticCode').toUpperCase();
    const name = getFormString(formData, 'name');
    const numericCodeText = getFormString(formData, 'numericCode');
    const decimalPlaceText = getFormString(formData, 'decimalPlace');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const numericCode = Number.parseInt(numericCodeText, 10);
    const decimalPlace = Number.parseInt(decimalPlaceText, 10);

    const values = { alphabeticCode, decimalPlace: decimalPlaceText, eventDateTime, name, numericCode: numericCodeText };

    if (!alphabeticCode || !name || !numericCodeText || !decimalPlaceText || !eventDateTime)
      return fail(400, {
        alphabeticCode,
        intent: 'modifyCurrency',
        message: 'Currency, numeric code, decimal places, name, and event date are required.',
        status: 'failure',
        values
      });

    if (!Number.isInteger(numericCode) || numericCode < 0 || numericCode > 999)
      return fail(400, {
        alphabeticCode,
        intent: 'modifyCurrency',
        message: 'Numeric code must be between 000 and 999.',
        status: 'failure',
        values
      });

    if (!Number.isInteger(decimalPlace) || decimalPlace < 0)
      return fail(400, {
        alphabeticCode,
        intent: 'modifyCurrency',
        message: 'Decimal places must be zero or greater.',
        status: 'failure',
        values
      });

    try {
      const currencyModifiedRequest: CurrencyModifiedRequest = {
        alphabeticCode,
        decimalPlace,
        eventDateTime: toApiDateTime(eventDateTime),
        name,
        numericCode,
        reason: `Modify currency ${alphabeticCode}`
      };

      const result = await postCurrencyModifiedEvent(fetch, currencyModifiedRequest, currentUser.userID);

      return {
        alphabeticCode,
        eventID: result.eventID,
        intent: 'modifyCurrency',
        message: `${alphabeticCode} was updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        alphabeticCode,
        intent: 'modifyCurrency',
        message: error instanceof Error ? error.message : 'Unable to update currency.',
        status: 'failure',
        values
      });
    }
  }
};

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
