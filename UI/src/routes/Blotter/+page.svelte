<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import { BrokerDropdown, ComplexSelect, Field, TicketDropdown, type ComplexSelectOption } from '$lib/components/forms';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { dateForInput, dateTimeForInput, formatDisplayDateTime, formatShortDate, formatTableDateTime, nextWorkingDayDateForInput, nowForInput, toApiDateTime } from '$lib/dates';
  import type { Account, Broker, FoleoTraderOrder, Holding, Instrument, InstrumentPriceCash, InstrumentPriceEquity, InstrumentPriceFixedIncome, InstrumentValue, Ticket, TicketReferenceEvent, TicketSide, TicketStage, TradeFileStatus } from '$lib/types';
  import type { SubmitFunction } from './$types';

  type TicketEditContext = 'Proposal' | 'Trade';

  let { data, form } = $props();

  const eventDateDefault = $derived(data.valuationDate);
  let submitting = $state('');
  let createTicketSide = $state<'' | 'Buy' | 'Sell'>('');
  let createTicketInstrument = $state('');
  let selectedSides = $state<TicketSide[]>([]);
  let selectedStages = $state<TicketStage[]>([]);
  let selectedEstimatedBookCosts = $state(false);
  let fixBrokerByTicket = $state<Record<number, string>>({});
  let tradeFileBrokerByTicket = $state<Record<number, string>>({});
  let selectedTradeFileBatchBrokerLEI = $state('');
  let selectedPendingTicketNumbers = $state<number[]>([]);
  let freeTextFilter = $state('');
  let ticketViewMode = $state<'Detailed' | 'Compact'>('Compact');
  let expandedTicketNumbers = $state<number[]>([]);
  let collapsedTicketNumbers = $state<number[]>([]);
  let editingTicket = $state<{ ticketNumber: number; context: TicketEditContext | '' }>({ ticketNumber: 0, context: '' });
  let cancelConfirmingTicketNumber = $state(0);
  let cancelConfirmationInput = $state('');
  let proposalQuantityDrafts = $state<Record<string, string>>({});
  let tradeQuantityDrafts = $state<Record<string, string>>({});
  let tradeSettlementAmountDrafts = $state<Record<string, string>>({});
  let tradeCashHoldingDrafts = $state<Record<string, string>>({});
  let tradeDateTimeDrafts = $state<Record<number, string>>({});
  let settlementDateDrafts = $state<Record<number, string>>({});
  let openHistoryTicketNumber = $state(0);
  let openAccountAddTicketNumber = $state(0);
  let submittedTicketNumber = $state(0);
  let historyByTicketNumber = $state<Record<number, { events: TicketReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = '';
  const tickets = $derived(data.tickets?.items ?? []);
  const foleoTraderOrders = $derived(data.foleoTraderOrders?.items ?? []);
  const filteredTickets = $derived(tickets.filter(ticketMatchesFilters));
  const hasOpenTickets = $derived(tickets.some(isOpenTicket));
  const accounts = $derived(data.accounts?.items ?? []);
  const brokers = $derived(data.brokers?.items ?? []);
  const pendingTradeFileTickets = $derived(tickets.filter((ticket) => ticket.tradeExecutionStatus === 'PendingTradeFile'));
  const pendingTicketsForBatchBroker = $derived(pendingTradeFileTickets.filter((ticket) => ticket.executionBrokerLEI === selectedTradeFileBatchBrokerLEI));
  const holdings = $derived(data.holdings?.items ?? []);
  const instruments = $derived(data.instruments?.items ?? []);
  const instrumentValues = $derived(data.instrumentValues?.items ?? []);
  const ticketSideOptions: TicketSide[] = ['Buy', 'Sell'];
  const decimalInputPattern = '(?:[0-9]+|[0-9]{1,3}(?:,[0-9]{3})+)(?:[.][0-9]+)?';
  const integerInputPattern = '(?:[0-9]+|[0-9]{1,3}(?:,[0-9]{3})+)';
  const ticketStageOptions = $derived(data.ticketStageOptions ?? []);
  const activeAccounts = $derived(accounts.filter((account) => account.active));
  const activeBrokers = $derived([...brokers.filter((broker) => broker.active)].sort((left, right) => brokerLabel(left).localeCompare(brokerLabel(right))));
  const activeInstruments = $derived(instruments.filter((instrument) => instrument.active));
  const sortedInstruments = $derived(
    [...activeInstruments].sort((left, right) => instrumentLabel(left).localeCompare(instrumentLabel(right)))
  );
  const createTicketInstrumentOptions = $derived<ComplexSelectOption[]>(sortedInstruments.map((instrument) => ({
    id: instrument.instrumentID,
    name: instrument.name,
    meta: `${instrument.formalName} - ${instrument.priceCurrency}`,
    search: `${instrument.instrumentID} ${instrument.name} ${instrument.formalName} ${instrument.priceCurrency} ${instrument.exchange} ${instrument.cfi}`
  })));
  const asOfSummary = $derived(data.auditDateTime && data.tickets ? formatDisplayDateTime(data.tickets.asOfDateTime) : 'now');
  const ticketSummaryQualifier = $derived(selectedStages.length === 0 && !selectedEstimatedBookCosts ? 'active ' : '');
  const liveUpdateLastEventIDs = $derived([
    data.tickets?.lastEventID ?? null,
    data.foleoTraderOrders?.lastEventID ?? null
  ]);
  const hasActiveFoleoTraderOrders = $derived(foleoTraderOrders.some((order) => order.status === 'Submitted' || order.status === 'PartiallyFilled'));
  const hasActiveTradeFiles = $derived(pendingTradeFileTickets.length > 0 || (data.tradeFiles?.length ?? 0) > 0);
  const formTicketNumber = $derived(form?.status === 'failure' ? readFormTicketNumber(form) || submittedTicketNumber : 0);
  const historyContextKey = $derived([
    data.valuationDate,
    data.auditDateTime ?? '',
    data.tickets?.lastEventID ?? '',
    form?.status === 'success' ? form.eventID ?? '' : ''
  ].join('|'));
  const canCommitTicket = $derived(
    createTicketSide !== '' &&
    !!resolveInstrumentInput(createTicketInstrument) &&
    submitting !== 'createTicket'
  );
  const canConfirmTicketCancel = $derived(cancelConfirmationInput.trim().toLowerCase() === 'delete');

  $effect(() => {
    const nextHistoryContextKey = historyContextKey;
    if (!loadedHistoryContextKey) {
      loadedHistoryContextKey = nextHistoryContextKey;
      return;
    }

    if (nextHistoryContextKey === loadedHistoryContextKey)
      return;

    loadedHistoryContextKey = nextHistoryContextKey;
    if (openHistoryTicketNumber)
      void loadHistory(openHistoryTicketNumber);
  });

  const enhanceAction = (name: string): SubmitFunction => {
    return ({ formData }) => {
      submittedTicketNumber = readSubmittedTicketNumber(formData);
      submitting = name;
      return async ({ result, update }) => {
        const historyTicketNumber = result.type === 'success' ? openHistoryTicketNumber : 0;
        await update();
        submitting = '';
        if (result.type === 'success' && name.startsWith('save-ticket-'))
          stopTicketEdit();
        if (name.startsWith('delete-'))
          cancelTicketCancellation();
        if (historyTicketNumber)
          void loadHistory(historyTicketNumber);
      };
    };
  };

  function readSubmittedTicketNumber(formData: FormData) {
    const ticketNumber = Number(formData.get('ticketNumber'));
    return Number.isFinite(ticketNumber) ? ticketNumber : 0;
  }

  function readFormTicketNumber(value: unknown) {
    if (!value || typeof value !== 'object' || !('ticketNumber' in value))
      return 0;

    const ticketNumber = (value as { ticketNumber?: unknown }).ticketNumber;
    return typeof ticketNumber === 'number' && Number.isFinite(ticketNumber) ? ticketNumber : 0;
  }

  function findInstrument(instrumentID: string) {
    return instruments.find((instrument) => instrument.instrumentID === instrumentID) ?? null;
  }

  function findInstrumentValue(instrumentID: string) {
    return instrumentValues.find((instrument) => instrument.instrumentID === instrumentID) ?? null;
  }

  function instrumentName(instrumentID: string) {
    return findInstrument(instrumentID)?.name ?? instrumentID;
  }

  function instrumentLabel(instrument: Instrument) {
    const ticker = instrument.identifiers.find((identifier) => identifier.type === 'Ticker' || identifier.type === 0)?.value ?? '';
    return [ticker, instrument.name, instrument.exchange].filter(Boolean).join(' - ');
  }

  function instrumentIdentifier(instrument: Instrument | null, type: 'ISIN' | 'Sedol') {
    return instrument?.identifiers.find((identifier) => matchesIdentifierType(identifier.type, type))?.value ?? '';
  }

  function matchesIdentifierType(type: string | number, expected: 'ISIN' | 'Sedol') {
    if (typeof type === 'string')
      return type.toLowerCase() === expected.toLowerCase();

    return expected === 'Sedol'
      ? type === 0
      : type === 1;
  }

  function cfiCategory(cfi: string | null | undefined) {
    switch ((cfi ?? '').charAt(0).toUpperCase()) {
      case 'C':
        return 'Collective investment vehicles';
      case 'D':
        return 'Debt instruments';
      case 'E':
        return 'Equities';
      case 'F':
        return 'Futures';
      case 'M':
        return 'Others/Miscellaneous';
      case 'O':
        return 'Options';
      case 'R':
        return 'Entitlements';
      case 'S':
        return 'Swaps';
      default:
        return '';
    }
  }

  function instrumentHeaderFacts(instrument: Instrument | null) {
    const isin = instrumentIdentifier(instrument, 'ISIN');
    const sedol = instrumentIdentifier(instrument, 'Sedol');
    const category = cfiCategory(instrument?.cfi);

    return [
      isin ? { label: 'ISIN', value: isin } : null,
      sedol ? { label: 'SEDOL', value: sedol } : null,
      category ? { label: 'CFI Category', value: category } : null
    ].filter((item): item is { label: string; value: string } => item !== null);
  }

  function resolveInstrumentInput(value: string) {
    const input = value.trim();

    if (!input)
      return null;

    return sortedInstruments.find((instrument) =>
      instrument.instrumentID === input ||
      instrumentLabel(instrument) === input ||
      instrument.name === input
    ) ?? null;
  }

  function accountName(accountID: string) {
    return accounts.find((account) => account.accountID === accountID)?.name ?? accountID;
  }

  function accountCurrency(accountID: string) {
    return accounts.find((account) => account.accountID === accountID)?.bookCurrency ?? '';
  }

  function cashInvestableHoldings(ticket: Ticket, accountID: string) {
    return [...holdings]
      .filter((holding) =>
        holding.accountID === accountID &&
        holding.holdingKind === 'CashInvestable' &&
        holding.active &&
        findInstrument(holding.instrumentID)?.priceCurrency === ticket.tradeCurrency
      )
      .sort((left, right) => {
        if (left.default !== right.default)
          return left.default ? -1 : 1;

        return cashHoldingLabel(left).localeCompare(cashHoldingLabel(right));
      });
  }

  function selectedTradeCashHoldingID(ticket: Ticket, accountID: string, cashHoldingID: string | null | undefined) {
    const options = cashInvestableHoldings(ticket, accountID);
    if (cashHoldingID && options.some((holding) => holding.holdingID === cashHoldingID))
      return cashHoldingID;

    return options.find((holding) => holding.default)?.holdingID ?? '';
  }

  function tradeCashHoldingInputValue(ticket: Ticket, accountID: string, cashHoldingID: string | null | undefined) {
    const key = tradeDraftKey(ticket.ticketNumber, accountID);
    const draft = tradeCashHoldingDrafts[key];

    if (draft !== undefined)
      return draft;

    return selectedTradeCashHoldingID(ticket, accountID, cashHoldingID);
  }

  function cashHoldingLabel(holding: Holding) {
    const currency = findInstrument(holding.instrumentID)?.priceCurrency ?? '';
    return [holding.name, currency].filter(Boolean).join(' ');
  }

  function selectedCashHoldingLabel(ticket: Ticket, accountID: string, cashHoldingID: string | null | undefined) {
    const selectedID = selectedTradeCashHoldingID(ticket, accountID, cashHoldingID);
    const holding = cashInvestableHoldings(ticket, accountID).find((item) => item.holdingID === selectedID);
    return holding ? cashHoldingLabel(holding) : 'Not set';
  }

  function brokerLabel(broker: Broker) {
    return [broker.name, broker.lei].filter(Boolean).join(' - ');
  }

  function brokerName(brokerLEI: string | null | undefined) {
    if (!brokerLEI)
      return 'Not set';

    return brokers.find((broker) => broker.lei === brokerLEI)?.name ?? brokerLEI;
  }

  function availableAccounts(ticket: Ticket) {
    const selected = new Set(ticket.accountIDs);
    return activeAccounts.filter((account) => !selected.has(account.accountID));
  }

  function accountAddOptions(accounts: Account[]): ComplexSelectOption[] {
    return accounts.map((account) => ({
      id: account.accountID,
      name: account.name,
      meta: account.bookCurrency
    }));
  }

  function handleAccountAddOpenChange(open: boolean, ticketNumber: number) {
    if (open)
      openAccountAddTicketNumber = ticketNumber;
    else if (openAccountAddTicketNumber === ticketNumber)
      openAccountAddTicketNumber = 0;
  }

  function equityPrice(price: InstrumentValue['price']): InstrumentPriceEquity | null {
    return price && 'bid' in price ? price : null;
  }

  function fixedIncomePrice(price: InstrumentValue['price']): InstrumentPriceFixedIncome | null {
    return price && 'cleanPrice' in price ? price : null;
  }

  function cashPrice(price: InstrumentValue['price']): InstrumentPriceCash | null {
    return price && 'price' in price ? price : null;
  }

  function decimalText(value: number | null | undefined) {
    if (value == null)
      return '';
    if (!Number.isFinite(value))
      return '';
    if (value === 0)
      return '0';

    const text = value.toString();

    if (!text.toLowerCase().includes('e'))
      return text;

    const [coefficient, exponentText] = text.toLowerCase().split('e');
    const exponent = Number(exponentText);
    const sign = coefficient.startsWith('-') ? '-' : '';
    const unsignedCoefficient = sign ? coefficient.slice(1) : coefficient;
    const [whole, fraction = ''] = unsignedCoefficient.split('.');
    const digits = `${whole}${fraction}`;
    const decimalIndex = whole.length + exponent;

    if (decimalIndex <= 0)
      return `${sign}0.${'0'.repeat(Math.abs(decimalIndex))}${digits}`;
    if (decimalIndex >= digits.length)
      return `${sign}${digits}${'0'.repeat(decimalIndex - digits.length)}`;

    return `${sign}${digits.slice(0, decimalIndex)}.${digits.slice(decimalIndex)}`;
  }

  function decimalDisplay(value: number | null | undefined) {
    return decimalText(value) || 'Not set';
  }

  function groupedDecimalText(value: number | null | undefined, minimumFractionDigits = 0) {
    const text = decimalText(value);

    if (!text)
      return '';

    const sign = text.startsWith('-') ? '-' : '';
    const unsignedText = sign ? text.slice(1) : text;
    const [whole, fraction = ''] = unsignedText.split('.');
    const groupedWhole = whole.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    const paddedFraction = fraction.padEnd(minimumFractionDigits, '0');

    return paddedFraction ? `${sign}${groupedWhole}.${paddedFraction}` : `${sign}${groupedWhole}`;
  }

  function currencyFractionDigits(currency: string | null | undefined) {
    if (!currency)
      return 2;

    try {
      return new Intl.NumberFormat(undefined, { currency, style: 'currency' }).resolvedOptions().maximumFractionDigits;
    } catch {
      return 2;
    }
  }

  function priceText(value: number | null | undefined, currency: string | null | undefined) {
    return groupedDecimalText(value, currencyFractionDigits(currency));
  }

  function currentInstrumentPrice(instrumentID: string) {
    const instrument = findInstrumentValue(instrumentID);
    const equity = equityPrice(instrument?.price);

    if (equity)
      return equity.mid.amount ?? null;

    const fixedIncome = fixedIncomePrice(instrument?.price);

    if (fixedIncome)
      return fixedIncome.cleanPrice.amount ?? null;

    const cash = cashPrice(instrument?.price);

    return cash?.price.amount ?? null;
  }

  function targetPriceDisplay(ticket: Ticket) {
    return formattedDisplay(priceText(ticket.proposalTargetPrice, ticket.tradeCurrency));
  }

  function targetPriceInputValue(ticket: Ticket) {
    return priceText(ticket.proposalTargetPrice ?? currentInstrumentPrice(ticket.instrumentID), ticket.tradeCurrency);
  }

  function quantityText(value: number | null | undefined) {
    return groupedDecimalText(value);
  }

  function quantityTotal(items: { quantity: number }[]) {
    return items.reduce((total, item) => total + item.quantity, 0);
  }

  function proposalTotal(ticket: Ticket) {
    return quantityTotal(ticket.proposalAllocations);
  }

  function proposalQuantityDraftKey(ticketNumber: number, accountID: string) {
    return `${ticketNumber}:${accountID}`;
  }

  function tradeDraftKey(ticketNumber: number, accountID: string) {
    return `${ticketNumber}:${accountID}`;
  }

  function parseDecimalInput(value: string) {
    const normalized = value.replace(/,/g, '').trim();
    const number = normalized ? Number(normalized) : 0;
    return Number.isFinite(number) ? number : 0;
  }

  function proposalQuantityInputValue(ticket: Ticket, accountID: string) {
    const key = proposalQuantityDraftKey(ticket.ticketNumber, accountID);
    const draft = proposalQuantityDrafts[key];

    if (draft !== undefined)
      return draft;

    const allocation = ticket.proposalAllocations.find((item) => item.accountID === accountID);
    return quantityText(allocation?.quantity);
  }

  function liveProposalAllocationTotal(ticket: Ticket) {
    return ticket.accountIDs.reduce((total, accountID) => {
      const key = proposalQuantityDraftKey(ticket.ticketNumber, accountID);
      const draft = proposalQuantityDrafts[key];

      if (draft !== undefined)
        return total + parseDecimalInput(draft);

      const allocation = ticket.proposalAllocations.find((item) => item.accountID === accountID);
      return total + (allocation?.quantity ?? 0);
    }, 0);
  }

  function tradeQuantityInputValue(ticket: Ticket, accountID: string) {
    const key = tradeDraftKey(ticket.ticketNumber, accountID);
    const draft = tradeQuantityDrafts[key];

    if (draft !== undefined)
      return draft;

    const allocation = ticket.tradeAllocations.find((item) => item.accountID === accountID);
    return quantityText(allocation?.quantity);
  }

  function tradeSettlementAmountInputValue(ticket: Ticket, accountID: string) {
    const key = tradeDraftKey(ticket.ticketNumber, accountID);
    const draft = tradeSettlementAmountDrafts[key];

    if (draft !== undefined)
      return draft;

    const allocation = ticket.tradeAllocations.find((item) => item.accountID === accountID);
    return quantityText(allocation?.settlementAmount);
  }

  function liveTradeAllocationTotal(ticket: Ticket) {
    return ticket.accountIDs.reduce((total, accountID) => {
      const key = tradeDraftKey(ticket.ticketNumber, accountID);
      const draft = tradeQuantityDrafts[key];

      if (draft !== undefined)
        return total + parseDecimalInput(draft);

      const allocation = ticket.tradeAllocations.find((item) => item.accountID === accountID);
      return total + (allocation?.quantity ?? 0);
    }, 0);
  }

  function liveTradeSettlementAmountTotal(ticket: Ticket) {
    return ticket.accountIDs.reduce((total, accountID) => {
      const key = tradeDraftKey(ticket.ticketNumber, accountID);
      const draft = tradeSettlementAmountDrafts[key];

      if (draft !== undefined)
        return total + parseDecimalInput(draft);

      const allocation = ticket.tradeAllocations.find((item) => item.accountID === accountID);
      return total + (allocation?.settlementAmount ?? 0);
    }, 0);
  }

  function settlementAmountTotal(items: { settlementAmount: number }[]) {
    return items.reduce((total, item) => total + item.settlementAmount, 0);
  }

  function quantityDifference(expected: number | null | undefined, actual: number) {
    return (expected ?? 0) - actual;
  }

  function quantitiesMatch(expected: number | null | undefined, actual: number) {
    return expected != null && Math.abs(expected - actual) < 0.00000001;
  }

  function formattedDisplay(text: string) {
    return text || 'Not set';
  }

  function cleanDecimalInput(event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    const cleaned = input.value.replace(/[^0-9,.]/g, '');
    const [whole, ...fractionParts] = cleaned.split('.');
    input.value = fractionParts.length > 0 ? `${whole}.${fractionParts.join('')}` : whole;
  }

  function cleanIntegerInput(event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    input.value = input.value.replace(/[^0-9,]/g, '');
  }

  function updateProposalQuantityDraft(ticketNumber: number, accountID: string, event: Event) {
    cleanDecimalInput(event);

    const input = event.currentTarget as HTMLInputElement;
    proposalQuantityDrafts[proposalQuantityDraftKey(ticketNumber, accountID)] = input.value;
  }

  function updateTradeQuantityDraft(ticketNumber: number, accountID: string, event: Event) {
    cleanDecimalInput(event);

    const input = event.currentTarget as HTMLInputElement;
    tradeQuantityDrafts[tradeDraftKey(ticketNumber, accountID)] = input.value;
  }

  function updateTradeSettlementAmountDraft(ticketNumber: number, accountID: string, event: Event) {
    cleanDecimalInput(event);

    const input = event.currentTarget as HTMLInputElement;
    tradeSettlementAmountDrafts[tradeDraftKey(ticketNumber, accountID)] = input.value;
  }

  function updateTradeCashHoldingDraft(ticketNumber: number, accountID: string, event: Event) {
    const select = event.currentTarget as HTMLSelectElement;
    tradeCashHoldingDrafts[tradeDraftKey(ticketNumber, accountID)] = select.value;
  }

  function tradeDateTimeInputValue(ticket: Ticket) {
    return tradeDateTimeDrafts[ticket.ticketNumber] ?? (ticket.tradeDateTime ? dateTimeForInput(ticket.tradeDateTime) : nowForInput());
  }

  function settlementDateInputValue(ticket: Ticket) {
    return settlementDateDrafts[ticket.ticketNumber] ?? (ticket.settlementDateTime ? dateForInput(ticket.settlementDateTime) : nextWorkingDayDateForInput(tradeDateTimeInputValue(ticket)));
  }

  function tradeSettlementMin(ticket: Ticket) {
    return dateForInput(tradeDateTimeInputValue(ticket));
  }

  function tradeDateDisplay(ticket: Ticket) {
    return ticket.tradeDateTime ? formatDisplayDateTime(ticket.tradeDateTime) : '-';
  }

  function settlementDateDisplay(ticket: Ticket) {
    return ticket.settlementDateTime ? formatShortDate(ticket.settlementDateTime) : '-';
  }

  function updateTradeDateTimeDraft(ticketNumber: number, event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    tradeDateTimeDrafts[ticketNumber] = input.value;
    if (!settlementDateDrafts[ticketNumber])
      settlementDateDrafts[ticketNumber] = nextWorkingDayDateForInput(input.value);
  }

  function updateSettlementDateDraft(ticketNumber: number, event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    settlementDateDrafts[ticketNumber] = input.value;
  }

  function resizeTextarea(textarea: HTMLTextAreaElement) {
    textarea.style.height = 'auto';
    textarea.style.height = `${textarea.scrollHeight}px`;
  }

  function autoResizeTextarea(value: string | null | undefined) {
    return (textarea: HTMLTextAreaElement) => {
      void value;
      const resize = () => resizeTextarea(textarea);

      resize();
      requestAnimationFrame(resize);
      textarea.addEventListener('input', resize);

      return () => {
        textarea.removeEventListener('input', resize);
      };
    };
  }

  function ticketStep(ticket: Ticket) {
    switch (ticket.stage) {
      case 'Proposal':
        return 'Proposal';
      case 'Trade':
        return 'Trade';
      case 'Completed':
        return 'Completed';
      case 'Cancelled':
        return 'Cancelled';
      default:
        return ticket.stage;
    }
  }

  function canEditProposal(ticket: Ticket) {
    return ticket.accountIDs.length > 0 && canEditProposalTerms(ticket);
  }

  function canEditProposalTerms(ticket: Ticket) {
    return ticket.stage === 'Proposal' && ticket.proposalDecision === 'InProgress';
  }

  function canSetProposalText(ticket: Ticket) {
    return ticket.stage === 'Proposal';
  }

  function canApproveProposal(ticket: Ticket) {
    return ticket.stage === 'Proposal' &&
      ticket.proposalDecision === 'PendingApproval' &&
      ticket.proposalAllocations.length > 0;
  }

  function isAwaitingProposalDecision(ticket: Ticket) {
    return ticket.stage === 'Proposal' && ticket.proposalDecision === 'PendingApproval';
  }

  function canRequestProposalDecision(ticket: Ticket) {
    return ticket.stage === 'Proposal' &&
      ticket.proposalDecision === 'InProgress' &&
      ticket.proposalTargetPrice != null &&
      ticket.proposalAllocations.length > 0;
  }

  function canEditTrade(ticket: Ticket) {
    return ticket.stage === 'Trade' && ticket.proposalDecision === 'Approved' && ticket.tradeDecision === 'InProgress' && !ticket.isExecutionLocked;
  }

  function canSetTradeText(ticket: Ticket) {
    return ticket.stage === 'Trade' && !ticket.isExecutionLocked;
  }

  function canEditFills(ticket: Ticket) {
    return ticket.stage === 'Trade' && !ticket.isExecutionLocked;
  }

  function isAwaitingTradeDecision(ticket: Ticket) {
    return ticket.stage === 'Trade' && ticket.tradeDecision === 'PendingApproval';
  }

  function canRequestTradeDecision(ticket: Ticket) {
    const tradeTotal = quantityTotal(ticket.tradeAllocations);

    return ticket.stage === 'Trade' &&
      ticket.tradeDecision === 'InProgress' &&
      ticket.tradePrice != null &&
      !!ticket.tradeDateTime &&
      !!ticket.settlementDateTime &&
      ticket.tradeAllocations.length > 0 &&
      hasTradeCashHoldings(ticket.tradeAllocations) &&
      quantitiesMatch(proposalTotal(ticket), tradeTotal);
  }

  function foleoTraderOrderFor(ticket: Ticket) {
    return foleoTraderOrders.findLast((order) => order.ticketNumber === ticket.ticketNumber) ?? null;
  }

  function canSendFoleoTrader(ticket: Ticket, order: FoleoTraderOrder | null) {
    return ticket.stage === 'Trade' &&
      ticket.proposalDecision === 'Approved' &&
      ticket.tradeDecision === 'InProgress' &&
      ticket.proposalTargetPrice != null &&
      proposalTotal(ticket) > 0 &&
      ticket.tradePrice == null &&
      ticket.tradeAllocations.length === 0 &&
      ticket.fills.length === 0 &&
      (ticket.tradeExecutionStatus === 'Ready' || ticket.tradeExecutionStatus === 'Failed') &&
      order === null;
  }

  function selectBatchBroker(lei: string) {
    selectedTradeFileBatchBrokerLEI = lei;
    selectedPendingTicketNumbers = [];
  }

  function selectFixBroker(ticketNumber: number, lei: string) {
    fixBrokerByTicket = { ...fixBrokerByTicket, [ticketNumber]: lei };
  }

  function selectTradeFileBroker(ticketNumber: number, lei: string) {
    tradeFileBrokerByTicket = { ...tradeFileBrokerByTicket, [ticketNumber]: lei };
  }

  function foleoTraderStatusText(order: FoleoTraderOrder | null) {
    if (!order)
      return '';

    const filled = `${quantityText(order.filledQuantity)} / ${quantityText(order.orderQuantity)}`;
    return order.status === 'Failed'
      ? `FoleoTrader failed: ${order.lastError ?? 'Unable to send order'}`
      : `FoleoTrader ${order.status} ${filled}`;
  }

  function canApproveTrade(ticket: Ticket) {
    const tradeTotal = quantityTotal(ticket.tradeAllocations);

    return ticket.stage === 'Trade' &&
      ticket.tradeDecision === 'PendingApproval' &&
      !!ticket.tradeDateTime &&
      !!ticket.settlementDateTime &&
      ticket.tradeAllocations.length > 0 &&
      hasTradeCashHoldings(ticket.tradeAllocations) &&
      quantitiesMatch(proposalTotal(ticket), tradeTotal) &&
      quantitiesMatch(tradeTotal, quantityTotal(ticket.fills)) &&
      quantitiesMatch(settlementAmountTotal(ticket.tradeAllocations), settlementAmountTotal(ticket.fills));
  }

  function hasTradeCashHoldings(allocations: Ticket['tradeAllocations']) {
    return allocations.every((allocation) => !!allocation.cashHoldingID);
  }

  function isTicketExpanded(ticketNumber: number) {
    return ticketViewMode === 'Detailed'
      ? !collapsedTicketNumbers.includes(ticketNumber)
      : expandedTicketNumbers.includes(ticketNumber);
  }

  function isTicketEditing(ticketNumber: number, context?: TicketEditContext) {
    return editingTicket.ticketNumber === ticketNumber && (!context || editingTicket.context === context);
  }

  function toggleTicketExpanded(ticketNumber: number) {
    const expanded = isTicketExpanded(ticketNumber);

    if (ticketViewMode === 'Detailed') {
      collapsedTicketNumbers = expanded
        ? [...collapsedTicketNumbers, ticketNumber]
        : collapsedTicketNumbers.filter((collapsedTicketNumber) => collapsedTicketNumber !== ticketNumber);
    } else {
      expandedTicketNumbers = expanded
        ? expandedTicketNumbers.filter((expandedTicketNumber) => expandedTicketNumber !== ticketNumber)
        : [...expandedTicketNumbers, ticketNumber];
    }

    stopTicketEdit();
    cancelTicketCancellation();
  }

  async function toggleHistory(ticketNumber: number) {
    if (openHistoryTicketNumber === ticketNumber) {
      openHistoryTicketNumber = 0;
      delete historyByTicketNumber[ticketNumber];
      return;
    }

    openHistoryTicketNumber = ticketNumber;

    if (historyByTicketNumber[ticketNumber])
      return;

    await loadHistory(ticketNumber);
  }

  async function loadHistory(ticketNumber: number) {
    historyByTicketNumber[ticketNumber] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Blotter/History', window.location.origin);
      historyUrl.searchParams.set('ticketNumber', ticketNumber.toString());
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByTicketNumber[ticketNumber] = {
        events: await response.json() as TicketReferenceEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByTicketNumber[ticketNumber] = {
        events: [],
        error: error instanceof Error ? error.message : 'Unable to load history.',
        loading: false
      };
    }
  }

  function startTicketEdit(ticket: Ticket, context: TicketEditContext) {
    expandTicket(ticket.ticketNumber);
    editingTicket = { ticketNumber: ticket.ticketNumber, context };
    if (context === 'Trade') {
      const tradeDateTime = ticket.tradeDateTime ? dateTimeForInput(ticket.tradeDateTime) : nowForInput();
      tradeDateTimeDrafts[ticket.ticketNumber] = tradeDateTime;
      settlementDateDrafts[ticket.ticketNumber] = ticket.settlementDateTime ? dateForInput(ticket.settlementDateTime) : nextWorkingDayDateForInput(tradeDateTime);
    }
    cancelTicketCancellation();
  }

  function expandTicket(ticketNumber: number) {
    if (ticketViewMode === 'Detailed') {
      collapsedTicketNumbers = collapsedTicketNumbers.filter((collapsedTicketNumber) => collapsedTicketNumber !== ticketNumber);
    } else if (!expandedTicketNumbers.includes(ticketNumber)) {
      expandedTicketNumbers = [...expandedTicketNumbers, ticketNumber];
    }
  }

  function stopTicketEdit() {
    if (editingTicket.ticketNumber) {
      delete tradeDateTimeDrafts[editingTicket.ticketNumber];
      delete settlementDateDrafts[editingTicket.ticketNumber];
    }
    editingTicket = { ticketNumber: 0, context: '' };
  }

  function startTicketCancellation(ticketNumber: number) {
    cancelConfirmingTicketNumber = ticketNumber;
    cancelConfirmationInput = '';
  }

  function cancelTicketCancellation() {
    cancelConfirmingTicketNumber = 0;
    cancelConfirmationInput = '';
  }

  function clearSideFilters() {
    selectedSides = [];
  }

  function selectSideFilter(side: TicketSide) {
    selectedSides = [side];
  }

  function clearStageFilters() {
    selectedStages = [];
    selectedEstimatedBookCosts = false;
  }

  function toggleStageFilter(stage: TicketStage) {
    selectedStages = selectedStages.includes(stage)
      ? selectedStages.filter((selectedStage) => selectedStage !== stage)
      : [...selectedStages, stage];
  }

  function toggleEstimatedBookCostFilter() {
    selectedEstimatedBookCosts = !selectedEstimatedBookCosts;
  }

  function isOpenTicket(ticket: Ticket) {
    return ticket.isActive !== false && ticket.stage !== 'Completed' && ticket.stage !== 'Cancelled';
  }

  function ticketMatchesFilters(ticket: Ticket) {
    const sideMatches = selectedSides.length === 0 || selectedSides.includes(ticket.side);
    const stageMatches = selectedStages.length === 0 ? isOpenTicket(ticket) : selectedStages.includes(ticket.stage);
    const estimatedMatches = !selectedEstimatedBookCosts || hasEstimatedBookCost(ticket);
    const text = freeTextFilter.trim().toLowerCase();

    return sideMatches && stageMatches && estimatedMatches && (!text || ticketSearchText(ticket).includes(text));
  }

  function hasEstimatedBookCost(ticket: Ticket) {
    return ticket.tradeAllocations.some((allocation) => {
      if (allocation.bookCostEstimated === true)
        return true;

      if (allocation.bookCostOverride != null)
        return false;

      const bookCurrency = accountCurrency(allocation.accountID);
      return !!ticket.tradeCurrency && !!bookCurrency && ticket.tradeCurrency !== bookCurrency;
    });
  }

  function ticketSearchText(ticket: Ticket) {
    return [
      ticket.ticketNumber.toString(),
      `#${ticket.ticketNumber}`,
      ticket.side,
      ticket.stage,
      stageDescription(ticket.stage),
      ticket.proposalDecision,
      ticket.tradeDecision,
      ticketDecisionDescription(ticket),
      ticket.proposalReason,
      ticket.proposalAllocation,
      ticket.tradeInstructionNotes,
      ticket.tradeProgressNotes,
      ticketStep(ticket),
      hasEstimatedBookCost(ticket) ? 'estimated book cost' : '',
      instrumentName(ticket.instrumentID),
      ...instrumentHeaderFacts(findInstrument(ticket.instrumentID)).map((fact) => fact.value),
      ...ticket.accountIDs.flatMap((accountID) => [accountName(accountID), accountCurrency(accountID)]),
      formatTableDateTime(ticket.lastAuditDateTime)
    ].join(' ').toLowerCase();
  }

  function stageDescription(stage: TicketStage) {
    return ticketStageOptions.find((option) => option.stage === stage)?.description ?? stage;
  }

  function decisionDescription(decision: Ticket['proposalDecision']) {
    switch (decision) {
      case 'InProgress':
        return 'In progress';
      case 'PendingApproval':
        return 'Pending approval';
      case 'NotApproved':
        return 'Not approved';
      default:
        return decision;
    }
  }

  function ticketDecisionDescription(ticket: Ticket) {
    if (ticket.stage === 'Proposal')
      return decisionDescription(ticket.proposalDecision);
    if (ticket.stage === 'Trade' || ticket.stage === 'Completed') {
      const decision = decisionDescription(ticket.tradeDecision);
      const tradeFileStatus = ticket.stage === 'Trade' && ticket.tradeDecision === 'InProgress'
        ? ticketTradeFileStatusDescription(ticket)
        : '';
      return tradeFileStatus ? `${decision} (TradeFile: ${tradeFileStatus})` : decision;
    }
    return decisionDescription(ticket.proposalDecision);
  }

  function ticketTradeFileStatusDescription(ticket: Ticket) {
    const tradeFile = (data.tradeFiles ?? []).find((candidate) =>
      (ticket.tradeFileID && candidate.tradeFileID === ticket.tradeFileID) ||
      candidate.tickets.some((tradeFileTicket) => tradeFileTicket.ticketNumber === ticket.ticketNumber)
    );
    const status = tradeFile?.status ?? ticketTradeFileStatus(ticket);
    if (status === 'Acknowledged')
      return 'Received';
    if (status === 'InProgress')
      return 'Received - processing';
    return status;
  }

  function ticketTradeFileStatus(ticket: Ticket): TradeFileStatus | 'Pending' | '' {
    if (ticket.tradeExecutionMethod !== 'TradeFile' && !ticket.tradeFileID)
      return '';

    switch (ticket.tradeExecutionStatus) {
      case 'PendingTradeFile':
        return 'Pending';
      case 'TradeFileRequested':
        return 'Requested';
      case 'TradeFileCreated':
        return 'Created';
      case 'TradeFileSent':
        return 'Sent';
      case 'TradeFileAcknowledged':
        return 'Acknowledged';
      case 'InProgress':
        return 'InProgress';
      case 'Failed':
        return 'Failed';
      case 'Completed':
        return 'Completed';
      default:
        return '';
    }
  }

  function ticketHistorySummary(event: TicketReferenceEvent) {
    const details = event.details ?? {};
    const type = event.$type;

    if (type === 'TicketCreatedEvent')
      return [readDetailString(details, 'Side'), instrumentName(readDetailString(details, 'InstrumentID')), readDetailString(details, 'TradeCurrency')].filter(Boolean).join(' · ');

    if (type === 'TicketAccountAddedEvent' || type === 'TicketAccountRemovedEvent')
      return accountName(readDetailString(details, 'AccountID'));

    if (type === 'TicketProposalCreatedEvent' || type === 'TicketProposalModifiedEvent')
      return [
        `Target ${detailAmount(details, 'TargetPrice')}`,
        allocationQuantitySummary(details),
        allocationCount(details)
      ].filter(Boolean).join(' · ');

    if (type === 'TicketTradeCreatedEvent' || type === 'TicketTradeModifiedEvent')
      return [`Price ${detailAmount(details, 'TradedPrice')}`, allocationCount(details)].filter(Boolean).join(' · ');

    if (type === 'TicketTradeFillAddedEvent' || type === 'TicketTradeFillModifiedEvent')
      return [`Price ${readDetailValue(details, 'Price')}`, `Quantity ${readDetailValue(details, 'Quantity')}`, `Settlement ${readDetailValue(details, 'SettlementAmount')}`, readDetailString(details, 'Note')].filter(Boolean).join(' · ');

    if (type === 'TicketTradeFillRemovedEvent')
      return `Fill ${readDetailString(details, 'FillID')}`;

    if (type === 'TicketTradeDecisionRequestedEvent')
      return 'Trade review requested';

    return [
      readDetailString(details, 'ProposalReason'),
      readDetailString(details, 'ProposalAllocation'),
      readDetailString(details, 'TradeInstructionNotes'),
      readDetailString(details, 'TradeProgressNotes')
    ].filter(Boolean).join(' · ');
  }

  function detailAmount(details: Record<string, unknown>, key: string) {
    const value = details[key];

    if (typeof value === 'number')
      return value.toString();

    if (value && typeof value === 'object') {
      const amount = (value as Record<string, unknown>).amount ?? (value as Record<string, unknown>).Amount ?? (value as Record<string, unknown>).value ?? (value as Record<string, unknown>).Value;
      if (typeof amount === 'number')
        return amount.toString();
    }

    return readDetailString(details, key);
  }

  function allocationCount(details: Record<string, unknown>) {
    const allocations = details.Allocations ?? details.allocations;
    return Array.isArray(allocations)
      ? `${allocations.length} allocation${allocations.length === 1 ? '' : 's'}`
      : '';
  }

  function allocationQuantitySummary(details: Record<string, unknown>) {
    const allocations = details.Allocations ?? details.allocations;
    if (!Array.isArray(allocations))
      return '';

    const total = allocations.reduce((sum, allocation) => {
      if (!allocation || typeof allocation !== 'object')
        return sum;

      const value = (allocation as Record<string, unknown>).Quantity ?? (allocation as Record<string, unknown>).quantity;
      return sum + (typeof value === 'number' ? value : 0);
    }, 0);

    return total > 0 ? `Allocated ${quantityText(total)}` : '';
  }

  function readDetailString(details: Record<string, unknown>, key: string) {
    const value = details[key] ?? details[key.charAt(0).toLowerCase() + key.slice(1)];
    return typeof value === 'string' ? value : '';
  }

  function readDetailValue(details: Record<string, unknown>, key: string) {
    const value = details[key] ?? details[key.charAt(0).toLowerCase() + key.slice(1)];
    return typeof value === 'string' || typeof value === 'number' ? value.toString() : '';
  }

</script>

<main class="blotter-page min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">Blotter</p>
      <div class="page-header-content">
        <div class="page-header-main">
          <div class="page-title-row">
            <h1 class="page-title">Ticket Blotter</h1>
          </div>
          <p class="page-subtitle">Active buy and sell tickets through proposal, trade, approval, and deletion.</p>
        </div>
        <div class="page-header-aside">
          <BookmarkButton />
          <div class="page-header-summary">
            {filteredTickets.length} {ticketSummaryQualifier}ticket{filteredTickets.length === 1 ? '' : 's'} · as of {asOfSummary}
          </div>
        </div>
      </div>

      <section class="section-band create-ticket-card create-ticket-action-card blotter-filter-card">
        <div class="filter-card-header">
          <h2 class="create-ticket-title">Filter</h2>
          <div class="side-radio-group ticket-view-group" role="radiogroup" aria-label="Ticket display mode">
            <label class="side-radio-pill ticket-filter-pill">
              <input checked={ticketViewMode === 'Compact'} name="ticketViewMode" type="radio" value="Compact" onchange={() => ticketViewMode = 'Compact'} />
              <span>Compact</span>
            </label>
            <label class="side-radio-pill ticket-filter-pill">
              <input checked={ticketViewMode === 'Detailed'} name="ticketViewMode" type="radio" value="Detailed" onchange={() => ticketViewMode = 'Detailed'} />
              <span>Detailed</span>
            </label>
          </div>
        </div>
        <div class="ticket-filter-layout">
          <div class="side-radio-group ticket-filter-group ticket-side-filter-group" role="radiogroup" aria-label="Ticket side filters">
            <label class="side-radio-pill ticket-filter-pill">
              <input checked={selectedSides.length === 0} name="ticketSideFilter" type="radio" value="All" onchange={clearSideFilters} />
              <span>All</span>
            </label>
            {#each ticketSideOptions as side (side)}
              <label class="side-radio-pill ticket-filter-pill">
                <input
                  checked={selectedSides.includes(side)}
                  name="ticketSideFilter"
                  type="radio"
                  value={side}
                  onchange={() => selectSideFilter(side)}
                />
                <span>{side}</span>
              </label>
            {/each}
          </div>
          <div class="side-radio-group ticket-filter-group ticket-stage-filter-group" role="group" aria-label="Ticket stage filters">
            <label class="side-radio-pill ticket-filter-pill">
              <input checked={selectedStages.length === 0} name="ticketStageFilterAll" type="checkbox" onchange={clearStageFilters} />
              <span>All</span>
            </label>
            {#each ticketStageOptions as option (option.stage)}
              <label class="side-radio-pill ticket-filter-pill">
                <input
                  checked={selectedStages.includes(option.stage)}
                  name={`ticketStageFilter-${option.stage}`}
                  type="checkbox"
                  onchange={() => toggleStageFilter(option.stage)}
                />
                <span>{option.description}</span>
              </label>
            {/each}
            <label class="side-radio-pill ticket-filter-pill">
              <input checked={selectedEstimatedBookCosts} name="ticketEstimatedBookCostFilter" type="checkbox" onchange={toggleEstimatedBookCostFilter} />
              <span>Estimated</span>
            </label>
          </div>
          <Field class="ticket-text-filter">
            <input
              aria-label="Filter tickets"
              class="house-control house-control-md"
              bind:value={freeTextFilter}
              placeholder="Filter tickets"
              type="search"
            />
          </Field>
        </div>
      </section>
    </div>
  </section>

  <section class="page-container page-section space-y-6">
    <AggregateUpdateWatcher
      aggregateKind={['Tickets', 'FoleoTraderOrders', 'TradeFiles']}
      autoReload={!editingTicket.ticketNumber && !cancelConfirmingTicketNumber}
      valuationDate={data.valuationDate}
      lastEventIDs={liveUpdateLastEventIDs}
      pollIntervalMs={hasActiveFoleoTraderOrders || hasActiveTradeFiles ? 2000 : 0}
      auditDateTime={data.auditDateTime}
    />

    {#if data.error}
      <Card density="compact" intent="error">{data.error}</Card>
    {/if}

    {#if form?.message && !formTicketNumber}
      <div class={form.status === 'success' ? 'success-message' : 'error-message'}>
        {form.message}
      </div>
    {/if}

    <section class="section-band create-ticket-card create-ticket-action-card">
      <h2 class="create-ticket-title">Create a ticket</h2>
      <form class="create-ticket-form" method="POST" action="?/createTicket" use:enhance={enhanceAction('createTicket')}>
        <fieldset class="ticket-side-field">
          <div class="side-radio-group" role="radiogroup" aria-label="Ticket side">
            <label class="side-radio-pill">
              <input bind:group={createTicketSide} name="side" type="radio" value="Buy" required />
              <span>Buy</span>
            </label>
            <label class="side-radio-pill">
              <input bind:group={createTicketSide} name="side" type="radio" value="Sell" required />
              <span>Sell</span>
            </label>
          </div>
        </fieldset>
        <Field controlId="create-ticket-instrument">
          <ComplexSelect
            compactBrand
            id="create-ticket-instrument"
            name="instrumentID"
            options={createTicketInstrumentOptions}
            placeholder={createTicketInstrumentOptions.length ? 'Select instrument' : 'No instruments available'}
            bind:value={createTicketInstrument}
          />
        </Field>
        <button class="commit-ticket-button" type="submit" disabled={!canCommitTicket}>
          Commit
        </button>
      </form>
    </section>

    {#if pendingTradeFileTickets.length > 0}
      <section class="section-band create-ticket-card create-ticket-action-card trade-file-card">
        <div class="filter-card-header">
          <h2 class="create-ticket-title">Trade Files</h2>
          <span class="page-header-summary">{pendingTradeFileTickets.length} pending</span>
        </div>
        <form class="trade-file-batch-controls" method="POST" action="?/sendTradeFileBatch" use:enhance={enhanceAction('trade-file-batch')}>
          <input type="hidden" name="eventDateTime" value={eventDateDefault} />
          <BrokerDropdown
            {brokers}
            compactBrand
            method="TradeFile"
            name="brokerLEI"
            placeholder="Select TradeFile broker"
            bind:selectedBrokerLEI={() => selectedTradeFileBatchBrokerLEI, selectBatchBroker}
          />
          <TicketDropdown
            compactBrand
            disabled={!selectedTradeFileBatchBrokerLEI || !pendingTicketsForBatchBroker.length}
            {instruments}
            name="ticketNumbers"
            tickets={pendingTicketsForBatchBroker}
            bind:selectedTicketNumbers={selectedPendingTicketNumbers}
          />
          <button
            class="house-button house-button-secondary house-button-md"
            type="submit"
            disabled={!selectedTradeFileBatchBrokerLEI || selectedPendingTicketNumbers.length === 0 || submitting === 'trade-file-batch'}
          >
            Send File
          </button>
        </form>
      </section>
    {/if}

    {#if filteredTickets.length === 0}
      <section class="empty-state">{tickets.length === 0 || (selectedStages.length === 0 && !selectedEstimatedBookCosts && !hasOpenTickets) ? 'No active tickets.' : 'No tickets match the selected filters.'}</section>
    {:else}
      <section class="ticket-list">
        {#each filteredTickets as ticket (ticket.ticketNumber)}
          {@const ticketInstrument = findInstrument(ticket.instrumentID)}
          {@const instrument = ticketInstrument?.name ?? ticket.instrumentID}
          {@const instrumentFacts = instrumentHeaderFacts(ticketInstrument)}
          {@const ticketExpanded = isTicketExpanded(ticket.ticketNumber)}
          {@const ticketEditing = isTicketEditing(ticket.ticketNumber)}
          {@const ticketEditingProposal = isTicketEditing(ticket.ticketNumber, 'Proposal')}
          {@const ticketEditingTrade = isTicketEditing(ticket.ticketNumber, 'Trade')}
          {@const ticketAwaitingProposalDecision = isAwaitingProposalDecision(ticket)}
          {@const ticketAwaitingTradeDecision = isAwaitingTradeDecision(ticket)}
          {@const ticketAwaitingDecision = ticketAwaitingProposalDecision || ticketAwaitingTradeDecision}
          {@const foleoTraderOrder = foleoTraderOrderFor(ticket)}
          {@const foleoTraderStatus = foleoTraderStatusText(foleoTraderOrder)}
          {@const ticketCancelConfirming = cancelConfirmingTicketNumber === ticket.ticketNumber}
          {@const proposalInputActive = ticketEditingProposal && !ticketCancelConfirming && !ticketAwaitingDecision}
          {@const tradeInputActive = ticketEditingTrade && !ticketCancelConfirming && !ticketAwaitingDecision}
          {@const saveFormID = `ticket-save-${ticket.ticketNumber}`}
          {@const proposalAllocationTotal = liveProposalAllocationTotal(ticket)}
          {@const tradeAllocationTotal = liveTradeAllocationTotal(ticket)}
          {@const tradeAllocationRemaining = quantityDifference(proposalAllocationTotal, tradeAllocationTotal)}
          {@const fillTotal = quantityTotal(ticket.fills)}
          {@const fillRemaining = quantityDifference(tradeAllocationTotal, fillTotal)}
          {@const fillSettlementAmountTotal = settlementAmountTotal(ticket.fills)}
          {@const savedFillRemaining = quantityDifference(quantityTotal(ticket.tradeAllocations), fillTotal)}
          {@const savedFillSettlementAmountRemaining = quantityDifference(settlementAmountTotal(ticket.tradeAllocations), fillSettlementAmountTotal)}
          <article
            class="ticket-card"
            class:ticket-card-buy={ticket.side === 'Buy'}
            class:ticket-card-sell={ticket.side === 'Sell'}
            class:ticket-card-compact={!ticketExpanded}
          >
            <header class="ticket-header">
              <div class="ticket-header-main">
                <div class="ticket-title">
                  <span>#{ticket.ticketNumber}</span>
                  <span class:buy-side={ticket.side === 'Buy'} class:sell-side={ticket.side === 'Sell'}>{ticket.side}</span>
                  <span>{instrument}</span>
                  {#if instrumentFacts.length > 0}
                    <span class="ticket-instrument-meta">
                      {#each instrumentFacts as fact (fact.label)}
                        <span><strong>{fact.label}</strong> {fact.value}</span>
                      {/each}
                    </span>
                  {/if}
                </div>
              </div>
              <div class="ticket-card-actions">
                {#if ticketEditing && !ticketAwaitingDecision}
                  <form
                    class="ticket-save-form"
                    id={saveFormID}
                    method="POST"
                    action="?/saveTicketFields"
                    use:enhance={enhanceAction(`save-ticket-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <button class="ticket-action-button" type="submit" formnovalidate disabled={ticketCancelConfirming || submitting === `save-ticket-${ticket.ticketNumber}`}>
                      Save
                    </button>
                  </form>
                {/if}
                {#if canEditProposalTerms(ticket)}
                  <form
                    class="ticket-save-form"
                    method="POST"
                    action="?/proposalRequestDecision"
                    use:enhance={enhanceAction(`proposal-request-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <button class="ticket-action-button" type="submit" disabled={ticketCancelConfirming || ticketEditing || submitting === `proposal-request-${ticket.ticketNumber}` || !canRequestProposalDecision(ticket)}>
                      Request approval
                    </button>
                  </form>
                {/if}
                {#if ticket.stage === 'Trade' && ticket.tradeDecision === 'InProgress'}
                  <form
                    class="ticket-save-form"
                    method="POST"
                    action="?/tradeRequestDecision"
                    use:enhance={enhanceAction(`trade-request-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <button class="ticket-action-button" type="submit" disabled={ticketCancelConfirming || ticketEditing || submitting === `trade-request-${ticket.ticketNumber}` || !canRequestTradeDecision(ticket)}>
                      Request review
                    </button>
                  </form>
                {/if}
                {#if ticketAwaitingProposalDecision}
                  <form
                    class="ticket-save-form"
                    method="POST"
                    use:enhance={enhanceAction(`proposal-decision-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <button class="ticket-action-button" type="submit" formaction="?/proposalApprove" disabled={ticketCancelConfirming || submitting === `proposal-decision-${ticket.ticketNumber}`}>
                      Approve
                    </button>
                    <button class="ticket-action-button" type="submit" formaction="?/proposalNotApprove" disabled={ticketCancelConfirming || submitting === `proposal-decision-${ticket.ticketNumber}`}>
                      Decline
                    </button>
                  </form>
                {/if}
                {#if ticketAwaitingTradeDecision}
                  <form
                    class="ticket-save-form"
                    method="POST"
                    use:enhance={enhanceAction(`trade-decision-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <button class="ticket-action-button" type="submit" formaction="?/tradeApprove" disabled={ticketCancelConfirming || submitting === `trade-decision-${ticket.ticketNumber}` || !canApproveTrade(ticket)}>
                      Approve
                    </button>
                    <button class="ticket-action-button" type="submit" formaction="?/tradeNotApprove" formnovalidate disabled={ticketCancelConfirming || submitting === `trade-decision-${ticket.ticketNumber}`}>
                      Decline
                    </button>
                  </form>
                {/if}
                {#if !ticketAwaitingDecision && canSetProposalText(ticket)}
                  <button class="ticket-action-button" onclick={() => ticketEditingProposal ? stopTicketEdit() : startTicketEdit(ticket, 'Proposal')} type="button" disabled={ticketCancelConfirming || ticketEditingTrade}>
                    {ticketEditingProposal ? 'Cancel' : 'Edit Proposal'}
                  </button>
                {/if}
                {#if !ticketAwaitingDecision && canSetTradeText(ticket)}
                  <button class="ticket-action-button" onclick={() => ticketEditingTrade ? stopTicketEdit() : startTicketEdit(ticket, 'Trade')} type="button" disabled={ticketCancelConfirming || ticketEditingProposal}>
                    {ticketEditingTrade ? 'Cancel' : 'Edit Trade'}
                  </button>
                {/if}
                {#if !ticketAwaitingDecision}
                  <button class="ticket-action-button" onclick={() => startTicketCancellation(ticket.ticketNumber)} type="button" disabled={ticketCancelConfirming}>
                    Delete
                  </button>
                {/if}
                <button class="ticket-action-button" onclick={() => toggleHistory(ticket.ticketNumber)} type="button" disabled={ticketCancelConfirming}>
                  {openHistoryTicketNumber === ticket.ticketNumber ? 'Hide' : 'History'}
                </button>
                <button
                  aria-label={ticketExpanded ? 'Collapse ticket card' : 'Expand ticket card'}
                  class="ticket-action-button ticket-icon-button"
                  onclick={() => toggleTicketExpanded(ticket.ticketNumber)}
                  disabled={ticketCancelConfirming}
                  title={ticketExpanded ? 'Collapse' : 'Expand'}
                  type="button"
                >
                  <svg aria-hidden="true" viewBox="0 0 20 20">
                    {#if ticketExpanded}
                      <path d="M5 12.5 10 7.5l5 5" />
                    {:else}
                      <path d="M5 7.5 10 12.5l5-5" />
                    {/if}
                  </svg>
                </button>
              </div>
              {#if ticket.stage === 'Trade' && ticket.tradeDecision === 'InProgress' && canSendFoleoTrader(ticket, foleoTraderOrder)}
                <div class="trade-execution-groups">
                  <form
                    class="trade-execution-group"
                    method="POST"
                    action="?/sendFoleoTraderOrder"
                    use:enhance={enhanceAction(`foleo-trader-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <span class="trade-execution-label">FIX</span>
                    <BrokerDropdown
                      {brokers}
                      compactBrand
                      method="FIX"
                      name="brokerLEI"
                      placeholder="Select FIX broker"
                      bind:selectedBrokerLEI={() => fixBrokerByTicket[ticket.ticketNumber] ?? '', (lei) => selectFixBroker(ticket.ticketNumber, lei)}
                    />
                    <button class="house-button house-button-secondary house-button-md" type="submit" disabled={ticketCancelConfirming || ticketEditing || submitting === `foleo-trader-${ticket.ticketNumber}` || !fixBrokerByTicket[ticket.ticketNumber]}>
                      Send Trade
                    </button>
                  </form>
                  <form
                    class="trade-execution-group"
                    method="POST"
                    action="?/sendTradeFileTicket"
                    use:enhance={enhanceAction(`trade-file-ticket-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <span class="trade-execution-label">TradeFile</span>
                    <BrokerDropdown
                      {brokers}
                      compactBrand
                      method="TradeFile"
                      name="brokerLEI"
                      placeholder="Select TradeFile broker"
                      bind:selectedBrokerLEI={() => tradeFileBrokerByTicket[ticket.ticketNumber] ?? '', (lei) => selectTradeFileBroker(ticket.ticketNumber, lei)}
                    />
                    <button class="house-button house-button-secondary house-button-md" type="submit" disabled={ticketCancelConfirming || ticketEditing || submitting === `trade-file-ticket-${ticket.ticketNumber}` || !tradeFileBrokerByTicket[ticket.ticketNumber]}>
                      Send Trade
                    </button>
                  </form>
                </div>
              {/if}
            </header>

            {#if form?.message && formTicketNumber === ticket.ticketNumber}
              <div class="ticket-form-message error-message">
                {form.message}
              </div>
            {/if}

            {#if openHistoryTicketNumber === ticket.ticketNumber}
              {@const history = historyByTicketNumber[ticket.ticketNumber]}
              <section class="ticket-history-panel">
                <div>
                  {#if history?.loading}
                    <div class="ticket-history-muted">Loading history...</div>
                  {:else if history?.error}
                    <div class="ticket-history-error">{history.error}</div>
                  {:else}
                    <HistoryEventsCard
                      eventDateTime={data.valuationDate}
                      asAtDateTime={data.auditDateTime}
                      events={history?.events ?? []}
                      emptyMessage="No history found for this ticket."
                    />
                  {/if}
                </div>
              </section>
            {/if}

            {#if ticketCancelConfirming}
              <form
                class="ticket-cancel-confirmation danger-confirmation"
                method="POST"
                action="?/deleteTicket"
                use:enhance={enhanceAction(`delete-${ticket.ticketNumber}`)}
              >
                <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                <label>
                  <span>Type Delete to confirm</span>
                  <input class="input" autocomplete="off" bind:value={cancelConfirmationInput} spellcheck="false" />
                </label>
                <div class="danger-confirmation-actions">
                  <button disabled={!canConfirmTicketCancel || submitting === `delete-${ticket.ticketNumber}`} type="submit">Delete</button>
                  <button type="button" onclick={cancelTicketCancellation}>Keep ticket</button>
                </div>
              </form>
            {/if}

            {#if !ticketExpanded}
              <div class="ticket-compact-details">
                <span>{ticketStep(ticket)}</span>
                <span>{ticketDecisionDescription(ticket)}</span>
                <span class="compact-account-pill-row">
                  {#each ticket.accountIDs as accountID (accountID)}
                    <span class="compact-account-pill">{accountName(accountID)} {accountCurrency(accountID)}</span>
                  {:else}
                    <span class="compact-account-pill compact-account-pill-empty">No accounts</span>
                  {/each}
                </span>
              </div>
            {:else}
              <div class="ticket-detail-stack">
              <section class="workflow-panel workflow-panel-wide">
                <h2>Proposal</h2>
                <form class="ticket-main-form" method="POST" action="?/saveProposal" use:enhance={enhanceAction(`proposal-${ticket.ticketNumber}`)}>
                  <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                  <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                  <input type="hidden" name="hasProposal" value={ticket.proposalAllocations.length > 0 ? 'true' : 'false'} />
                  <input type="hidden" name="tradeCurrency" value={ticket.tradeCurrency} />
                  <div class="two-col">
                    <label class="field">
                      <span>Target price ({ticket.tradeCurrency})</span>
                      {#if proposalInputActive && canEditProposalTerms(ticket)}
                        <input form={saveFormID} class="input ticket-term-input" type="text" inputmode="decimal" pattern={decimalInputPattern} name="targetPrice" value={targetPriceInputValue(ticket)} oninput={cleanDecimalInput} />
                      {:else}
                        <span class="ticket-readonly-value">{targetPriceDisplay(ticket)}</span>
                      {/if}
                    </label>
                  </div>
                </form>
                <div class="ticket-note-grid">
                  <div class="ticket-text-set-form">
                    <label class="field ticket-textarea-field">
                      <span>Proposal reason</span>
                      <textarea form={saveFormID} class="input ticket-textarea" name="proposalReason" value={ticket.proposalReason} rows="1" {@attach autoResizeTextarea(ticket.proposalReason)} disabled={!proposalInputActive || !canSetProposalText(ticket)}></textarea>
                    </label>
                  </div>
                  <div class="ticket-text-set-form">
                    <label class="field ticket-textarea-field">
                      <span>Proposal allocation</span>
                      <textarea form={saveFormID} class="input ticket-textarea" name="proposalAllocation" value={ticket.proposalAllocation} rows="1" {@attach autoResizeTextarea(ticket.proposalAllocation)} disabled={!proposalInputActive || !canSetProposalText(ticket)}></textarea>
                    </label>
                  </div>
                </div>
              </section>

              <section class="workflow-panel workflow-panel-wide">
                <h2>Accounts</h2>
                {#if canEditProposalTerms(ticket)}
                  {@const accountsAvailableForAdd = availableAccounts(ticket)}
                  {@const accountOptionsForAdd = accountAddOptions(accountsAvailableForAdd)}
                  <form class="account-add-form" method="POST" action="?/addAccount" use:enhance={enhanceAction(`add-account-${ticket.ticketNumber}`)}>
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <ComplexSelect
                      ariaLabel="Accounts available to add"
                      class="ticket-account-add-select"
                      disabled={!proposalInputActive || accountsAvailableForAdd.length === 0}
                      emptyText="No accounts available"
                      multiple
                      name="accountIDs"
                      open={openAccountAddTicketNumber === ticket.ticketNumber}
                      onopenchange={(open) => handleAccountAddOpenChange(open, ticket.ticketNumber)}
                      options={accountOptionsForAdd}
                      placeholder="No accounts available"
                      searchPlaceholder="Search accounts"
                      showClear={false}
                      showSelectAll={false}
                      summary={accountsAvailableForAdd.length === 0 ? 'No accounts available' : 'Add accounts'}
                    />
                    <button class="house-button house-button-secondary house-button-md account-add-button" type="submit" disabled={!proposalInputActive || accountsAvailableForAdd.length === 0}>Add</button>
                  </form>
                {/if}
                <div class="account-allocation-table">
                  <div class="account-allocation-header">
                    <span>Account</span>
                    <span>Currency</span>
                    <span>Proposal quantity</span>
                    <span>Current quantity</span>
                    <span></span>
                  </div>
                  <div class="account-allocation-body">
                    {#each ticket.accountIDs as accountID (accountID)}
                      {@const allocation = ticket.proposalAllocations.find((item) => item.accountID === accountID)}
                      <div class="account-allocation-row">
                        <div class="account-allocation-account">
                          <span>{accountName(accountID)}</span>
                        </div>
                        <span class="account-allocation-currency">{accountCurrency(accountID)}</span>
                        <div class="account-allocation-quantity">
                          <input form={saveFormID} type="hidden" name="proposalAllocationAccountID" value={accountID} disabled={!proposalInputActive || !canEditProposal(ticket)} />
                          {#if proposalInputActive && canEditProposal(ticket)}
                            <input form={saveFormID} class="input ticket-term-input" type="text" inputmode="decimal" pattern={decimalInputPattern} name={`proposalQuantity-${accountID}`} value={proposalQuantityInputValue(ticket, accountID)} placeholder="Qty" oninput={(event) => updateProposalQuantityDraft(ticket.ticketNumber, accountID, event)} />
                          {:else}
                            <span class="ticket-readonly-value">{formattedDisplay(quantityText(allocation?.quantity))}</span>
                          {/if}
                        </div>
                        <span class="ticket-readonly-value account-current-quantity">-</span>
                        <form class="account-allocation-remove-form" method="POST" action="?/removeAccount" use:enhance={enhanceAction(`remove-account-${ticket.ticketNumber}-${accountID}`)}>
                          <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                          <input type="hidden" name="accountID" value={accountID} />
                          <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                          <button class="account-remove-button" type="submit" title="Remove account" disabled={!proposalInputActive}>Remove</button>
                        </form>
                      </div>
                    {:else}
                      <div class="account-allocation-empty">No accounts selected</div>
                    {/each}
                  </div>
                </div>
                <div class="quantity-balance">
                  <span>Proposal allocated {formattedDisplay(quantityText(proposalAllocationTotal))}</span>
                </div>
              </section>

              {#if ticket.stage === 'Trade' && !ticketEditingProposal}
                <div class="ticket-detail-row">
                <section class="workflow-panel workflow-panel-wide">
                  <h2>Trade</h2>
                  <form class="ticket-main-form trade-main-form" method="POST" action="?/saveTrade" use:enhance={enhanceAction(`trade-${ticket.ticketNumber}`)}>
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <input type="hidden" name="hasTrade" value={ticket.tradeAllocations.length > 0 ? 'true' : 'false'} />
                    <div class="trade-price-row">
                      <label class="field">
                        <span>Traded price ({ticket.tradeCurrency})</span>
                        {#if tradeInputActive && canEditTrade(ticket)}
                          <input form={saveFormID} class="input ticket-term-input" type="text" inputmode="decimal" pattern={decimalInputPattern} name="tradedPrice" value={priceText(ticket.tradePrice, ticket.tradeCurrency)} oninput={cleanDecimalInput} />
                        {:else}
                          <span class="ticket-readonly-value">{formattedDisplay(priceText(ticket.tradePrice, ticket.tradeCurrency))}</span>
                        {/if}
                      </label>
                      <label class="field">
                        <span>Trade date/time</span>
                        {#if tradeInputActive && canEditTrade(ticket)}
                          <DateTimeInput class="ticket-term-input" form={saveFormID} fullWidth name="tradeDateTime" onchange={(event) => updateTradeDateTimeDraft(ticket.ticketNumber, event)} required showShortcuts={false} step="1" value={tradeDateTimeInputValue(ticket)} />
                        {:else}
                          <span class="ticket-readonly-value">{tradeDateDisplay(ticket)}</span>
                        {/if}
                      </label>
                      <label class="field">
                        <span>Settlement date</span>
                        {#if tradeInputActive && canEditTrade(ticket)}
                          <input form={saveFormID} class="input ticket-term-input" name="settlementDate" type="date" value={settlementDateInputValue(ticket)} min={tradeSettlementMin(ticket)} onchange={(event) => updateSettlementDateDraft(ticket.ticketNumber, event)} required />
                        {:else}
                          <span class="ticket-readonly-value">{settlementDateDisplay(ticket)}</span>
                        {/if}
                      </label>
                    </div>
                    <div class="account-allocation-table trade-allocation-table">
                      <div class="account-allocation-header">
                        <span>Account</span>
                        <span>Trade allocation</span>
                        <span>Settlement amount</span>
                        <span>Cash holding</span>
                      </div>
                      <div class="account-allocation-body">
                        {#each ticket.accountIDs as accountID (accountID)}
                          {@const allocation = ticket.tradeAllocations.find((item) => item.accountID === accountID)}
                          {@const cashHoldings = cashInvestableHoldings(ticket, accountID)}
                          {@const selectedCashHoldingID = tradeCashHoldingInputValue(ticket, accountID, allocation?.cashHoldingID)}
                          <div class="account-allocation-row">
                            <div class="account-allocation-account">
                              <span>{accountName(accountID)}</span>
                            </div>
                            <input form={saveFormID} type="hidden" name="tradeAllocationAccountID" value={accountID} disabled={!tradeInputActive || !canEditTrade(ticket)} />
                            {#if tradeInputActive && canEditTrade(ticket)}
                              <input form={saveFormID} class="input" type="text" inputmode="decimal" pattern={decimalInputPattern} name={`tradeQuantity-${accountID}`} value={tradeQuantityInputValue(ticket, accountID)} placeholder="Qty" oninput={(event) => updateTradeQuantityDraft(ticket.ticketNumber, accountID, event)} />
                              <input form={saveFormID} class="input" type="text" inputmode="decimal" pattern={decimalInputPattern} name={`tradeSettlementAmount-${accountID}`} value={tradeSettlementAmountInputValue(ticket, accountID)} placeholder="Amount" oninput={(event) => updateTradeSettlementAmountDraft(ticket.ticketNumber, accountID, event)} />
                              <select form={saveFormID} class="input trade-cash-holding-select" name={`tradeCashHoldingID-${accountID}`} value={selectedCashHoldingID} disabled={cashHoldings.length === 0} onchange={(event) => updateTradeCashHoldingDraft(ticket.ticketNumber, accountID, event)} required>
                                <option value="">Cash holding</option>
                                {#each cashHoldings as holding (holding.holdingID)}
                                  <option value={holding.holdingID}>{cashHoldingLabel(holding)}</option>
                                {/each}
                              </select>
                            {:else}
                              <span class="ticket-readonly-value">{formattedDisplay(quantityText(allocation?.quantity))}</span>
                              <span class="ticket-readonly-value">{formattedDisplay(quantityText(allocation?.settlementAmount))}</span>
                              <span class="ticket-readonly-value">{selectedCashHoldingLabel(ticket, accountID, allocation?.cashHoldingID)}</span>
                            {/if}
                          </div>
                        {:else}
                          <div class="account-allocation-empty">No accounts selected</div>
                        {/each}
                      </div>
                    </div>
                    <div class="quantity-balance" class:quantity-balance-mismatch={!quantitiesMatch(proposalAllocationTotal, tradeAllocationTotal)}>
                      <span>Trade allocated {formattedDisplay(quantityText(tradeAllocationTotal))}</span>
                      <span>Proposal total {formattedDisplay(quantityText(proposalAllocationTotal))}</span>
                      <span>Remaining {formattedDisplay(quantityText(tradeAllocationRemaining))}</span>
                    </div>
                  </form>
                  <div class="ticket-note-grid">
                    <div class="ticket-text-set-form">
                      <label class="field ticket-textarea-field">
                        <span>Instruction notes</span>
                        <textarea form={saveFormID} class="input ticket-textarea" name="tradeInstructionNotes" value={ticket.tradeInstructionNotes} rows="1" {@attach autoResizeTextarea(ticket.tradeInstructionNotes)} disabled={!tradeInputActive || !canSetTradeText(ticket)}></textarea>
                      </label>
                    </div>
                    <div class="ticket-text-set-form">
                      <label class="field ticket-textarea-field">
                        <span>Progress notes</span>
                        <textarea form={saveFormID} class="input ticket-textarea" name="tradeProgressNotes" value={ticket.tradeProgressNotes} rows="1" {@attach autoResizeTextarea(ticket.tradeProgressNotes)} disabled={!tradeInputActive || !canSetTradeText(ticket)}></textarea>
                      </label>
                    </div>
                  </div>
                </section>

                <section class="workflow-panel">
                  <div class="fill-section-header">
                    <h2>Fills</h2>
                    {#if foleoTraderStatus}
                      <span class="quantity-balance-pill foleo-trader-fill-pill">{foleoTraderStatus}</span>
                    {/if}
                  </div>
                  <div class="fill-table">
                    <div class="fill-table-header">
                      <span>Broker</span>
                      <span>Price</span>
                      <span>Quantity</span>
                      <span>Settlement amount</span>
                      <span>Note</span>
                      <span></span>
                    </div>
                    <div class="fill-list">
                      {#each ticket.fills as fill (fill.fillID)}
                        <form class="fill-row" method="POST" action="?/removeFill" use:enhance={enhanceAction(`fill-${ticket.ticketNumber}-${fill.fillID}`)}>
                          <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                          <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                          <input type="hidden" name="fillID" value={fill.fillID} />
                          <span class="ticket-readonly-value">{brokerName(fill.brokerLEI)}</span>
                          <span class="ticket-readonly-value">{priceText(fill.price, ticket.tradeCurrency)}</span>
                          <span class="ticket-readonly-value">{decimalDisplay(fill.quantity)}</span>
                          <span class="ticket-readonly-value">{decimalDisplay(fill.settlementAmount)}</span>
                          <span class="ticket-readonly-value">{fill.note || '-'}</span>
                          <button type="submit" title="Remove fill" disabled={!tradeInputActive || !canEditFills(ticket)}>X</button>
                        </form>
                      {:else}
                        <div class="fill-empty">No fills</div>
                      {/each}
                    </div>
                  </div>
                  <div class="quantity-balance" class:quantity-balance-mismatch={!quantitiesMatch(tradeAllocationTotal, fillTotal)}>
                    <span>Filled {formattedDisplay(quantityText(fillTotal))}</span>
                    <span>Trade total {formattedDisplay(quantityText(tradeAllocationTotal))}</span>
                    <span>Remaining {formattedDisplay(quantityText(fillRemaining))}</span>
                  </div>
                  <form class="fill-form" method="POST" action="?/addFill" use:enhance={enhanceAction(`add-fill-${ticket.ticketNumber}`)}>
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    {#if tradeInputActive && canEditFills(ticket)}
                      <input type="hidden" name="fillPrice" value={ticket.tradePrice ?? ''} />
                      <span class="ticket-readonly-value fill-price-preview">Price {formattedDisplay(priceText(ticket.tradePrice, ticket.tradeCurrency))}</span>
                      <select class="input" name="brokerLEI" disabled={activeBrokers.length === 0} required>
                        <option value="">Broker</option>
                        {#each activeBrokers as broker (broker.lei)}
                          <option value={broker.lei}>{brokerLabel(broker)}</option>
                        {/each}
                      </select>
                      <input class="input" type="text" inputmode="numeric" pattern={integerInputPattern} name="fillQuantity" placeholder="Qty" oninput={cleanIntegerInput} required />
                      <input class="input" type="text" inputmode="decimal" pattern={decimalInputPattern} name="fillSettlementAmount" placeholder="Amount" oninput={cleanDecimalInput} required />
                      <input class="input" name="fillNote" placeholder="Note" />
                      <button class="house-button house-button-secondary house-button-sm" type="submit" disabled={activeBrokers.length === 0 || savedFillRemaining <= 0 || savedFillSettlementAmountRemaining <= 0 || ticket.tradePrice == null}>Add fill</button>
                    {:else}
                      <span class="ticket-readonly-value">Edit Trade to add fills</span>
                    {/if}
                  </form>
                </section>
                </div>
              {/if}
              </div>
            {/if}
          </article>
        {/each}
      </section>
    {/if}
  </section>
</main>
