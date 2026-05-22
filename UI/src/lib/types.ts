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

export type InstrumentLogo = {
  svg: string;
};

export type InstrumentIdentifier = {
  type: string | number;
  value: string;
};

export type Money = {
  amount: number;
  currency: string;
};

export type InstrumentPriceEquity = {
  $type?: 'InstrumentPriceEquity';
  bid: Money;
  mid: Money;
  ask: Money;
  nav: Money;
  priceType?: string;
};

export type InstrumentIncomeEquity = {
  $type?: 'InstrumentIncomeEquity';
  income: Money;
  incomeType?: string;
};

export type Instrument = {
  instrumentID: string;
  name: string;
  formalName: string;
  exchange: string;
  cfi: string;
  logo?: InstrumentLogo | null;
  active: boolean;
  incomeCountry: string;
  priceCountry: string;
  identifiers: InstrumentIdentifier[];
  terms?: unknown;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type Instruments = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: Instrument[];
};

export type InstrumentValue = Instrument & {
  price?: InstrumentPriceEquity | null;
  income?: InstrumentIncomeEquity | null;
};

export type InstrumentValues = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: InstrumentValue[];
};

export type ReferenceEventBase = {
  $type: string;
  eventID: string;
  userID: string;
  eventDateTime: string;
  auditDateTime: string;
  reason: string;
  applicationStatus?: 'applied' | 'omitted';
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
  instrumentService?: {
    cacheEntryCount: number;
    instrumentCount: number;
  };
  instrumentValueService?: {
    cacheEntryCount: number;
    instrumentValueCount: number;
  };
  sse?: {
    activeSubscriberCount: number;
    publishedNotificationCount: number;
    lastNotificationType: string | null;
    lastKind: string | null;
    lastEventID: string | null;
    lastEventDateTime: string | null;
    lastAuditDateTime: string | null;
    lastReason: string | null;
    currentBuildID: string | null;
    lastBuildStatus: string | null;
    lastBuildStage: string | null;
    lastBuildUpdatedAtUtc: string | null;
  };
  aggregateMaintenance?: {
    enabled: boolean;
    periodicDelay: string;
    eventTriggerCount: number;
    eventTriggerDelay: string;
    daysFromToday: number;
    endOfWeeksFromToday: number;
    endOfMonthsFromToday: number;
    status: string;
    activeRunID: string | null;
    lastRunID: string | null;
    lastTrigger: string | null;
    lastStartedAtUtc: string | null;
    lastCompletedAtUtc: string | null;
    lastScannedAggregates: number;
    lastMissingAggregates: number;
    lastFixedAggregates: number;
    lastFailedAggregates: number;
    totalScannedAggregates: number;
    totalMissingAggregates: number;
    totalFixedAggregates: number;
    totalFailedAggregates: number;
    skippedRunCount: number;
    pendingEventCount: number;
    isSuspended: boolean;
    suspensionReason: string | null;
    suspendedAtUtc: string | null;
    suspendedRunCount: number;
    lastError: string | null;
    recentErrors: string[];
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

export type AggregateKind =
  | 'Countries'
  | 'Currencies'
  | 'FXs'
  | 'FXRates'
  | 'Instruments'
  | 'InstrumentValues';

export type AggregateUpdateNotification = {
  notificationType: 'AggregateUpdated' | 'AggregatesInvalidated';
  kind: AggregateKind | 'All';
  eventID: string | null;
  eventDateTime: string | null;
  auditDateTime: string | null;
  affectedFrom: string | null;
  reason: string;
};

export type BuildProgressNotification = {
  notificationType: 'BuildProgress';
  buildID: string;
  status: 'Running' | 'Succeeded' | 'Failed' | 'Rejected';
  stage: string;
  message: string;
  completedSteps: number;
  totalSteps: number;
  completedEvents: number;
  totalEvents: number;
  startedAtUtc: string;
  updatedAtUtc: string;
  error: string | null;
};

export type AggregateMaintenanceNotification = {
  notificationType: 'AggregateMaintenance';
  status: string;
  trigger: string | null;
  changed: boolean;
  updatedAtUtc: string;
};
