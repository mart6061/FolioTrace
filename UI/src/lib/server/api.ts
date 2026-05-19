import { env } from '$env/dynamic/private';
import type { Countries, MemoryDiagnostics } from '$lib/types';

const fallbackApiBaseUrl = 'https://localhost:7058/API';

export type CountryModifiedRequest = {
  eventDateTime: string;
  reason: string;
  alpha2: string;
  alpha3: string;
  numeric: number;
  name: string;
};

export type CountryCreatedRequest = CountryModifiedRequest;

export type EventSubmissionResponse = {
  eventID: string;
  links: {
    rel: string;
    href: string;
    method: string;
  }[];
};

export function getApiBaseUrl() {
  return (env.API_BASE_URL || fallbackApiBaseUrl).replace(/\/$/, '');
}

export async function getCountries(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/Countries/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Countries;
}

export async function getMemoryDiagnostics(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/Diagnostics/Memory`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as MemoryDiagnostics;
}

export async function postCountryCreatedEvent(
  fetchApi: typeof fetch,
  request: CountryCreatedRequest,
  userID: string
) {
  return postCountryEvent(fetchApi, 'CountryCreatedEvent', request, userID);
}

export async function postCountryModifiedEvent(
  fetchApi: typeof fetch,
  request: CountryModifiedRequest,
  userID: string
) {
  return postCountryEvent(fetchApi, 'CountryModifiedEvent', request, userID);
}

async function postCountryEvent(
  fetchApi: typeof fetch,
  eventType: 'CountryCreatedEvent' | 'CountryModifiedEvent',
  request: CountryCreatedRequest | CountryModifiedRequest,
  userID: string
) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Country/${eventType}`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json'
    },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      Alpha2: request.alpha2,
      Alpha3: request.alpha3,
      Numeric: request.numeric,
      Name: request.name
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

function readApiError(errorText: string) {
  if (!errorText)
    return '';

  try {
    const error = JSON.parse(errorText) as { validationErrors?: string[]; title?: string };
    return error.validationErrors?.join(' ') || error.title || errorText;
  } catch {
    return errorText;
  }
}
