import { env } from '$env/dynamic/private';
import type {
  Accounts,
  AccountReferenceEvent,
  ApiExchangeSearchResponse,
  AssetAllocationMappings,
  Brokers,
  BrokerReferenceEvent,
  Countries,
  CountryReferenceEvent,
  Currencies,
  CurrencyReferenceEvent,
  FIXOperationSearchResponse,
  FXRates,
  FoleoTraderOrders,
  FXRateHistoryEvent,
  FXs,
  Holding,
  Holdings,
  HoldingPositions,
  HoldingKind,
  HoldingReferenceEvent,
  InstrumentLogo,
  InstrumentReferenceEvent,
  InstrumentValues,
  InstrumentValueHistoryEvent,
  Instruments,
  MemoryDiagnostics,
  TicketReferenceEvent,
  TicketDetails,
  Tickets,
  TicketSide,
  TicketStageOption,
  TransactionReferenceEvent,
  BuildProgressNotification,
  UserMenuPreferences,
  UserMenuPreferenceItem,
  Users,
  UserValuationDateOption,
  UserValuationPreferences,
  UserBookmarks,
  UserBookmarkType,
  HoldingDateBasis,
  EventPropertyDetail,
  InstrumentPriceBasis,
  ReportConfigs,
  ReportNodeBase,
  Valuations,
  ValuationSettings,
  ValuationSettingReferenceEvent
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

export type AccountDisplayOrderSetRequest = {
  eventDateTime: string;
  reason: string;
  accountID: string;
  displayOrder: number;
};

export type HoldingCreatedRequest = {
  eventDateTime: string;
  reason: string;
  holdingID?: string;
  accountID: string;
  instrumentID: string;
  holdingKind: HoldingKind;
  name: string;
  active: boolean;
  default: boolean;
  bankName?: string;
  accountName?: string;
  sortCode?: string;
  accountNumber?: string;
  bic?: string;
  iban?: string;
};

export type HoldingModifiedRequest = {
  eventDateTime: string;
  reason: string;
  holdingID: string;
  holdingKind: HoldingKind;
  name: string;
  default: boolean;
  bankName?: string;
  accountName?: string;
  sortCode?: string;
  accountNumber?: string;
  bic?: string;
  iban?: string;
};

export type HoldingActiveModifiedRequest = {
  eventDateTime: string;
  reason: string;
  holdingID: string;
  active: boolean;
};

export type HoldingQueryOptions = {
  holdingID?: string | null;
  accountID?: string | null;
  instrumentID?: string | null;
  holdingKind?: HoldingKind | null;
  includeInactive?: boolean;
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

export type AssetAllocationNodeAccountSettingRequest = {
  accountID: string;
  targetWeight: number;
  targetWeightMax: number;
  targetWeightMin: number;
  targetYield: number;
};

export type AssetAllocationNodeRequest = {
  nodeID: string;
  nodes: string[];
  name: string;
  subtotal: boolean;
  hidden: boolean;
  colour?: string | null;
  accountSettings: AssetAllocationNodeAccountSettingRequest[];
};

export type AssetAllocationCreatedRequest = {
  eventDateTime: string;
  effectiveDateTime: string;
  reason: string;
  assetAllocationID?: string;
  name: string;
  accountIDs: string[];
  active: boolean;
  rootNodeID?: string;
  nodes: AssetAllocationNodeRequest[];
};

export type AssetAllocationModifiedRequest = {
  eventDateTime: string;
  effectiveDateTime: string;
  reason: string;
  assetAllocationID: string;
  name: string;
  rootNodeID: string;
  nodes: AssetAllocationNodeRequest[];
};

export type AssetAllocationAccountIDsSetRequest = {
  eventDateTime: string;
  reason: string;
  assetAllocationID: string;
  accountIDs: string[];
};

export type AssetAllocationActiveSetRequest = {
  eventDateTime: string;
  reason: string;
  assetAllocationID: string;
  active: boolean;
};

export type AssetAllocationMappingSetRequest = {
  eventDateTime: string;
  reason: string;
  assetAllocationID: string;
  holdingID: string;
  nodeID: string;
};

export type ReportNodeRequest = ReportNodeBase;

export type ReportCreatedRequest = {
  eventDateTime: string;
  effectiveDateTime: string;
  reason: string;
  reportID?: string | null;
  name: string;
  active: boolean;
  nodes: ReportNodeRequest[];
};

export type ReportModifiedRequest = {
  eventDateTime: string;
  effectiveDateTime: string;
  reason: string;
  reportID: string;
  name: string;
  active: boolean;
  nodes: ReportNodeRequest[];
};

export type BrokerModifiedRequest = {
  eventDateTime: string;
  reason: string;
  lei: string;
  name: string;
  commission: number;
};

export type BrokerCreatedRequest = BrokerModifiedRequest & {
  active: boolean;
  approvedDateTime: string;
  nextReview: string;
  notes: string;
};

export type BrokerActiveSetRequest = {
  eventDateTime: string;
  reason: string;
  lei: string;
  active: boolean;
};

export type BrokerApprovedDateTimeSetRequest = {
  eventDateTime: string;
  reason: string;
  lei: string;
  approvedDateTime: string;
};

export type BrokerNextReviewSetRequest = {
  eventDateTime: string;
  reason: string;
  lei: string;
  nextReview: string;
};

export type BrokerNotesSetRequest = {
  eventDateTime: string;
  reason: string;
  lei: string;
  notes: string;
};

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
  priceCurrency: string;
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

export type TransactionRequest = {
  holdingID: string;
  instrumentID: string;
  accountID: string;
  quantity: number;
  bookCost: number;
};

export type TransactionSetRequest = {
  userID: string;
  eventDateTime: string;
  settlementDateTime: string;
  reason: string;
  credits: TransactionRequest[];
  debits: TransactionRequest[];
};

export type TransactionCancellationRequest = {
  userID: string;
  reason: string;
  eventSetID: string;
};

export type TransactionSetSubmissionResponse = {
  eventIDs: string[];
  links: {
    rel: string;
    href: string;
    method: string;
  }[];
};

export type TicketCreatedRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  side: TicketSide;
  instrumentID: string;
};

export type TicketAccountRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  ticketNumber: number;
  accountID: string;
};

