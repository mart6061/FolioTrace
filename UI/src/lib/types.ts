export type CountryFlag = {
  svg: string;
};

export type Country = {
  alpha2: string;
  alpha3: string;
  numeric: number;
  name: string;
  flag?: CountryFlag | null;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type Countries = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: Country[];
};

export type Currency = {
  alphabeticCode: string;
  numericCode: number;
  decimalPlace: number;
  name: string;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type Currencies = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: Currency[];
};

export type FX = {
  pair: string;
  baseCurrency: string;
  quoteCurrency: string;
  displayPair: string;
  active: boolean;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type FXs = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: FX[];
};

export type FXPrice = {
  bid: number;
  mid: number;
  ask: number;
};

export type FXRate = FX & {
  price: FXPrice;
};

export type FXRates = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: FXRate[];
};

export type ReferenceEventBase = {
  $type: string;
  eventID: string;
  userID: string;
  eventDateTime: string;
  auditDateTime: string;
  reason: string;
};

export type CountryReferenceEvent = ReferenceEventBase & {
  alpha2: string;
  alpha3?: string;
  numeric?: number;
  name?: string;
  flag?: CountryFlag | null;
};

export type CurrencyReferenceEvent = ReferenceEventBase & {
  alphabeticCode: string;
  numericCode: number;
  decimalPlace: number;
  name: string;
};

export type MemoryDiagnostics = {
  eventCache: {
    isLoaded: boolean;
    streamCount: number;
    eventCount: number;
  };
  countryService: {
    cacheEntryCount: number;
    countryCount: number;
  };
  currencyService: {
    cacheEntryCount: number;
    currencyCount: number;
  };
  fxService: {
    cacheEntryCount: number;
    fxCount: number;
  };
  fxRateService: {
    cacheEntryCount: number;
    fxRateCount: number;
  };
};

export type ApiHttpMessage = {
  headers: Record<string, string[]>;
  body: string | null;
  contentType: string | null;
  contentLength: number | null;
  bodyTruncated: boolean;
};

export type ApiExchange = {
  id: string;
  startedAtUtc: string;
  completedAtUtc: string;
  durationMilliseconds: number;
  method: string;
  path: string;
  queryString: string;
  statusCode: number | null;
  exceptionType: string | null;
  exceptionMessage: string | null;
  request: ApiHttpMessage;
  response: ApiHttpMessage;
};

export type ApiExchangeSearchResponse = {
  items: ApiExchange[];
  totalCount: number;
  page: number;
  pageSize: number;
};
