import { env } from '$env/dynamic/private';
import type {
  ApiExchangeSearchResponse,
  Countries,
  CountryReferenceEvent,
  Currencies,
  CurrencyReferenceEvent,
  FXRates,
  FXs,
  MemoryDiagnostics
} from '$lib/types';

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

export type CurrencyModifiedRequest = {
  eventDateTime: string;
  reason: string;
  alphabeticCode: string;
  numericCode: number;
  decimalPlace: number;
  name: string;
};

export type CurrencyCreatedRequest = CurrencyModifiedRequest;

export type FXCreatedRequest = {
  eventDateTime: string;
  reason: string;
  baseCurrency: string;
  quoteCurrency: string;
  active: boolean;
};

export type FXActiveModifiedRequest = {
  eventDateTime: string;
  reason: string;
  pair: string;
  active: boolean;
};

export type FXRateSetRequest = {
  eventDateTime: string;
  reason: string;
  pair: string;
  bid: number;
  mid: number;
  ask: number;
};

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

export async function getCurrencies(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/Currencies/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Currencies;
}

export async function getCountryEvents(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Country/`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as CountryReferenceEvent[];
}

export async function getCurrencyEvents(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Currency/`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as CurrencyReferenceEvent[];
}

export async function getFXs(fetchApi: typeof fetch, eventDateTime: string, auditDateTime: string | null) {
  const url = new URL(`${getApiBaseUrl()}/FXs/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as FXs;
}

export async function getFXRates(fetchApi: typeof fetch, eventDateTime: string, auditDateTime: string | null) {
  const url = new URL(`${getApiBaseUrl()}/FXRates/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as FXRates;
}

export async function getMemoryDiagnostics(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/Diagnostics/Memory`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as MemoryDiagnostics;
}

export async function getSystemVersion(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/System/Version`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as { apiVersion: string };
}

export async function postSystemBuild(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/System/Build`, {
    method: 'POST'
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as {
    status: string;
    message: string;
    removedCacheViews: {
      countries: number;
      currencies: number;
      fXs: number;
      fXRates: number;
    };
  };
}

export type ApiExchangeSearchRequest = {
  fromUtc?: string;
  toUtc?: string;
  method?: string;
  path?: string;
  statusCode?: string;
  minimumDurationMilliseconds?: string;
  maximumDurationMilliseconds?: string;
  text?: string;
  page?: string;
  pageSize?: string;
};

export async function getApiExchanges(fetchApi: typeof fetch, request: ApiExchangeSearchRequest) {
  const url = new URL(`${getApiBaseUrl()}/Diagnostics/RequestTrace`);

  for (const [key, value] of Object.entries(request)) {
    if (value)
      url.searchParams.set(key, value);
  }

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as ApiExchangeSearchResponse;
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

export async function postCurrencyCreatedEvent(
  fetchApi: typeof fetch,
  request: CurrencyCreatedRequest,
  userID: string
) {
  return postCurrencyEvent(fetchApi, 'CurrencyCreatedEvent', request, userID);
}

export async function postCurrencyModifiedEvent(
  fetchApi: typeof fetch,
  request: CurrencyModifiedRequest,
  userID: string
) {
  return postCurrencyEvent(fetchApi, 'CurrencyModifiedEvent', request, userID);
}

export async function postFXCreatedEvent(fetchApi: typeof fetch, request: FXCreatedRequest, userID: string) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/FX/FXCreatedEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      BaseCurrency: request.baseCurrency,
      QuoteCurrency: request.quoteCurrency,
      Active: request.active
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postFXActiveModifiedEvent(fetchApi: typeof fetch, request: FXActiveModifiedRequest, userID: string) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/FX/FXActiveModifiedEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      Pair: request.pair,
      Active: request.active
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postFXRateSetEvent(fetchApi: typeof fetch, request: FXRateSetRequest, userID: string) {
  return postFXRateEvent(fetchApi, request, userID);
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

async function postCurrencyEvent(
  fetchApi: typeof fetch,
  eventType: 'CurrencyCreatedEvent' | 'CurrencyModifiedEvent',
  request: CurrencyCreatedRequest | CurrencyModifiedRequest,
  userID: string
) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Currency/${eventType}`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json'
    },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      AlphabeticCode: request.alphabeticCode,
      NumericCode: request.numericCode,
      DecimalPlace: request.decimalPlace,
      Name: request.name
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

async function postFXRateEvent(
  fetchApi: typeof fetch,
  request: FXRateSetRequest,
  userID: string
) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/FXRate/FXRateSetEvent`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json'
    },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      Pair: request.pair,
      Price: {
        Bid: request.bid,
        Mid: request.mid,
        Ask: request.ask
      }
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
