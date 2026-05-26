import { env } from '$env/dynamic/private';
import type {
  Accounts,
  AccountReferenceEvent,
  ApiExchangeSearchResponse,
  Countries,
  CountryReferenceEvent,
  Currencies,
  CurrencyReferenceEvent,
  FXRates,
  FXRateHistoryEvent,
  FXs,
  Holdings,
  HoldingReferenceEvent,
  InstrumentLogo,
  InstrumentValues,
  InstrumentValueHistoryEvent,
  Instruments,
  MemoryDiagnostics,
  BuildProgressNotification
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

export type AccountModifiedRequest = {
  eventDateTime: string;
  reason: string;
  accountID: string;
  name: string;
  formalName: string;
};

export type AccountCreatedRequest = AccountModifiedRequest & {
  bookCurrency: string;
  active: boolean;
};

export type AccountActiveModifiedRequest = {
  eventDateTime: string;
  reason: string;
  accountID: string;
  active: boolean;
};

export type HoldingCreatedRequest = {
  eventDateTime: string;
  reason: string;
  holdingID?: string;
  accountID: string;
  instrumentID: string;
  holdingType: string;
  nominalType?: string | null;
  name: string;
  active: boolean;
  default: boolean;
};

export type HoldingModifiedRequest = {
  eventDateTime: string;
  reason: string;
  holdingID: string;
  nominalType?: string | null;
  name: string;
  default: boolean;
};

export type HoldingActiveModifiedRequest = {
  eventDateTime: string;
  reason: string;
  holdingID: string;
  active: boolean;
};

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

export type InstrumentPriceSetRequest = {
  eventDateTime: string;
  reason: string;
  instrumentID: string;
  currency: string;
  priceType: 'InstrumentPriceEquity' | 'InstrumentPriceFixedIncome';
  bid?: number;
  mid?: number;
  ask?: number;
  nav?: number;
  cleanPrice?: number;
};

export type InstrumentCreatedRequest = {
  eventDateTime: string;
  reason: string;
  instrumentID: string;
  name: string;
  formalName: string;
  exchange: string;
  cfi: string;
  active: boolean;
  incomeCountry: string;
  priceCountry: string;
};

export type InstrumentModifiedRequest = Omit<InstrumentCreatedRequest, 'active'> & {
  logo?: InstrumentLogo | null;
};

export type InstrumentIdentifierSetRequest = {
  eventDateTime: string;
  reason: string;
  instrumentID: string;
  identifierType: 'Ticker' | 'Sedol' | 'ISIN' | 'CUSIP' | 'FIGI' | 'RIC';
  identifierValue: string;
};

export type InstrumentIdentifierUnsetRequest = {
  eventDateTime: string;
  reason: string;
  instrumentID: string;
  identifierType: 'Ticker' | 'Sedol' | 'ISIN' | 'CUSIP' | 'FIGI' | 'RIC';
};

export type EventSubmissionResponse = {
  eventID: string;
  links: {
    rel: string;
    href: string;
    method: string;
  }[];
};

export class ApiError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message);
  }
}

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

export async function getAccounts(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/Accounts/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Accounts;
}