export type TicketProposalRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  ticketNumber: number;
  targetPrice: number;
  tradeCurrency?: string;
  allocations: {
    accountID: string;
    quantity: number;
  }[];
};

export type TicketApprovalRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  ticketNumber: number;
};

export type TicketTradeApprovalRequest = TicketApprovalRequest;

export type TicketTextSetRequest = TicketApprovalRequest & {
  value: string;
};

export type TicketTradeRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  ticketNumber: number;
  tradedPrice: number;
  tradeDateTime: string;
  settlementDateTime: string;
  allocations: {
    accountID: string;
    cashHoldingID: string;
    quantity: number;
    bookCost: number;
  }[];
};

export type TicketTradeFillRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  ticketNumber: number;
  fillID?: string;
  brokerLEI: string;
  price: number;
  quantity: number;
  bookCost: number;
  note: string;
};

export type TicketTradeFillRemovedRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  ticketNumber: number;
  fillID: string;
};

export type FoleoTraderOrderRequest = {
  userID: string;
  eventDateTime: string;
  ticketNumber: number;
};

export type FoleoTraderOrderSubmissionResponse = {
  eventID: string;
  clOrdID: string;
};

export type TicketCancellationRequest = TicketApprovalRequest;

export type UserMenuPreferencesRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  items: UserMenuPreferenceItem[];
};

export type UserValuationPreferencesRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  valuationDateOption: UserValuationDateOption;
  holdingDateBasis: HoldingDateBasis;
  showZeroBalances: boolean;
};

export type UserCreatedRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  displayName: string;
  displayPreferences: {
    darkMode: boolean;
    rememberTraceDate: boolean;
  };
  valuationPreferences: {
    valuationDate: string;
    showIncome: boolean;
    showBook: boolean;
  };
};

export type UserSessionEventRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
};

export type UserReferenceEvent = {
  $type?: string;
  type?: string;
  Type?: string;
  eventID?: string;
  userID?: string;
};

export type UserBookmarkRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  bookmarkID?: string;
  bookmarkType: UserBookmarkType;
  url: string;
  displayOrder: number;
};

export type UserBookmarkDisplayOrderSetRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  bookmarkID: string;
  displayOrder: number;
};

export type UserBookmarkDeletedRequest = {
  userID: string;
  eventDateTime: string;
  reason: string;
  bookmarkID: string;
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
  includeInactiveOrOptions: boolean | HoldingQueryOptions = true
) {
  const options = typeof includeInactiveOrOptions === 'boolean'
    ? { includeInactive: includeInactiveOrOptions }
    : includeInactiveOrOptions;
  const url = new URL(`${getApiBaseUrl()}/Holdings/`);
  url.searchParams.set('eventDateTime', eventDateTime);
  url.searchParams.set('includeInactive', String(options.includeInactive ?? true));

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);
  if (options.holdingID)
    url.searchParams.set('holdingID', options.holdingID);
  if (options.accountID)
    url.searchParams.set('accountID', options.accountID);
  if (options.instrumentID)
    url.searchParams.set('instrumentID', options.instrumentID);
  if (options.holdingKind)
    url.searchParams.set('holdingKind', options.holdingKind);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  const holdings = (await response.json()) as Holdings;
  return {
    ...holdings,
    items: holdings.items.map((holding) => ({
      ...holding,
      holdingKind: holding.holdingKind ?? holdingKindFromDiscriminator(holding)
    }))
  };
}

export async function getHoldingPositions(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null,
  holdingDateBasis: HoldingDateBasis = 'EventDateTime',
  accountID: string | null = null,
  includeZero = false
) {
  const url = new URL(`${getApiBaseUrl()}/HoldingPositions`);
  url.searchParams.set('eventDateTime', eventDateTime);
  url.searchParams.set('holdingDateBasis', holdingDateBasis);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  if (accountID)
    url.searchParams.set('accountID', accountID);

  if (includeZero)
    url.searchParams.set('includeZero', 'true');

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as HoldingPositions;
}

