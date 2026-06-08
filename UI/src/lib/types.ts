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

export type Account = {
  accountID: string;
  name: string;
  formalName: string;
  bookCurrency: string;
  active: boolean;
  displayOrder: number;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type Accounts = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: Account[];
};

export type UserDisplayPreferences = {
  darkMode: boolean;
  rememberTraceDate: boolean;
};

export type UserProfileValuationPreferences = {
  valuationDate: string;
  showIncome: boolean;
  showBook: boolean;
};

export type User = {
  userID: string;
  displayName: string;
  displayPreferences: UserDisplayPreferences;
  valuationPreferences: UserProfileValuationPreferences;
  lastSignedIn: string | null;
  lastSignedOut: string | null;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type Users = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: User[];
};

export type UserMenuPreferenceItem = {
  menuItemID: string;
  visible: boolean;
};

export type UserMenuPreferences = {
  userID: string;
  items: UserMenuPreferenceItem[];
  hasStoredPreferences: boolean;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type UserValuationDateOption =
  | 'Now'
  | 'TodayEndOfDay'
  | 'YesterdayEndOfDay'
  | 'LastWeekEndOfDay'
  | 'LastMonthEndOfDay'
  | 'LastQuarterEndOfDay';

export type UserValuationPreferences = {
  userID: string;
  valuationDateOption: UserValuationDateOption;
  holdingDateBasis: HoldingDateBasis;
  showZeroBalances: boolean;
  hasStoredPreferences: boolean;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type UserBookmarkType = 'Base' | 'Query';

export type UserBookmarkItem = {
  bookmarkID: string;
  bookmarkType: UserBookmarkType;
  url: string;
  displayOrder: number;
};

export type UserBookmarks = {
  userID: string;
  items: UserBookmarkItem[];
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type HoldingKind =
  | 'CashDebt'
  | 'CashInvestable'
  | 'CashNonInvestable'
  | 'PositionMemo'
  | 'PositionCash'
  | 'PositionAsset'
  | 'Inflow'
  | 'Outflow'
  | 'InSpecieIn'
  | 'InSpecieOut'
  | 'FeesCustodian'
  | 'FeesAdministrator'
  | 'FeesBank'
  | 'Income'
  | 'Interest';

export type Holding = {
  holdingID: string;
  accountID: string;
  instrumentID: string;
  holdingKind: HoldingKind;
  name: string;
  active: boolean;
  default: boolean;
  bankName?: string | null;
  accountName?: string | null;
  sortCode?: string | null;
  accountNumber?: string | null;
  bic?: string | null;
  iban?: string | null;
  includeInValuation: boolean;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type Holdings = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: Holding[];
};

export type HoldingPosition = Holding & {
  accountName: string;
  instrumentName: string;
  quantity: number;
  bookCost: number;
  holdingDateBasis: HoldingDateBasis;
};

export type HoldingDateBasis = 'EventDateTime' | 'SettlementDateTime';

export type InstrumentPriceBasis = 'Bid' | 'Ask' | 'Mid' | 'NAV';

export type HoldingPositions = {
  valuationDateTime: string;
  holdingDateBasis: HoldingDateBasis;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: HoldingPosition[];
};

export type ValuationTotals = {
  bookValue: number;
  bookCost: number;
  incompleteCount: number;
};

export type ValuationItem = {
  accountID: string;
  accountName: string;
  holdingID: string;
  holdingName: string;
  holdingKind: HoldingKind | string;
  instrumentID: string;
  instrumentName: string;
  name: string;
  priceCurrency: string;
  valuationCurrency: string;
  fxPair?: string | null;
  fxDisplayPair?: string | null;
  fxRate?: number | null;
  quantity: number;
  localPrice?: number | null;
  quotePrice?: number | null;
  bookValue?: number | null;
  bookCost: number;
  complete: boolean;
  incompleteReason?: string | null;
};

export type AccountValuation = {
  accountID: string;
  accountName: string;
  bookCurrency: string;
  valuationCurrency: string;
  items: ValuationItem[];
  totals: ValuationTotals;
};

export type Valuations = {
  valuationDateTime: string;
  asOfDateTime: string;
  holdingDateBasis: HoldingDateBasis;
  instrumentPriceBasis: InstrumentPriceBasis;
  valuationCurrency: string;
  accountID?: string | null;
  lastEventID: string;
  lastAuditDateTime: string;
  accounts: AccountValuation[];
  totals: ValuationTotals;
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

export type AssetAllocationNodeAccountSetting = {
  accountID: string;
  targetWeight: number;
  targetWeightMax: number;
  targetWeightMin: number;
  targetYield: number;
};

export type AssetAllocationNode = {
  nodeID: string;
  nodes: string[];
  name: string;
  subtotal: boolean;
  hidden: boolean;
  accountSettings: AssetAllocationNodeAccountSetting[];
};

export type ValuationSetting = {
  assetAllocationID: string;
  name: string;
  accountIDs: string[];
  active: boolean;
  rootNodeID: string;
  nodes: AssetAllocationNode[];
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type ValuationSettings = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: ValuationSetting[];
};

export type Broker = {
  name: string;
  lei: string;
  commission: number;
  active: boolean;
  approvedDateTime: string;
  nextReview: string;
  notes: string;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
};

export type Brokers = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: Broker[];
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

export type InstrumentPrice = {
  amount?: number | null;
};

export type InstrumentPriceEquity = {
  $type?: 'InstrumentPriceEquity';
  bid: InstrumentPrice;
  mid: InstrumentPrice;
  ask: InstrumentPrice;
  nav: InstrumentPrice;
  priceType?: string;
};

export type ValuationPrice = {
  amount?: number | null;
};

export type InstrumentPriceFixedIncome = {
  $type?: 'InstrumentPriceFixedIncome';
  cleanPrice: ValuationPrice;
  priceType?: string;
};

export type InstrumentPriceCash = {
  $type?: 'InstrumentPriceCash';
  price: InstrumentPrice;
  priceType?: string;
};

export type InstrumentIncomeEquity = {
  $type?: 'InstrumentIncomeEquity';
  dividendAmount: InstrumentPrice;
  dividendType: string;
  exDividend: string | null;
  declaration: string | null;
  record: string | null;
  payable: string | null;
  incomeType?: string;
};

export type InstrumentIncomeFixedIncome = {
  $type?: 'InstrumentIncomeFixedIncome';
  accruedInterest: ValuationPrice;
  incomeType?: string;
};

export type InstrumentIncomeCash = {
  $type?: 'InstrumentIncomeCash';
  income: {
    value?: number | null;
  };
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
  priceCurrency: string;
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
  price?: InstrumentPriceEquity | InstrumentPriceFixedIncome | InstrumentPriceCash | null;
  priceValuationDateTime?: string | null;
  income?: InstrumentIncomeEquity | InstrumentIncomeFixedIncome | InstrumentIncomeCash | null;
};

export type InstrumentValues = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: InstrumentValue[];
};

export type TicketSide = 'Buy' | 'Sell';

export type TicketStage = 'Proposal' | 'Trade' | 'Completed' | 'Cancelled';

export type TicketDecision = 'InProgress' | 'PendingApproval' | 'Approved' | 'NotApproved';

export type TicketStageOption = {
  stage: TicketStage;
  description: string;
};

export type TicketProposalAllocation = {
  accountID: string;
  quantity: number;
};

export type TicketTradeAllocation = {
  accountID: string;
  cashHoldingID?: string | null;
  quantity: number;
  bookCost: number;
};

export type TicketFill = {
  fillID: string;
  brokerLEI: string;
  price: number;
  quantity: number;
  bookCost: number;
  note: string;
};

export type Ticket = {
  ticketNumber: number;
  side: TicketSide;
  instrumentID: string;
  tradeCurrency: string;
  stage: TicketStage;
  proposalDecision: TicketDecision;
  tradeDecision: TicketDecision;
  accountIDs: string[];
  proposalTargetPrice?: number | null;
  proposalTotalAmount?: number | null;
  proposalAllocations: TicketProposalAllocation[];
  proposalReason: string;
  proposalAllocation: string;
  tradePrice?: number | null;
  tradeAllocations: TicketTradeAllocation[];
  fills: TicketFill[];
  tradeInstructionNotes: string;
  tradeProgressNotes: string;
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  isActive: boolean;
};

export type Tickets = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: Ticket[];
};

export type FoleoTraderOrderStatus = 'Submitted' | 'PartiallyFilled' | 'Filled' | 'Rejected' | 'Failed';

export type FoleoTraderOrder = {
  ticketNumber: number;
  clOrdID: string;
  status: FoleoTraderOrderStatus;
  orderQuantity: number;
  filledQuantity: number;
  price: number;
  currency: string;
  side: TicketSide;
  securityID: string;
  securityIDSource: string;
  symbol: string;
  lastExecID?: string | null;
  lastError?: string | null;
  submittedAt: string;
  updatedAt: string;
};

export type FoleoTraderOrders = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: FoleoTraderOrder[];
};

export type TicketDetail = Ticket & {
  instrument: Instrument | null;
  accounts: Account[];
};

export type TicketDetails = {
  valuationDateTime: string;
  asOfDateTime: string;
  lastEventID: string;
  lastAuditDateTime: string;
  items: TicketDetail[];
};

export type ReferenceEventBase = {
  $type: string;
  eventID: string;
  userID: string;
  eventDateTime: string;
  auditDateTime: string;
  reason: string;
  applicationStatus?: 'applied' | 'omitted';
  propertyDetails?: EventPropertyDetail[];
};

export type EventPropertyDetail = {
  name: string;
  description: string;
  order?: number;
  value: unknown;
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

export type ValuationSettingReferenceEvent = ReferenceEventBase & {
  assetAllocationID: string;
  name?: string;
  accountIDs?: string[];
  active?: boolean;
  rootNodeID?: string;
  nodes?: AssetAllocationNode[];
};

export type BrokerReferenceEvent = ReferenceEventBase & {
  lei: string;
  name?: string;
  commission?: number;
  active?: boolean;
  approvedDateTime?: string;
  nextReview?: string;
  notes?: string;
};

export type AccountReferenceEvent = ReferenceEventBase & {
  accountID: string;
  name?: string;
  formalName?: string;
  bookCurrency?: string;
  active?: boolean;
  displayOrder?: number;
};

export type HoldingReferenceEvent = ReferenceEventBase & {
  holdingID: string;
  accountID?: string;
  instrumentID?: string;
  holdingKind?: HoldingKind;
  name?: string;
  active?: boolean;
  default?: boolean;
  bankName?: string | null;
  accountName?: string | null;
  sortCode?: string | null;
  accountNumber?: string | null;
};

export type TransactionReferenceEvent = ReferenceEventBase & {
  eventSetID: string;
  eventIDGroup: string[];
  settlementDateTime: string;
  holdingID?: string;
  accountID?: string;
  instrumentID?: string;
  quantity?: number;
  bookCost?: number;
  cancelledEventID?: string;
  cancelledIDGroup?: string[];
};

export type HoldingHistoryEvent = HoldingReferenceEvent | TransactionReferenceEvent;

export type ValuationHistoryEvent = HoldingHistoryEvent & {
  exclusionKind: 'valuationDate' | 'auditDate';
  exclusionReason: string;
};

export type TicketReferenceEvent = ReferenceEventBase & {
  ticketNumber: number;
  details?: Record<string, unknown>;
};

export type InstrumentReferenceEvent = ReferenceEventBase & {
  instrumentID: string;
  name?: string;
  formalName?: string;
  exchange?: string;
  cfi?: string;
  logo?: InstrumentLogo | null;
  active?: boolean;
  incomeCountry?: string;
  priceCountry?: string;
  priceCurrency?: string;
  identifier?: InstrumentIdentifier | null;
  identifierType?: string | number;
  terms?: unknown;
};

export type InstrumentValueHistoryEvent = ReferenceEventBase & {
  instrumentID: string;
  valueKind: 'Price' | 'Income';
  summary: string;
};

export type FXRateHistoryEvent = ReferenceEventBase & {
  pair: string;
  displayPair: string;
  summary: string;
};

export type MemoryDiagnostics = {
  eventCache: {
    isLoaded: boolean;
    streamCount: number;
    eventCount: number;
    estimatedMemoryBytes: number;
    unprocessedEventCount: number;
    recentUnprocessedEvents: {
      streamId: string | null;
      eventId: string | null;
      eventType: string;
      reason: string;
      recordedAtUtc: string;
    }[];
  };
  accountService?: {
    cacheEntryCount: number;
    accountCount: number;
    estimatedMemoryBytes: number;
  };
  brokerService?: {
    cacheEntryCount: number;
    brokerCount: number;
    estimatedMemoryBytes: number;
  };
  countryService: {
    cacheEntryCount: number;
    countryCount: number;
    estimatedMemoryBytes: number;
  };
  currencyService: {
    cacheEntryCount: number;
    currencyCount: number;
    estimatedMemoryBytes: number;
  };
  fxService: {
    cacheEntryCount: number;
    fxCount: number;
    estimatedMemoryBytes: number;
  };
  fxRateService: {
    cacheEntryCount: number;
    fxRateCount: number;
    estimatedMemoryBytes: number;
  };
  holdingService?: {
    cacheEntryCount: number;
    holdingCount: number;
    estimatedMemoryBytes: number;
  };
  holdingPositionService?: {
    cacheEntryCount: number;
    positionCount: number;
    estimatedMemoryBytes: number;
  };
  instrumentService?: {
    cacheEntryCount: number;
    instrumentCount: number;
    estimatedMemoryBytes: number;
  };
  instrumentValueService?: {
    cacheEntryCount: number;
    instrumentValueCount: number;
    estimatedMemoryBytes: number;
  };
  userService?: {
    cacheEntryCount: number;
    userCount: number;
    estimatedMemoryBytes: number;
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
  | 'Accounts'
  | 'Brokers'
  | 'Countries'
  | 'Currencies'
  | 'FXs'
  | 'FXRates'
  | 'FoleoTraderOrders'
  | 'Holdings'
  | 'HoldingPositions'
  | 'Instruments'
  | 'InstrumentValues'
  | 'Tickets'
  | 'Users'
  | 'UserBookmarks'
  | 'UserMenuPreferences'
  | 'UserValuationPreferences'
  | 'ValuationSettings';

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
