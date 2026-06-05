<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import EventPropertyDetails from '$lib/components/EventPropertyDetails.svelte';
  import { formatDisplayDateTime, formatTableDateTime, toApiDateTime } from '$lib/dates';
  import type { Broker, Instrument, InstrumentPriceCash, InstrumentPriceEquity, InstrumentPriceFixedIncome, InstrumentValue, Ticket, TicketReferenceEvent, TicketSide, TicketStage } from '$lib/types';
  import type { SubmitFunction } from './$types';

  type TicketEditContext = 'Proposal' | 'Trade';

  let { data, form } = $props();

  const eventDateDefault = $derived(data.valuationDate);
  let submitting = $state('');
  let createTicketSide = $state<'' | 'Buy' | 'Sell'>('');
  let createTicketInstrument = $state('');
  let selectedSides = $state<TicketSide[]>([]);
  let selectedStages = $state<TicketStage[]>([]);
  let freeTextFilter = $state('');
  let ticketViewMode = $state<'Detailed' | 'Compact'>('Compact');
  let expandedTicketNumbers = $state<number[]>([]);
  let collapsedTicketNumbers = $state<number[]>([]);
  let editingTicket = $state<{ ticketNumber: number; context: TicketEditContext | '' }>({ ticketNumber: 0, context: '' });
  let cancelConfirmingTicketNumber = $state(0);
  let cancelConfirmationInput = $state('');
  let openHistoryTicketNumber = $state(0);
  let historyByTicketNumber = $state<Record<number, { events: TicketReferenceEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');
  const tickets = $derived(data.tickets?.items ?? []);
  const filteredTickets = $derived(tickets.filter(ticketMatchesFilters));
  const accounts = $derived(data.accounts?.items ?? []);
  const brokers = $derived(data.brokers?.items ?? []);
  const instruments = $derived(data.instruments?.items ?? []);
  const instrumentValues = $derived(data.instrumentValues?.items ?? []);
  const ticketSideOptions: TicketSide[] = ['Buy', 'Sell'];
  const decimalInputPattern = '(?:[0-9]+|[0-9]{1,3}(?:,[0-9]{3})+)(?:[.][0-9]+)?';
  const ticketStageOptions = $derived(data.ticketStageOptions ?? []);
  const activeAccounts = $derived(accounts.filter((account) => account.active));
  const activeBrokers = $derived([...brokers.filter((broker) => broker.active)].sort((left, right) => brokerLabel(left).localeCompare(brokerLabel(right))));
  const activeInstruments = $derived(instruments.filter((instrument) => instrument.active));
  const sortedInstruments = $derived(
    [...activeInstruments].sort((left, right) => instrumentLabel(left).localeCompare(instrumentLabel(right)))
  );
  const asOfSummary = $derived(data.auditDateTime && data.tickets ? formatDisplayDateTime(data.tickets.asOfDateTime) : 'now');
  const canCommitTicket = $derived(
    createTicketSide !== '' &&
    !!resolveInstrumentInput(createTicketInstrument) &&
    submitting !== 'createTicket'
  );
  const canConfirmTicketCancel = $derived(cancelConfirmationInput.trim().toLowerCase() === 'delete');

  $effect(() => {
    const nextHistoryContextKey = createHistoryContextKey();
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
    return () => {
      submitting = name;
      return async ({ result, update }) => {
        await update();
        submitting = '';
        if (result.type === 'success' && name.startsWith('save-ticket-'))
          stopTicketEdit();
        if (name.startsWith('delete-'))
          cancelTicketCancellation();
      };
    };
  };

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
    const target = priceText(ticket.proposalTargetPrice, ticket.tradeCurrency);
    const current = priceText(currentInstrumentPrice(ticket.instrumentID), ticket.tradeCurrency);

    if (!target)
      return 'Not set';

    return current ? `${target} (current ${current})` : target;
  }

  function quantityText(value: number | null | undefined) {
    return groupedDecimalText(value);
  }

  function quantityTotal(items: { quantity: number }[]) {
    return items.reduce((total, item) => total + item.quantity, 0);
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
      ticket.proposalAllocations.length > 0 &&
      quantitiesMatch(ticket.proposalTotalAmount, quantityTotal(ticket.proposalAllocations));
  }

  function canRequestProposalDecision(ticket: Ticket) {
    return ticket.stage === 'Proposal' &&
      ticket.proposalDecision === 'InProgress' &&
      ticket.proposalTargetPrice != null &&
      ticket.proposalTotalAmount != null &&
      ticket.proposalAllocations.length > 0 &&
      quantitiesMatch(ticket.proposalTotalAmount, quantityTotal(ticket.proposalAllocations));
  }

  function canEditTrade(ticket: Ticket) {
    return ticket.stage === 'Trade' && ticket.proposalDecision === 'Approved' && ticket.tradeDecision === 'InProgress';
  }

  function canSetTradeText(ticket: Ticket) {
    return ticket.stage === 'Trade';
  }

  function canEditFills(ticket: Ticket) {
    return ticket.stage === 'Trade';
  }

  function canApproveTrade(ticket: Ticket) {
    const tradeTotal = quantityTotal(ticket.tradeAllocations);

    return ticket.stage === 'Trade' &&
      ticket.tradeDecision === 'PendingApproval' &&
      ticket.tradeAllocations.length > 0 &&
      quantitiesMatch(ticket.proposalTotalAmount, tradeTotal) &&
      quantitiesMatch(tradeTotal, quantityTotal(ticket.fills));
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

  function createHistoryContextKey() {
    return [
      data.valuationDate,
      data.auditDateTime ?? '',
      data.tickets?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
  }

  function startTicketEdit(ticketNumber: number, context: TicketEditContext) {
    editingTicket = { ticketNumber, context };
    cancelTicketCancellation();
  }

  function stopTicketEdit() {
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

  function toggleSideFilter(side: TicketSide) {
    selectedSides = selectedSides.includes(side)
      ? selectedSides.filter((selectedSide) => selectedSide !== side)
      : [...selectedSides, side];
  }

  function clearStageFilters() {
    selectedStages = [];
  }

  function toggleStageFilter(stage: TicketStage) {
    selectedStages = selectedStages.includes(stage)
      ? selectedStages.filter((selectedStage) => selectedStage !== stage)
      : [...selectedStages, stage];
  }

  function ticketMatchesFilters(ticket: Ticket) {
    const sideMatches = selectedSides.length === 0 || selectedSides.includes(ticket.side);
    const stageMatches = selectedStages.length === 0 || selectedStages.includes(ticket.stage);
    const text = freeTextFilter.trim().toLowerCase();

    return sideMatches && stageMatches && (!text || ticketSearchText(ticket).includes(text));
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
    if (ticket.stage === 'Trade' || ticket.stage === 'Completed')
      return decisionDescription(ticket.tradeDecision);
    return decisionDescription(ticket.proposalDecision);
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
        `Total ${detailAmount(details, 'TotalAmount')}`,
        allocationCount(details)
      ].filter(Boolean).join(' · ');

    if (type === 'TicketTradeCreatedEvent' || type === 'TicketTradeModifiedEvent')
      return [`Price ${detailAmount(details, 'TradedPrice')}`, allocationCount(details)].filter(Boolean).join(' · ');

    if (type === 'TicketTradeFillAddedEvent' || type === 'TicketTradeFillModifiedEvent')
      return [`Price ${readDetailValue(details, 'Price')}`, `Quantity ${readDetailValue(details, 'Quantity')}`, readDetailString(details, 'Note')].filter(Boolean).join(' · ');

    if (type === 'TicketTradeFillRemovedEvent')
      return `Fill ${readDetailString(details, 'FillID')}`;

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

  function readDetailString(details: Record<string, unknown>, key: string) {
    const value = details[key] ?? details[key.charAt(0).toLowerCase() + key.slice(1)];
    return typeof value === 'string' ? value : '';
  }

  function readDetailValue(details: Record<string, unknown>, key: string) {
    const value = details[key] ?? details[key.charAt(0).toLowerCase() + key.slice(1)];
    return typeof value === 'string' || typeof value === 'number' ? value.toString() : '';
  }

</script>

<main class="min-h-screen">
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
            {filteredTickets.length} active ticket{filteredTickets.length === 1 ? '' : 's'} · as of {asOfSummary}
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section space-y-6">
    <AggregateUpdateWatcher aggregateKind="Tickets" valuationDate={data.valuationDate} lastEventID={data.tickets?.lastEventID ?? null} auditDateTime={data.auditDateTime} />

    {#if data.error}
      <div class="rounded border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{data.error}</div>
    {/if}

    {#if form?.message}
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
        <label class="create-ticket-field">
          <span>Instrument</span>
          <input
            class="h-9 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-indigo-600 focus:ring-2 focus:ring-indigo-600/20"
            bind:value={createTicketInstrument}
            list="ticket-instrument-options"
            name="instrumentID"
            placeholder="Search instruments"
            required
          />
          <datalist id="ticket-instrument-options">
            {#each sortedInstruments as instrument (instrument.instrumentID)}
              <option label={instrumentLabel(instrument)} value={instrumentLabel(instrument)}></option>
            {/each}
          </datalist>
        </label>
        <button class="commit-ticket-button" type="submit" disabled={!canCommitTicket}>
          Commit
        </button>
      </form>
    </section>

    <section class="section-band create-ticket-card create-ticket-action-card">
      <h2 class="create-ticket-title">Filter</h2>
      <div class="ticket-filter-layout">
        <div class="side-radio-group ticket-filter-group ticket-side-filter-group" role="group" aria-label="Ticket side filters">
          <label class="side-radio-pill ticket-filter-pill">
            <input checked={selectedSides.length === 0} name="ticketSideFilterAll" type="checkbox" onchange={clearSideFilters} />
            <span>All</span>
          </label>
          {#each ticketSideOptions as side (side)}
            <label class="side-radio-pill ticket-filter-pill">
              <input
                checked={selectedSides.includes(side)}
                name={`ticketSideFilter-${side}`}
                type="checkbox"
                onchange={() => toggleSideFilter(side)}
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
        </div>
        <label class="create-ticket-field ticket-text-filter">
          <input
            aria-label="Filter tickets"
            class="h-9 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-indigo-600 focus:ring-2 focus:ring-indigo-600/20"
            bind:value={freeTextFilter}
            placeholder="Filter tickets"
            type="search"
          />
        </label>
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
    </section>

    {#if filteredTickets.length === 0}
      <section class="empty-state">{tickets.length === 0 ? 'No active tickets.' : 'No tickets match the selected filters.'}</section>
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
          {@const ticketAwaitingProposalDecision = canApproveProposal(ticket)}
          {@const ticketAwaitingTradeDecision = canApproveTrade(ticket)}
          {@const ticketAwaitingDecision = ticketAwaitingProposalDecision || ticketAwaitingTradeDecision}
          {@const ticketCancelConfirming = cancelConfirmingTicketNumber === ticket.ticketNumber}
          {@const proposalInputActive = ticketEditingProposal && !ticketCancelConfirming && !ticketAwaitingDecision}
          {@const tradeInputActive = ticketEditingTrade && !ticketCancelConfirming && !ticketAwaitingDecision}
          {@const saveFormID = `ticket-save-${ticket.ticketNumber}`}
          {@const proposalAllocationTotal = quantityTotal(ticket.proposalAllocations)}
          {@const proposalAllocationRemaining = quantityDifference(ticket.proposalTotalAmount, proposalAllocationTotal)}
          {@const tradeAllocationTotal = quantityTotal(ticket.tradeAllocations)}
          {@const tradeAllocationRemaining = quantityDifference(ticket.proposalTotalAmount, tradeAllocationTotal)}
          {@const fillTotal = quantityTotal(ticket.fills)}
          {@const fillRemaining = quantityDifference(tradeAllocationTotal, fillTotal)}
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
                {#if ticketExpanded && ticketEditing && !ticketAwaitingDecision}
                  <form
                    class="ticket-save-form"
                    id={saveFormID}
                    method="POST"
                    action="?/saveTicketFields"
                    use:enhance={enhanceAction(`save-ticket-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <button class="ticket-action-button" type="submit" disabled={ticketCancelConfirming || submitting === `save-ticket-${ticket.ticketNumber}`}>
                      Save
                    </button>
                  </form>
                {/if}
                {#if ticketExpanded && canEditProposalTerms(ticket)}
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
                {#if ticketExpanded && ticketAwaitingProposalDecision}
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
                {#if ticketExpanded && ticketAwaitingTradeDecision}
                  <form
                    class="ticket-save-form"
                    method="POST"
                    use:enhance={enhanceAction(`trade-decision-${ticket.ticketNumber}`)}
                  >
                    <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                    <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                    <button class="ticket-action-button" type="submit" formaction="?/tradeApprove" disabled={ticketCancelConfirming || submitting === `trade-decision-${ticket.ticketNumber}`}>
                      Approve
                    </button>
                    <button class="ticket-action-button" type="submit" formaction="?/tradeNotApprove" disabled={ticketCancelConfirming || submitting === `trade-decision-${ticket.ticketNumber}`}>
                      Decline
                    </button>
                  </form>
                {/if}
                {#if ticketExpanded && !ticketAwaitingDecision && canSetProposalText(ticket)}
                  <button class="ticket-action-button" onclick={() => ticketEditingProposal ? stopTicketEdit() : startTicketEdit(ticket.ticketNumber, 'Proposal')} type="button" disabled={ticketCancelConfirming || ticketEditingTrade}>
                    {ticketEditingProposal ? 'Cancel' : 'Edit Proposal'}
                  </button>
                {/if}
                {#if ticketExpanded && !ticketAwaitingDecision && canSetTradeText(ticket)}
                  <button class="ticket-action-button" onclick={() => ticketEditingTrade ? stopTicketEdit() : startTicketEdit(ticket.ticketNumber, 'Trade')} type="button" disabled={ticketCancelConfirming || ticketEditingProposal}>
                    {ticketEditingTrade ? 'Cancel' : 'Edit Trade'}
                  </button>
                {/if}
                {#if ticketExpanded && !ticketAwaitingDecision}
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
            </header>

            {#if openHistoryTicketNumber === ticket.ticketNumber}
              {@const history = historyByTicketNumber[ticket.ticketNumber]}
              <section class="ticket-history-panel">
                <div class="ticket-history-card">
                  <div class="ticket-history-header">
                    <h2>Ticket #{ticket.ticketNumber} history</h2>
                    <span>
                      {history?.events.length ?? 0} events
                      {#if data.auditDateTime && history?.events.length}
                        · {history.events.filter((event) => event.applicationStatus === 'omitted').length} omitted
                      {/if}
                    </span>
                  </div>

                  {#if history?.loading}
                    <div class="ticket-history-muted">Loading history...</div>
                  {:else if history?.error}
                    <div class="ticket-history-error">{history.error}</div>
                  {:else if history?.events.length}
                    <ol class="ticket-history-list">
                      {#each history.events as event (event.eventID)}
                        <li class:ticket-history-event-omitted={event.applicationStatus === 'omitted'}>
                          <div class="ticket-history-time">
                            <div>{formatTableDateTime(event.eventDateTime)}</div>
                            <div>Audit {formatTableDateTime(event.auditDateTime)}</div>
                          </div>
                          <div class="ticket-history-body">
                            <div class="ticket-history-event-title">
                              <span class="ticket-history-type">{event.$type}</span>
                              {#if event.applicationStatus === 'omitted'}
                                <span class="ticket-history-status ticket-history-status-omitted">Not applied</span>
                              {:else if data.auditDateTime}
                                <span class="ticket-history-status ticket-history-status-applied">Applied</span>
                              {/if}
                              <span class="ticket-history-event-id">{event.eventID}</span>
                            </div>
                            {#if ticketHistorySummary(event)}
                              <div class="ticket-history-summary">{ticketHistorySummary(event)}</div>
                            {/if}
                            <EventPropertyDetails details={event.propertyDetails} />
                            {#if event.applicationStatus === 'omitted'}
                              <div class="ticket-history-omitted-note">
                                Omitted from this view because its audit time is after the selected as-at date.
                              </div>
                            {/if}
                            <div class="ticket-history-reason">{event.reason}</div>
                          </div>
                        </li>
                      {/each}
                    </ol>
                  {:else}
                    <div class="ticket-history-muted">No history found for this ticket.</div>
                  {/if}
                </div>
              </section>
            {/if}

            {#if ticketExpanded && ticketCancelConfirming}
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
                        <input form={saveFormID} class="input ticket-term-input" type="text" inputmode="decimal" pattern={decimalInputPattern} name="targetPrice" value={priceText(ticket.proposalTargetPrice, ticket.tradeCurrency)} oninput={cleanDecimalInput} />
                      {:else}
                        <span class="ticket-readonly-value">{targetPriceDisplay(ticket)}</span>
                      {/if}
                    </label>
                    <label class="field">
                      <span>Total quantity</span>
                      {#if proposalInputActive && canEditProposalTerms(ticket)}
                        <input form={saveFormID} class="input ticket-term-input" type="text" inputmode="decimal" pattern={decimalInputPattern} name="totalAmount" value={quantityText(ticket.proposalTotalAmount)} oninput={cleanDecimalInput} />
                      {:else}
                        <span class="ticket-readonly-value">{formattedDisplay(quantityText(ticket.proposalTotalAmount))}</span>
                      {/if}
                    </label>
                  </div>
                </form>
                <div class="ticket-note-grid">
                  <div class="ticket-text-set-form">
                    <label class="field ticket-textarea-field">
                      <span>Proposal reason</span>
                      <textarea form={saveFormID} class="input ticket-textarea" name="proposalReason" value={ticket.proposalReason} disabled={!proposalInputActive || !canSetProposalText(ticket)}></textarea>
                    </label>
                  </div>
                  <div class="ticket-text-set-form">
                    <label class="field ticket-textarea-field">
                      <span>Proposal allocation</span>
                      <textarea form={saveFormID} class="input ticket-textarea" name="proposalAllocation" value={ticket.proposalAllocation} disabled={!proposalInputActive || !canSetProposalText(ticket)}></textarea>
                    </label>
                  </div>
                </div>
              </section>

              <section class="workflow-panel workflow-panel-wide">
                <h2>Accounts</h2>
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
                            <input form={saveFormID} class="input ticket-term-input" type="text" inputmode="decimal" pattern={decimalInputPattern} name={`proposalQuantity-${accountID}`} value={quantityText(allocation?.quantity)} placeholder="Qty" oninput={cleanDecimalInput} />
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
                  {#if canEditProposalTerms(ticket)}
                    <form class="account-allocation-row account-allocation-add-row" method="POST" action="?/addAccount" use:enhance={enhanceAction(`add-account-${ticket.ticketNumber}`)}>
                      <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                      <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                      <select class="input account-add-select" name="accountID" disabled={!proposalInputActive || availableAccounts(ticket).length === 0} required>
                        <option value="">Add account</option>
                        {#each availableAccounts(ticket) as account (account.accountID)}
                          <option value={account.accountID}>{account.name} {account.bookCurrency}</option>
                        {/each}
                      </select>
                      <span class="account-allocation-currency account-add-currency">-</span>
                      <span class="account-add-quantity-hint">Quantity can be entered after the account is added</span>
                      <span class="account-current-quantity-placeholder">-</span>
                      <button class="btn btn-secondary account-add-button" type="submit" disabled={!proposalInputActive || availableAccounts(ticket).length === 0}>Add</button>
                    </form>
                  {/if}
                </div>
                <div class="quantity-balance" class:quantity-balance-mismatch={!quantitiesMatch(ticket.proposalTotalAmount, proposalAllocationTotal)}>
                  <span>Proposal allocated {formattedDisplay(quantityText(proposalAllocationTotal))}</span>
                  <span>Total {formattedDisplay(quantityText(ticket.proposalTotalAmount))}</span>
                  <span>Remaining {formattedDisplay(quantityText(proposalAllocationRemaining))}</span>
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
                    <div class="two-col">
                      <label class="field">
                        <span>Traded price ({ticket.tradeCurrency})</span>
                        {#if tradeInputActive && canEditTrade(ticket)}
                          <input form={saveFormID} class="input ticket-term-input" type="text" inputmode="decimal" pattern={decimalInputPattern} name="tradedPrice" value={priceText(ticket.tradePrice, ticket.tradeCurrency)} oninput={cleanDecimalInput} />
                        {:else}
                          <span class="ticket-readonly-value">{formattedDisplay(priceText(ticket.tradePrice, ticket.tradeCurrency))}</span>
                        {/if}
                      </label>
                    </div>
                    <div class="account-allocation-table trade-allocation-table">
                      <div class="account-allocation-header">
                        <span>Account</span>
                        <span>Trade allocation</span>
                        <span>Book cost</span>
                      </div>
                      <div class="account-allocation-body">
                        {#each ticket.accountIDs as accountID (accountID)}
                          {@const allocation = ticket.tradeAllocations.find((item) => item.accountID === accountID)}
                          <div class="account-allocation-row">
                            <div class="account-allocation-account">
                              <span>{accountName(accountID)}</span>
                            </div>
                            <input form={saveFormID} type="hidden" name="tradeAllocationAccountID" value={accountID} disabled={!tradeInputActive || !canEditTrade(ticket)} />
                            {#if tradeInputActive && canEditTrade(ticket)}
                              <input form={saveFormID} class="input" type="text" inputmode="decimal" pattern={decimalInputPattern} name={`tradeQuantity-${accountID}`} value={quantityText(allocation?.quantity)} placeholder="Qty" oninput={cleanDecimalInput} />
                              <input form={saveFormID} class="input" type="text" inputmode="decimal" pattern={decimalInputPattern} name={`tradeBookCost-${accountID}`} value={quantityText(allocation?.bookCost)} placeholder="Book" oninput={cleanDecimalInput} />
                            {:else}
                              <span class="ticket-readonly-value">{formattedDisplay(quantityText(allocation?.quantity))}</span>
                              <span class="ticket-readonly-value">{formattedDisplay(quantityText(allocation?.bookCost))}</span>
                            {/if}
                          </div>
                        {:else}
                          <div class="account-allocation-empty">No accounts selected</div>
                        {/each}
                      </div>
                    </div>
                    <div class="quantity-balance" class:quantity-balance-mismatch={!quantitiesMatch(ticket.proposalTotalAmount, tradeAllocationTotal)}>
                      <span>Trade allocated {formattedDisplay(quantityText(tradeAllocationTotal))}</span>
                      <span>Total {formattedDisplay(quantityText(ticket.proposalTotalAmount))}</span>
                      <span>Remaining {formattedDisplay(quantityText(tradeAllocationRemaining))}</span>
                    </div>
                  </form>
                  <div class="ticket-note-grid">
                    <div class="ticket-text-set-form">
                      <label class="field ticket-textarea-field">
                        <span>Instruction notes</span>
                        <textarea form={saveFormID} class="input ticket-textarea" name="tradeInstructionNotes" value={ticket.tradeInstructionNotes} disabled={!tradeInputActive || !canSetTradeText(ticket)}></textarea>
                      </label>
                    </div>
                    <div class="ticket-text-set-form">
                      <label class="field ticket-textarea-field">
                        <span>Progress notes</span>
                        <textarea form={saveFormID} class="input ticket-textarea" name="tradeProgressNotes" value={ticket.tradeProgressNotes} disabled={!tradeInputActive || !canSetTradeText(ticket)}></textarea>
                      </label>
                    </div>
                  </div>
                </section>

                <section class="workflow-panel">
                  <h2>Fills</h2>
                  <div class="fill-table">
                    <div class="fill-table-header">
                      <span>Broker</span>
                      <span>Quantity</span>
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
                          <span class="ticket-readonly-value">{decimalDisplay(fill.quantity)}</span>
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
                      <input type="hidden" name="fillPrice" value={priceText(ticket.tradePrice, ticket.tradeCurrency)} />
                      <select class="input" name="brokerLEI" disabled={activeBrokers.length === 0} required>
                        <option value="">Broker</option>
                        {#each activeBrokers as broker (broker.lei)}
                          <option value={broker.lei}>{brokerLabel(broker)}</option>
                        {/each}
                      </select>
                      <input class="input" type="text" inputmode="decimal" pattern={decimalInputPattern} name="fillQuantity" placeholder="Qty" oninput={cleanDecimalInput} required />
                      <input class="input" name="fillNote" placeholder="Note" />
                      <button class="btn btn-secondary" type="submit" disabled={activeBrokers.length === 0 || fillRemaining <= 0 || ticket.tradePrice == null}>Add fill</button>
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