export async function getAssetAllocationMappings(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null,
  assetAllocationID: string | null = null,
  accountID: string | null = null
) {
  const url = new URL(`${getApiBaseUrl()}/AssetAllocationMappings/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  if (assetAllocationID)
    url.searchParams.set('assetAllocationID', assetAllocationID);

  if (accountID)
    url.searchParams.set('accountID', accountID);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as AssetAllocationMappings;
}

export async function getValuations(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null,
  holdingDateBasis: HoldingDateBasis,
  instrumentPriceBasis: InstrumentPriceBasis,
  valuationCurrency: string,
  accountID: string | null = null
) {
  const url = new URL(`${getApiBaseUrl()}/Valuations/`);
  url.searchParams.set('eventDateTime', eventDateTime);
  url.searchParams.set('holdingDateBasis', holdingDateBasis);
  url.searchParams.set('instrumentPriceBasis', instrumentPriceBasis);
  url.searchParams.set('valuationCurrency', valuationCurrency);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  if (accountID)
    url.searchParams.set('accountID', accountID);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Valuations;
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

export async function getValuationSettings(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/ValuationSettings/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as ValuationSettings;
}

export async function getReportConfigs(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/ReportConfigs/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as ReportConfigs;
}

export async function getBrokers(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/Brokers/`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Brokers;
}

type EventQueryFilters = Record<string, string | number | null | undefined>;

function createApiUrl(path: string, filters?: EventQueryFilters) {
  const url = new URL(`${getApiBaseUrl()}${path}`);

  if (!filters)
    return url;

  for (const [key, value] of Object.entries(filters)) {
    if (value === null || value === undefined || value === '')
      continue;

    url.searchParams.set(key, String(value));
  }

  return url;
}

export async function getCountryEvents(fetchApi: typeof fetch, filters?: EventQueryFilters) {
  const response = await fetchApi(createApiUrl('/Events/Country/', filters));

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as CountryReferenceEvent[];
}

export async function getCurrencyEvents(fetchApi: typeof fetch, filters?: EventQueryFilters) {
  const response = await fetchApi(createApiUrl('/Events/Currency/', filters));

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as CurrencyReferenceEvent[];
}

export async function getValuationSettingEvents(fetchApi: typeof fetch, filters?: EventQueryFilters) {
  const response = await fetchApi(createApiUrl('/Events/ValuationSetting/', filters));

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as ValuationSettingReferenceEvent[];
}

export async function getBrokerEvents(fetchApi: typeof fetch, filters?: EventQueryFilters) {
  const response = await fetchApi(createApiUrl('/Events/Broker/', filters));

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as BrokerReferenceEvent[];
}

export async function getAccountEvents(fetchApi: typeof fetch, filters?: EventQueryFilters) {
  const response = await fetchApi(createApiUrl('/Events/Account/', filters));

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as AccountReferenceEvent[];
}

export async function getHoldingEvents(fetchApi: typeof fetch, filters?: EventQueryFilters) {
  const response = await fetchApi(createApiUrl('/Events/Holding/', filters));

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as HoldingReferenceEvent[];
}

export async function getTransactionEvents(fetchApi: typeof fetch, filters?: string | EventQueryFilters) {
  const url = createApiUrl('/Events/Transaction/');
  const normalizedFilters = typeof filters === 'string' ? { accountID: filters } : filters;

  if (normalizedFilters?.accountID)
    url.searchParams.set('accountID', String(normalizedFilters.accountID));
  if (normalizedFilters?.eventSetID)
    url.searchParams.set('eventSetID', String(normalizedFilters.eventSetID));
  if (normalizedFilters?.holdingID)
    url.searchParams.set('holdingID', String(normalizedFilters.holdingID));
  if (normalizedFilters?.instrumentID)
    url.searchParams.set('instrumentID', String(normalizedFilters.instrumentID));
  if (normalizedFilters?.valuationDateTime)
    url.searchParams.set('valuationDateTime', String(normalizedFilters.valuationDateTime));
  if (normalizedFilters?.auditDateTime)
    url.searchParams.set('auditDateTime', String(normalizedFilters.auditDateTime));

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return ((await response.json()) as Record<string, unknown>[]).map(normalizeTransactionEvent);
}

export async function getInstrumentEvents(fetchApi: typeof fetch, filters?: EventQueryFilters) {
  const response = await fetchApi(createApiUrl('/Events/Instrument/', filters));

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as InstrumentReferenceEvent[];
}

export async function getInstrumentPriceEvents(fetchApi: typeof fetch, filters?: string | EventQueryFilters) {
  const normalizedFilters = typeof filters === 'string' ? { instrumentID: filters } : filters;
  const url = createApiUrl('/Events/InstrumentPrice/', normalizedFilters);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as InstrumentValueHistoryEvent[];
}

export async function getInstrumentIncomeEvents(fetchApi: typeof fetch, filters?: string | EventQueryFilters) {
  const normalizedFilters = typeof filters === 'string' ? { instrumentID: filters } : filters;
  const url = createApiUrl('/Events/InstrumentIncome/', normalizedFilters);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as InstrumentValueHistoryEvent[];
}

export async function getFXRateEvents(fetchApi: typeof fetch, filters?: EventQueryFilters) {
  const response = await fetchApi(createApiUrl('/Events/FXRate/', filters));

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

export async function getTickets(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null,
  includeClosed = false
) {
  const url = new URL(`${getApiBaseUrl()}/Tickets/`);
  url.searchParams.set('eventDateTime', eventDateTime);
  url.searchParams.set('includeClosed', String(includeClosed));

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Tickets;
}

export async function getFoleoTraderOrders(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/Trading/FoleoTrader/Orders`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as FoleoTraderOrders;
}

export async function getTicketDetails(
  fetchApi: typeof fetch,
  eventDateTime: string,
  auditDateTime: string | null,
  includeClosed = false
) {
  const url = new URL(`${getApiBaseUrl()}/Tickets/Details`);
  url.searchParams.set('eventDateTime', eventDateTime);
  url.searchParams.set('includeClosed', String(includeClosed));

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as TicketDetails;
}

export async function getTicketStageOptions(fetchApi: typeof fetch) {
  const response = await fetchApi(`${getApiBaseUrl()}/Tickets/Stages`);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as TicketStageOption[];
}

export async function getUsers(fetchApi: typeof fetch, eventDateTime: string, auditDateTime: string | null) {
  const url = new URL(`${getApiBaseUrl()}/Users`);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as Users;
}

export async function getUserMenuPreferences(
  fetchApi: typeof fetch,
  userID: string,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/UserMenuPreferences/`);
  url.searchParams.set('userID', userID);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as UserMenuPreferences;
}

export async function getUserValuationPreferences(
  fetchApi: typeof fetch,
  userID: string,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/UserValuationPreferences/`);
  url.searchParams.set('userID', userID);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as UserValuationPreferences;
}

export async function getUserBookmarks(
  fetchApi: typeof fetch,
  userID: string,
  eventDateTime: string,
  auditDateTime: string | null
) {
  const url = new URL(`${getApiBaseUrl()}/UserBookmarks/`);
  url.searchParams.set('userID', userID);
  url.searchParams.set('eventDateTime', eventDateTime);

  if (auditDateTime)
    url.searchParams.set('auditDateTime', auditDateTime);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as UserBookmarks;
}

export async function getUserEvents(fetchApi: typeof fetch, userID?: string) {
  const url = new URL(`${getApiBaseUrl()}/Events/User/`);

  if (userID)
    url.searchParams.set('userID', userID);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as UserReferenceEvent[];
}

export async function getTicketEvents(fetchApi: typeof fetch, filters?: number | EventQueryFilters) {
  const normalizedFilters = typeof filters === 'number' ? { ticketNumber: filters } : filters;
  const url = createApiUrl('/Events/Ticket/', normalizedFilters);

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as TicketReferenceEvent[];
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

export type FIXOperationSearchRequest = {
  fromUtc?: string;
  toUtc?: string;
  direction?: string;
  channel?: string;
  msgType?: string;
  clOrdID?: string;
  execID?: string;
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

export async function getFIXOperations(fetchApi: typeof fetch, request: FIXOperationSearchRequest) {
  const url = new URL(`${getApiBaseUrl()}/Diagnostics/FIXTrace`);

  for (const [key, value] of Object.entries(request)) {
    if (value)
      url.searchParams.set(key, value);
  }

  const response = await fetchApi(url);

  if (!response.ok)
    throw new Error(`API returned ${response.status} ${response.statusText}`);

  return (await response.json()) as FIXOperationSearchResponse;
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

export async function postAccountDisplayOrderSetEvent(
  fetchApi: typeof fetch,
  request: AccountDisplayOrderSetRequest,
  userID: string
) {
  return postAccountEvent(fetchApi, 'AccountDisplayOrderSetEvent', request, userID);
}

export async function postHoldingCreatedEvent(
  fetchApi: typeof fetch,
  request: HoldingCreatedRequest,
  userID: string
) {
  return postHoldingEvent(fetchApi, `${holdingEventPrefix(request.holdingKind)}CreatedEvent`, request, userID);
}

export async function postHoldingModifiedEvent(
  fetchApi: typeof fetch,
  request: HoldingModifiedRequest,
  userID: string
) {
  return postHoldingEvent(fetchApi, `${holdingEventPrefix(request.holdingKind)}ModifiedEvent`, request, userID);
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

export async function postAssetAllocationCreatedEvent(
  fetchApi: typeof fetch,
  request: AssetAllocationCreatedRequest,
  userID: string
) {
  return postAssetAllocationEvent(fetchApi, 'AssetAllocationCreatedEvent', request, userID);
}

export async function postAssetAllocationModifiedEvent(
  fetchApi: typeof fetch,
  request: AssetAllocationModifiedRequest,
  userID: string
) {
  return postAssetAllocationEvent(fetchApi, 'AssetAllocationModifiedEvent', request, userID);
}

export async function postAssetAllocationAccountIDsSetEvent(
  fetchApi: typeof fetch,
  request: AssetAllocationAccountIDsSetRequest,
  userID: string
) {
  return postAssetAllocationEvent(fetchApi, 'AssetAllocationAccountIDsSetEvent', request, userID);
}

export async function postAssetAllocationActiveSetEvent(
  fetchApi: typeof fetch,
  request: AssetAllocationActiveSetRequest,
  userID: string
) {
  return postAssetAllocationEvent(fetchApi, 'AssetAllocationActiveSetEvent', request, userID);
}

export async function postAssetAllocationMappingSetEvent(
  fetchApi: typeof fetch,
  request: AssetAllocationMappingSetRequest,
  userID: string
) {
  return postAssetAllocationMappingEvent(fetchApi, request, userID);
}

export async function postReportCreatedEvent(
  fetchApi: typeof fetch,
  request: ReportCreatedRequest,
  userID: string
) {
  return postReportEvent(fetchApi, 'ReportCreatedEvent', request, userID);
}

export async function postReportModifiedEvent(
  fetchApi: typeof fetch,
  request: ReportModifiedRequest,
  userID: string
) {
  return postReportEvent(fetchApi, 'ReportModifiedEvent', request, userID);
}

export async function postBrokerCreatedEvent(
  fetchApi: typeof fetch,
  request: BrokerCreatedRequest,
  userID: string
) {
  return postBrokerEvent(fetchApi, 'BrokerCreatedEvent', request, userID);
}

export async function postBrokerModifiedEvent(
  fetchApi: typeof fetch,
  request: BrokerModifiedRequest,
  userID: string
) {
  return postBrokerEvent(fetchApi, 'BrokerModifiedEvent', request, userID);
}

export async function postBrokerActiveSetEvent(
  fetchApi: typeof fetch,
  request: BrokerActiveSetRequest,
  userID: string
) {
  return postBrokerEvent(fetchApi, 'BrokerActiveSetEvent', request, userID);
}

export async function postBrokerApprovedDateTimeSetEvent(
  fetchApi: typeof fetch,
  request: BrokerApprovedDateTimeSetRequest,
  userID: string
) {
  return postBrokerEvent(fetchApi, 'BrokerApprovedDateTimeSetEvent', request, userID);
}

export async function postBrokerNextReviewSetEvent(
  fetchApi: typeof fetch,
  request: BrokerNextReviewSetRequest,
  userID: string
) {
  return postBrokerEvent(fetchApi, 'BrokerNextReviewSetEvent', request, userID);
}

export async function postBrokerNotesSetEvent(
  fetchApi: typeof fetch,
  request: BrokerNotesSetRequest,
  userID: string
) {
  return postBrokerEvent(fetchApi, 'BrokerNotesSetEvent', request, userID);
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
      PriceCountry: request.priceCountry,
      PriceCurrency: request.priceCurrency
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
      PriceCountry: request.priceCountry,
      PriceCurrency: request.priceCurrency
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

export async function postTransactionSet(fetchApi: typeof fetch, request: TransactionSetRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Transaction/TransactionSet`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      SettlementDateTime: request.settlementDateTime,
      Reason: request.reason,
      Credits: request.credits.map((credit) => ({
        HoldingID: credit.holdingID,
        InstrumentID: credit.instrumentID,
        AccountID: credit.accountID,
        Quantity: credit.quantity,
        BookCost: credit.bookCost
      })),
      Debits: request.debits.map((debit) => ({
        HoldingID: debit.holdingID,
        InstrumentID: debit.instrumentID,
        AccountID: debit.accountID,
        Quantity: debit.quantity,
        BookCost: debit.bookCost
      }))
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as TransactionSetSubmissionResponse;
}

export async function postTransactionCancellation(fetchApi: typeof fetch, request: TransactionCancellationRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/Transaction/Cancel`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      Reason: request.reason,
      EventSetID: request.eventSetID
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as TransactionSetSubmissionResponse;
}

export async function postTicketCreatedEvent(fetchApi: typeof fetch, request: TicketCreatedRequest) {
  return postTicketEvent(fetchApi, 'TicketCreatedEvent', request);
}

export async function postTicketAccountAddedEvent(fetchApi: typeof fetch, request: TicketAccountRequest) {
  return postTicketEvent(fetchApi, 'TicketAccountAddedEvent', request);
}

export async function postTicketAccountRemovedEvent(fetchApi: typeof fetch, request: TicketAccountRequest) {
  return postTicketEvent(fetchApi, 'TicketAccountRemovedEvent', request);
}

export async function postTicketProposalCreatedEvent(fetchApi: typeof fetch, request: TicketProposalRequest) {
  return postTicketEvent(fetchApi, 'TicketProposalCreatedEvent', request);
}

export async function postTicketProposalModifiedEvent(fetchApi: typeof fetch, request: TicketProposalRequest) {
  return postTicketEvent(fetchApi, 'TicketProposalModifiedEvent', request);
}

export async function postTicketProposalDecisionRequestedEvent(fetchApi: typeof fetch, request: TicketApprovalRequest) {
  return postTicketEvent(fetchApi, 'TicketProposalDecisionRequestedEvent', request);
}

export async function postTicketProposalApprovedEvent(fetchApi: typeof fetch, request: TicketApprovalRequest) {
  return postTicketEvent(fetchApi, 'TicketProposalApprovedEvent', request);
}

export async function postTicketProposalNotApprovedEvent(fetchApi: typeof fetch, request: TicketApprovalRequest) {
  return postTicketEvent(fetchApi, 'TicketProposalNotApprovedEvent', request);
}

export async function postTicketProposalReasonSetEvent(fetchApi: typeof fetch, request: TicketTextSetRequest) {
  return postTicketEvent(fetchApi, 'TicketProposalReasonSetEvent', request);
}

export async function postTicketProposalAllocationSetEvent(fetchApi: typeof fetch, request: TicketTextSetRequest) {
  return postTicketEvent(fetchApi, 'TicketProposalAllocationSetEvent', request);
}

export async function postTicketTradeCreatedEvent(fetchApi: typeof fetch, request: TicketTradeRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeCreatedEvent', request);
}

export async function postTicketTradeModifiedEvent(fetchApi: typeof fetch, request: TicketTradeRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeModifiedEvent', request);
}

export async function postTicketTradeFillAddedEvent(fetchApi: typeof fetch, request: TicketTradeFillRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeFillAddedEvent', request);
}

export async function postTicketTradeFillModifiedEvent(fetchApi: typeof fetch, request: TicketTradeFillRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeFillModifiedEvent', request);
}

export async function postTicketTradeFillRemovedEvent(fetchApi: typeof fetch, request: TicketTradeFillRemovedRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeFillRemovedEvent', request);
}

export async function postTicketTradeDecisionRequestedEvent(fetchApi: typeof fetch, request: TicketApprovalRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeDecisionRequestedEvent', request);
}

export async function postTicketTradeApprovedEvent(fetchApi: typeof fetch, request: TicketTradeApprovalRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeApprovedEvent', request);
}

export async function postTicketTradeNotApprovedEvent(fetchApi: typeof fetch, request: TicketApprovalRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeNotApprovedEvent', request);
}

export async function postFoleoTraderOrder(fetchApi: typeof fetch, request: FoleoTraderOrderRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Trading/FoleoTrader/Orders`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      TicketNumber: request.ticketNumber
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as FoleoTraderOrderSubmissionResponse;
}

export async function postTicketTradeInstructionNotesSetEvent(fetchApi: typeof fetch, request: TicketTextSetRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeInstructionNotesSetEvent', request);
}

export async function postTicketTradeProgressNotesSetEvent(fetchApi: typeof fetch, request: TicketTextSetRequest) {
  return postTicketEvent(fetchApi, 'TicketTradeProgressNotesSetEvent', request);
}

export async function postTicketCancelledEvent(fetchApi: typeof fetch, request: TicketCancellationRequest) {
  return postTicketEvent(fetchApi, 'TicketCancelledEvent', request);
}

export async function postUserMenuPreferencesCreatedEvent(fetchApi: typeof fetch, request: UserMenuPreferencesRequest) {
  return postUserMenuPreferencesEvent(fetchApi, 'UserMenuPreferencesCreatedEvent', request);
}

export async function postUserMenuPreferencesModifiedEvent(fetchApi: typeof fetch, request: UserMenuPreferencesRequest) {
  return postUserMenuPreferencesEvent(fetchApi, 'UserMenuPreferencesModifiedEvent', request);
}

export async function postUserValuationPreferencesCreatedEvent(fetchApi: typeof fetch, request: UserValuationPreferencesRequest) {
  return postUserValuationPreferencesEvent(fetchApi, 'UserValuationPreferencesCreatedEvent', request);
}

export async function postUserValuationPreferencesModifiedEvent(fetchApi: typeof fetch, request: UserValuationPreferencesRequest) {
  return postUserValuationPreferencesEvent(fetchApi, 'UserValuationPreferencesModifiedEvent', request);
}

export async function postUserCreatedEvent(fetchApi: typeof fetch, request: UserCreatedRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/User/UserCreatedEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      DisplayName: request.displayName,
      DisplayPreferences: {
        DarkMode: request.displayPreferences.darkMode,
        RememberTraceDate: request.displayPreferences.rememberTraceDate
      },
      ValuationPreferences: {
        ValuationDate: request.valuationPreferences.valuationDate,
        ShowIncome: request.valuationPreferences.showIncome,
        ShowBook: request.valuationPreferences.showBook
      }
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postUserSignedInEvent(fetchApi: typeof fetch, request: UserSessionEventRequest) {
  return postUserSessionEvent(fetchApi, 'UserSignedInEvent', request);
}

export async function postUserSignedOutEvent(fetchApi: typeof fetch, request: UserSessionEventRequest) {
  return postUserSessionEvent(fetchApi, 'UserSignedOutEvent', request);
}

export async function postUserBookmarkCreatedEvent(fetchApi: typeof fetch, request: UserBookmarkRequest) {
  return postUserBookmarkEvent(fetchApi, 'UserBookmarkCreatedEvent', request);
}

export async function postUserBookmarkModifiedEvent(fetchApi: typeof fetch, request: UserBookmarkRequest) {
  return postUserBookmarkEvent(fetchApi, 'UserBookmarkModifiedEvent', request);
}

export async function postUserBookmarkDisplayOrderSetEvent(fetchApi: typeof fetch, request: UserBookmarkDisplayOrderSetRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/UserBookmarks/UserBookmarkDisplayOrderSetEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      BookmarkID: request.bookmarkID,
      DisplayOrder: request.displayOrder
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

export async function postUserBookmarkDeletedEvent(fetchApi: typeof fetch, request: UserBookmarkDeletedRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/UserBookmarks/UserBookmarkDeletedEvent`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      BookmarkID: request.bookmarkID
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

async function postTicketEvent(fetchApi: typeof fetch, eventType: string, request: Record<string, unknown>) {
  const body: Record<string, unknown> = {
    UserID: request.userID,
    EventDateTime: request.eventDateTime,
    Reason: request.reason
  };

  if (typeof request.ticketNumber === 'number')
    body.TicketNumber = request.ticketNumber;
  if (request.side)
    body.Side = request.side;
  if (request.instrumentID)
    body.InstrumentID = request.instrumentID;
  if (request.accountID)
    body.AccountID = request.accountID;
  if (typeof request.targetPrice === 'number')
    body.TargetPrice = request.targetPrice;
  if (typeof request.tradeCurrency === 'string')
    body.TradeCurrency = request.tradeCurrency;
  if (typeof request.tradedPrice === 'number')
    body.TradedPrice = request.tradedPrice;
  if (typeof request.tradeDateTime === 'string')
    body.TradeDateTime = request.tradeDateTime;
  if (typeof request.settlementDateTime === 'string')
    body.SettlementDateTime = request.settlementDateTime;
  if (Array.isArray(request.allocations))
    body.Allocations = (request.allocations as { accountID: string; cashHoldingID?: string; quantity: number; bookCost?: number }[]).map((allocation) => ({
      AccountID: allocation.accountID,
      CashHoldingID: allocation.cashHoldingID,
      Quantity: allocation.quantity,
      BookCost: allocation.bookCost
    }));
  if (request.fillID)
    body.FillID = request.fillID;
  if (typeof request.brokerLEI === 'string')
    body.BrokerLEI = request.brokerLEI;
  if (typeof request.price === 'number')
    body.Price = request.price;
  if (typeof request.quantity === 'number')
    body.Quantity = request.quantity;
  if (typeof request.bookCost === 'number')
    body.BookCost = request.bookCost;
  if (typeof request.note === 'string')
    body.Note = request.note;
  if (typeof request.value === 'string')
    body.Value = request.value;

  const response = await fetchApi(`${getApiBaseUrl()}/Events/Ticket/${eventType}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

async function postUserBookmarkEvent(fetchApi: typeof fetch, eventType: 'UserBookmarkCreatedEvent' | 'UserBookmarkModifiedEvent', request: UserBookmarkRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/UserBookmarks/${eventType}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      BookmarkID: request.bookmarkID,
      BookmarkType: request.bookmarkType,
      Url: request.url,
      DisplayOrder: request.displayOrder
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

async function postUserSessionEvent(fetchApi: typeof fetch, eventType: 'UserSignedInEvent' | 'UserSignedOutEvent', request: UserSessionEventRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/User/${eventType}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

async function postUserMenuPreferencesEvent(fetchApi: typeof fetch, eventType: 'UserMenuPreferencesCreatedEvent' | 'UserMenuPreferencesModifiedEvent', request: UserMenuPreferencesRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/UserMenuPreferences/${eventType}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      Items: request.items.map((item) => ({
        MenuItemID: item.menuItemID,
        Visible: item.visible
      }))
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

async function postUserValuationPreferencesEvent(fetchApi: typeof fetch, eventType: 'UserValuationPreferencesCreatedEvent' | 'UserValuationPreferencesModifiedEvent', request: UserValuationPreferencesRequest) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/UserValuationPreferences/${eventType}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      UserID: request.userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      ValuationDateOption: request.valuationDateOption,
      HoldingDateBasis: request.holdingDateBasis,
      ShowZeroBalances: request.showZeroBalances
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
  eventType: 'AccountCreatedEvent' | 'AccountModifiedEvent' | 'AccountActiveModifiedEvent' | 'AccountDisplayOrderSetEvent',
  request: AccountCreatedRequest | AccountModifiedRequest | AccountActiveModifiedRequest | AccountDisplayOrderSetRequest,
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

  if ('displayOrder' in request)
    body.DisplayOrder = request.displayOrder;

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
  eventType: string,
  request: HoldingCreatedRequest | HoldingModifiedRequest | HoldingActiveModifiedRequest,
  userID: string
) {
  const body: Record<string, unknown> = {
    UserID: userID,
    EventDateTime: request.eventDateTime,
    Reason: request.reason,
    HoldingID: request.holdingID
  };

  if ('accountID' in request) {
    const created = request as HoldingCreatedRequest;
    if (!created.holdingID)
      delete body.HoldingID;
    body.AccountID = created.accountID;
    body.InstrumentID = created.instrumentID;
    body.Name = created.name;
    body.Active = created.active;
    body.Default = created.default;
    addBankFields(body, created);
  } else if ('name' in request) {
    const modified = request as HoldingModifiedRequest;
    body.Name = modified.name;
    body.Default = modified.default;
    addBankFields(body, modified);
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

function holdingEventPrefix(holdingKind: HoldingKind) {
  switch (holdingKind) {
    case 'PositionMemo':
      return 'HoldingPositionMemo';
    case 'PositionCash':
      return 'HoldingPositionCash';
    case 'PositionAsset':
      return 'HoldingPositionAsset';
    case 'CashDebt':
      return 'HoldingCashDebt';
    case 'CashInvestable':
      return 'HoldingCashInvestable';
    case 'CashNonInvestable':
      return 'HoldingCashNonInvestable';
    case 'Inflow':
      return 'HoldingInflow';
    case 'Outflow':
      return 'HoldingOutflow';
    case 'InSpecieIn':
      return 'HoldingInSpecieIn';
    case 'InSpecieOut':
      return 'HoldingInSpecieOut';
    case 'FeesCustodian':
      return 'HoldingFeesCustodian';
    case 'FeesAdministrator':
      return 'HoldingFeesAdministrator';
    case 'FeesBank':
      return 'HoldingFeesBank';
    case 'Income':
      return 'HoldingIncome';
    case 'Interest':
      return 'HoldingInterest';
  }
}

function holdingKindFromDiscriminator(holding: Holding): HoldingKind {
  const type = (holding as Holding & { $type?: string }).$type ?? '';
  const kind = type.startsWith('Holding') ? type.slice('Holding'.length) : type;

  if (isHoldingKind(kind))
    return kind;

  throw new Error(`Unknown holding discriminator '${type || '(missing)'}'.`);
}

function isHoldingKind(value: string): value is HoldingKind {
  return [
    'PositionMemo',
    'PositionCash',
    'PositionAsset',
    'CashDebt',
    'CashInvestable',
    'CashNonInvestable',
    'Inflow',
    'Outflow',
    'InSpecieIn',
    'InSpecieOut',
    'FeesCustodian',
    'FeesAdministrator',
    'FeesBank',
    'Income',
    'Interest'
  ].includes(value);
}

function isBankHoldingKind(holdingKind: HoldingKind) {
  return holdingKind === 'CashDebt' || holdingKind === 'CashInvestable' || holdingKind === 'CashNonInvestable';
}

function normalizeTransactionEvent(event: Record<string, unknown>): TransactionReferenceEvent {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    bookCost: readOptionalNumber(event, 'bookCost', 'BookCost'),
    cancelledEventID: readString(event, 'cancelledEventID', 'cancelledEventId', 'CancelledEventID') || undefined,
    cancelledIDGroup: readStringArray(event, 'cancelledIDGroup', 'cancelledIdGroup', 'CancelledIDGroup'),
    classDescription: readString(event, 'classDescription', 'ClassDescription') || undefined,
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID'),
    eventIDGroup: readStringArray(event, 'eventIDGroup', 'eventIdGroup', 'EventIDGroup'),
    eventSetID: readString(event, 'eventSetID', 'eventSetId', 'EventSetID'),
    holdingID: readString(event, 'holdingID', 'holdingId', 'HoldingID') || undefined,
    accountID: readString(event, 'accountID', 'accountId', 'AccountID') || undefined,
    instrumentID: readString(event, 'instrumentID', 'instrumentId', 'InstrumentID') || undefined,
    quantity: readOptionalNumber(event, 'quantity', 'Quantity'),
    reason: readString(event, 'reason', 'Reason'),
    settlementDateTime: readString(event, 'settlementDateTime', 'SettlementDateTime'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    propertyDetails: readEventPropertyDetails(event)
  };
}

export function readEventPropertyDetails(source: Record<string, unknown>): EventPropertyDetail[] | undefined {
  const details = source.propertyDetails ?? source.PropertyDetails;

  if (!Array.isArray(details))
    return undefined;

  return details
    .map((detail) => {
      if (!detail || typeof detail !== 'object')
        return null;

      const record = detail as Record<string, unknown>;
      const name = record.name ?? record.Name;
      const description = record.description ?? record.Description;
      const order = record.order ?? record.Order;
      const value = Object.prototype.hasOwnProperty.call(record, 'value')
        ? record.value
        : record.Value;

      if (typeof name !== 'string' || typeof description !== 'string')
        return null;

      const propertyDetail: EventPropertyDetail = {
        name,
        description,
        value
      };

      if (typeof order === 'number')
        propertyDetail.order = order;

      return propertyDetail;
    })
    .filter((detail): detail is EventPropertyDetail => detail !== null);
}

export function readApplicationStatus(source: Record<string, unknown>) {
  const value = source.applicationStatus ?? source.ApplicationStatus;
  return value === 'omitted' ? 'omitted' as const : 'applied' as const;
}

function readString(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'string')
      return value;
  }

  return '';
}

function readOptionalNumber(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'number')
      return value;
  }

  return undefined;
}

function readStringArray(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (Array.isArray(value))
      return value.filter((item): item is string => typeof item === 'string');
  }

  return [];
}

function addBankFields(body: Record<string, unknown>, request: HoldingCreatedRequest | HoldingModifiedRequest) {
  if (!isBankHoldingKind(request.holdingKind))
    return;

  body.BankName = request.bankName ?? '';
  body.AccountName = request.accountName ?? '';
  body.SortCode = request.sortCode ?? '';
  body.AccountNumber = request.accountNumber ?? '';
  body.BIC = request.bic ?? '';
  body.IBAN = request.iban ?? '';
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

async function postAssetAllocationEvent(
  fetchApi: typeof fetch,
  eventType:
    | 'AssetAllocationCreatedEvent'
    | 'AssetAllocationModifiedEvent'
    | 'AssetAllocationAccountIDsSetEvent'
    | 'AssetAllocationActiveSetEvent',
  request:
    | AssetAllocationCreatedRequest
    | AssetAllocationModifiedRequest
    | AssetAllocationAccountIDsSetRequest
    | AssetAllocationActiveSetRequest,
  userID: string
) {
  const body: Record<string, unknown> = {
    UserID: userID,
    EventDateTime: request.eventDateTime,
    Reason: request.reason,
    AssetAllocationID: request.assetAllocationID
  };

  if ('name' in request)
    body.Name = request.name;

  if ('effectiveDateTime' in request)
    body.EffectiveDateTime = request.effectiveDateTime;

  if ('accountIDs' in request)
    body.AccountIDs = request.accountIDs;

  if ('active' in request)
    body.Active = request.active;

  if ('rootNodeID' in request)
    body.RootNodeID = request.rootNodeID;

  if ('nodes' in request)
    body.Nodes = request.nodes.map((node) => ({
      NodeID: node.nodeID,
      Nodes: node.nodes,
      Name: node.name,
      Subtotal: node.subtotal,
      Hidden: node.hidden,
      Colour: node.colour,
      AccountSettings: node.accountSettings.map((setting) => ({
        AccountID: setting.accountID,
        TargetWeight: setting.targetWeight,
        TargetWeightMax: setting.targetWeightMax,
        TargetWeightMin: setting.targetWeightMin,
        TargetYield: setting.targetYield
      }))
    }));

  const response = await fetchApi(`${getApiBaseUrl()}/Events/ValuationSetting/${eventType}`, {
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

async function postAssetAllocationMappingEvent(
  fetchApi: typeof fetch,
  request: AssetAllocationMappingSetRequest,
  userID: string
) {
  const response = await fetchApi(`${getApiBaseUrl()}/Events/AssetAllocationMapping/AssetAllocationMappingSetEvent`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json'
    },
    body: JSON.stringify({
      UserID: userID,
      EventDateTime: request.eventDateTime,
      Reason: request.reason,
      AssetAllocationID: request.assetAllocationID,
      HoldingID: request.holdingID,
      NodeID: request.nodeID
    })
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(readApiError(errorText) || `API returned ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as EventSubmissionResponse;
}

async function postReportEvent(
  fetchApi: typeof fetch,
  eventType: 'ReportCreatedEvent' | 'ReportModifiedEvent',
  request: ReportCreatedRequest | ReportModifiedRequest,
  userID: string
) {
  const body: Record<string, unknown> = {
    UserID: userID,
    EventDateTime: request.eventDateTime,
    EffectiveDateTime: request.effectiveDateTime,
    Reason: request.reason,
    Name: request.name,
    Active: request.active,
    Nodes: request.nodes.map(toReportNodeBody)
  };

  if (request.reportID)
    body.ReportID = request.reportID;

  const response = await fetchApi(`${getApiBaseUrl()}/Events/Report/${eventType}`, {
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

function toReportNodeBody(node: ReportNodeRequest) {
  const type = node.type ?? node.$type ?? 'ReportNodeCoverPage';
  const body: Record<string, unknown> = {
    $type: type,
    ReportNodeID: node.reportNodeID,
    DisplayOrder: node.displayOrder,
    Name: node.name,
    Title: node.title
  };

  if (node.assetAllocationID)
    body.AssetAllocationID = node.assetAllocationID;

  if (type === 'ReportNodeChart')
    body.ChartType = node.chartType ?? 'Pie';

  if (type === 'ReportNodeValuation' && node.columns !== undefined)
    body.Columns = node.columns?.map((column, index) => ({
      ColumnKey: column.columnKey,
      DisplayOrder: index + 1
    })) ?? null;

  return body;
}

async function postBrokerEvent(
  fetchApi: typeof fetch,
  eventType:
    | 'BrokerCreatedEvent'
    | 'BrokerModifiedEvent'
    | 'BrokerActiveSetEvent'
    | 'BrokerApprovedDateTimeSetEvent'
    | 'BrokerNextReviewSetEvent'
    | 'BrokerNotesSetEvent',
  request:
    | BrokerCreatedRequest
    | BrokerModifiedRequest
    | BrokerActiveSetRequest
    | BrokerApprovedDateTimeSetRequest
    | BrokerNextReviewSetRequest
    | BrokerNotesSetRequest,
  userID: string
) {
  const body: Record<string, unknown> = {
    UserID: userID,
    EventDateTime: request.eventDateTime,
    Reason: request.reason,
    LEI: request.lei
  };

  if ('name' in request)
    body.Name = request.name;

  if ('commission' in request)
    body.Commission = request.commission;

  if ('active' in request)
    body.Active = request.active;

  if ('approvedDateTime' in request)
    body.ApprovedDateTime = request.approvedDateTime;

  if ('nextReview' in request)
    body.NextReview = request.nextReview;

  if ('notes' in request)
    body.Notes = request.notes;

  const response = await fetchApi(`${getApiBaseUrl()}/Events/Broker/${eventType}`, {
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
