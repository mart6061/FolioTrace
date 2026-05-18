import { todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import { getApiBaseUrl, getCountries, postCountryModifiedEvent, type CountryModifiedRequest } from '$lib/server/api';

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = url.searchParams.get('auditDateTime') || '';

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

export const actions = {
  modifyCountry: async ({ fetch, request }) => {
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
        message: 'Country, Alpha-3, numeric code, name, and event date are required.',
        status: 'failure',
        values: { alpha3, eventDateTime, name, numeric: numericText }
      });

    if (!Number.isInteger(numeric) || numeric < 0 || numeric > 999)
      return fail(400, {
        alpha2,
        message: 'Numeric code must be between 000 and 999.',
        status: 'failure',
        values: { alpha3, eventDateTime, name, numeric: numericText }
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

      const result = await postCountryModifiedEvent(fetch, countryModifiedRequest, systemUserID);

      return {
        alpha2,
        eventID: result.eventID,
        message: `${alpha2} was updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        alpha2,
        message: error instanceof Error ? error.message : 'Unable to update country.',
        status: 'failure',
        values: { alpha3, eventDateTime, name, numeric: numericText }
      });
    }
  }
};

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