export async function getHoldings(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null,
  includeInactive = true
) {
  const url = new URL(`${getApiBaseUrl()}/Holdings/`);
  url.searchParams.set('eventDateTime', eventDateTime);
  url.searchParams.set('includeInactive', String(includeInactive));

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Holdings;
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

export async function getAccountEvents(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Account/`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as AccountReferenceEvent[];
}

export async function getHoldingEvents(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Holding/`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as HoldingReferenceEvent[];
}

export async function getInstrumentPriceEvents(fetchApi: typeof fetch, instrumentID?: string) {
  const url = new URL(`${getApiBaseUrl()}/Events/InstrumentPrice/`);

  if (instrumentID)
    url.searchParams.set('instrumentID', instrumentID);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as InstrumentValueHistoryEvent[];
}

export async function getInstrumentIncomeEvents(fetchApi: typeof fetch, instrumentID?: string) {
  const url = new URL(`${getApiBaseUrl()}/Events/InstrumentIncome/`);

  if (instrumentID)
    url.searchParams.set('instrumentID', instrumentID);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as InstrumentValueHistoryEvent[];
}

export async function getFXRateEvents(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/FXRate/`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as FXRateHistoryEvent[];
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

export async function getInstruments(fetchApi: typeof fetch, eventDateTime: string, auditDateTime: string | null) {
  const url = new URL(`${getApiBaseUrl()}/Instruments/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Instruments;
}

export async function getInstrumentValues(fetchApi: typeof fetch, eventDateTime: string, auditDateTime: string | null) {
  const url = new URL(`${getApiBaseUrl()}/InstrumentValues/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as InstrumentValues;
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
    throw new ApiError(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`, response.status);
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
    progress: BuildProgressNotification;
  };
}

