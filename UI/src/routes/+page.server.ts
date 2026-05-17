import { todayEndForInput, toApiDateTime } from '$lib/dates';
import { getApiBaseUrl, getCountries } from '$lib/server/api';

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
