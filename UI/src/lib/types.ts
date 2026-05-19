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