export async function postSystemClearCacheAndProjections(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/System/ClearCacheAndProjections`, {
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
    clearedProjections: string[];
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

export async function postAccountCreatedEvent(
  fetchApi: typeof fetch,
  request: AccountCreatedRequest,
  userID: string
) {
  return postAccountEvent(fetchApi, 'AccountCreatedEvent', request, userID);
}

export async function postAccountModifiedEvent(
  fetchApi: typeof fetch,
  request: AccountModifiedRequest,
  userID: string
) {
  return postAccountEvent(fetchApi, 'AccountModifiedEvent', request, userID);
}

export async function postAccountActiveModifiedEvent(
  fetchApi: typeof fetch,
  request: AccountActiveModifiedRequest,
  userID: string
) {
  return postAccountEvent(fetchApi, 'AccountActiveModifiedEvent', request, userID);
}

export async function postHoldingCreatedEvent(
  fetchApi: typeof fetch,
  request: HoldingCreatedRequest,
  userID: string
) {
  return postHoldingEvent(fetchApi, 'HoldingCreatedEvent', request, userID);
}

export async function postHoldingModifiedEvent(
  fetchApi: typeof fetch,
  request: HoldingModifiedRequest,
  userID: string
) {
  return postHoldingEvent(fetchApi, 'HoldingModifiedEvent', request, userID);
}

export async function postHoldingActiveModifiedEvent(
  fetchApi: typeof fetch,
  request: HoldingActiveModifiedRequest,
  userID: string
) {
  return postHoldingEvent(fetchApi, 'HoldingActiveModifiedEvent', request, userID);
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

export async function postInstrumentPriceSetEvent(fetchApi: typeof fetch, request: InstrumentPriceSetRequest, userID: string) {
  const price = (amount: number) => ({ Amount: amount });
  const eventPrice = request.priceType === 'InstrumentPriceFixedIncome'
    ? {
        $type: 'InstrumentPriceFixedIncome',
        CleanPrice: { Amount: request.cleanPrice }
      }
    : {
        $type: 'InstrumentPriceEquity',
        Bid: price(request.bid ?? 0),
        Mid: price(request.mid ?? 0),
        Ask: price(request.ask ?? 0),
        Nav: price(request.nav ?? 0)
      };
  const response = await fetchApi(`${getApiBaseUrl()}/Events/InstrumentPrice/InstrumentPriceSetEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      InstrumentID: request.instrumentID,
      Price: eventPrice
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postInstrumentCreatedEvent(fetchApi: typeof fetch, request: InstrumentCreatedRequest, userID: string) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Instrument/InstrumentCreatedEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      InstrumentID: request.instrumentID,
      Name: request.name,
      FormalName: request.formalName,
      Exchange: request.exchange,
      CFI: request.cfi,
      Logo: null,
      Active: request.active,
      IncomeCountry: request.incomeCountry,
      PriceCountry: request.priceCountry
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postInstrumentModifiedEvent(fetchApi: typeof fetch, request: InstrumentModifiedRequest, userID: string) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Instrument/InstrumentModifiedEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      InstrumentID: request.instrumentID,
      Name: request.name,
      FormalName: request.formalName,
      Exchange: request.exchange,
      CFI: request.cfi,
      Logo: request.logo ? { Svg: request.logo.svg } : null,
      IncomeCountry: request.incomeCountry,
      PriceCountry: request.priceCountry
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postInstrumentIdentifierSetEvent(fetchApi: typeof fetch, request: InstrumentIdentifierSetRequest, userID: string) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Instrument/InstrumentIdentifierSetEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      InstrumentID: request.instrumentID,
      Identifier: {
        Type: request.identifierType,
        Value: request.identifierValue
      }
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postInstrumentIdentifierUnsetEvent(fetchApi: typeof fetch, request: InstrumentIdentifierUnsetRequest, userID: string) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Instrument/InstrumentIdentifierUnsetEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      InstrumentID: request.instrumentID,
      IdentifierType: request.identifierType
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postInstrumentTermsEquitySetEvent(fetchApi: typeof fetch, eventDateTime: string, instrumentID: string, userID: string) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Instrument/InstrumentTermsSetEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: eventDateTime,
      Reason: `Set equity terms ${instrumentID}`,
      InstrumentID: instrumentID,
      Terms: {
        $type: 'InstrumentTermsEquity'
      }
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
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

async function postAccountEvent(
  fetchApi: typeof fetch,
  eventType: 'AccountCreatedEvent' | 'AccountModifiedEvent' | 'AccountActiveModifiedEvent',
  request: AccountCreatedRequest | AccountModifiedRequest | AccountActiveModifiedRequest,
  userID: string
) {
  const body: Record<string, unknown> = {
    UserID: userID,
    EventDateTime: request.eventDateTime,
    Reason: request.reason
  };

  if (request.accountID)
    body.AccountID = request.accountID;

  if ('name' in request) {
    body.Name = request.name;
    body.FormalName = request.formalName;
  }

  if (eventType === 'AccountCreatedEvent')
    body.BookCurrency = (request as AccountCreatedRequest).bookCurrency;

  if ('active' in request)
    body.Active = request.active;

  const response = await fetchApi(`${getApiBaseUrl()}/Events/Account/${eventType}`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json'
    },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

async function postHoldingEvent(
  fetchApi: typeof fetch,
  eventType: 'HoldingCreatedEvent' | 'HoldingModifiedEvent' | 'HoldingActiveModifiedEvent',
  request: HoldingCreatedRequest | HoldingModifiedRequest | HoldingActiveModifiedRequest,
  userID: string
) {
  const body: Record<string, unknown> = {
    UserID: userID,
    EventDateTime: request.eventDateTime,
    Reason: request.reason,
    HoldingID: request.holdingID
  };

  if (eventType === 'HoldingCreatedEvent') {
    const created = request as HoldingCreatedRequest;
    if (!created.holdingID)
      delete body.HoldingID;
    body.AccountID = created.accountID;
    body.InstrumentID = created.instrumentID;
    body.HoldingType = created.holdingType;
    body.NominalType = created.nominalType || null;
    body.Name = created.name;
    body.Active = created.active;
    body.Default = created.default;
  } else if (eventType === 'HoldingModifiedEvent') {
    const modified = request as HoldingModifiedRequest;
    body.NominalType = modified.nominalType || null;
    body.Name = modified.name;
    body.Default = modified.default;
  } else if ('active' in request) {
    body.Active = request.active;
  }

  const response = await fetchApi(`${getApiBaseUrl()}/Events/Holding/${eventType}`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json'
    },
    body: JSON.stringify(body)
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
    const error = JSON.parse(errorText) as { message?: string; validationErrors?: string[]; title?: string };
    return error.validationErrors?.join(' ') || error.message || error.title || errorText;
  } catch {
    return errorText;
  }
}
