import { clampFutureInputDateTime, dateInputToApiStartOfDay, todayEndForInput, toApiDateTime } from '$lib/dates';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getAccounts,
  getActiveTradeFiles,
  getBrokers,
  getFoleoTraderOrders,
  getHoldings,
  getInstrumentValues,
  getInstruments,
  getTicketStageOptions,
  getTickets,
  postTicketAccountAddedEvent,
  postTicketAccountRemovedEvent,
  postTicketCancelledEvent,
  postTicketCreatedEvent,
  postTicketProposalApprovedEvent,
  postTicketProposalAllocationSetEvent,
  postTicketProposalCreatedEvent,
  postTicketProposalDecisionRequestedEvent,
  postTicketProposalModifiedEvent,
  postTicketProposalNotApprovedEvent,
  postTicketProposalReasonSetEvent,
  postTicketTradeApprovedEvent,
  postTicketTradeCreatedEvent,
  postTicketTradeFillAddedEvent,
  postTicketTradeFillRemovedEvent,
  postTicketTradeDecisionRequestedEvent,
  postTicketTradeInstructionNotesSetEvent,
  postTicketTradeModifiedEvent,
  postTicketTradeNotApprovedEvent,
  postTicketTradeProgressNotesSetEvent,
  postFoleoTraderOrder,
  postTradeFilePending,
  postTradeFileRequest,
  type TicketProposalRequest,
  type TicketTextSetRequest,
  type TicketTradeRequest
} from '$lib/server/api';
import type { Account, Instrument, Ticket, TicketProposalAllocation, TicketSide, TicketTradeAllocation } from '$lib/types';
import { fail } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
  const valuationDate = todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const eventDateTime = toApiDateTime(valuationDate);
  const asOfDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const [tickets, accounts, brokers, holdings, instruments, instrumentValues, ticketStageOptions, foleoTraderOrders, tradeFiles] = await Promise.all([
      getTickets(fetch, eventDateTime, asOfDateTime, true),
      getAccounts(fetch, eventDateTime, asOfDateTime),
      getBrokers(fetch, eventDateTime, asOfDateTime),
      getHoldings(fetch, eventDateTime, asOfDateTime, { holdingKind: 'CashInvestable', includeInactive: false }),
      getInstruments(fetch, eventDateTime, asOfDateTime),
      getInstrumentValues(fetch, eventDateTime, asOfDateTime),
      getTicketStageOptions(fetch),
      getFoleoTraderOrders(fetch, eventDateTime, asOfDateTime),
      getActiveTradeFiles(fetch)
    ]);

    return {
      accounts,
      auditDateTime,
      brokers,
      error: '',
      foleoTraderOrders,
      holdings,
      instruments,
      instrumentValues,
      ticketStageOptions,
      tickets,
      tradeFiles,
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      auditDateTime,
      brokers: null,
      error: error instanceof Error ? error.message : 'Unable to load the blotter.',
      foleoTraderOrders: null,
      holdings: null,
      instruments: null,
      instrumentValues: null,
      ticketStageOptions: [],
      tickets: null,
      tradeFiles: [],
      valuationDate
    };
  }
};

export const actions: Actions = {
  createTicket: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const side = getFormString(formData, 'side') as TicketSide;
    const instrumentValue = getFormString(formData, 'instrumentID');

    if (!side || !instrumentValue)
      return fail(400, responseFailure('createTicket', 'Side and instrument are required.'));

    try {
      const apiEventDateTime = new Date().toISOString();
      const instruments = await getInstruments(fetch, apiEventDateTime, null);
      const instrument = resolveInstrument(instruments.items, instrumentValue);

      if (!instrument)
        return fail(400, responseFailure('createTicket', 'Select a valid instrument.'));

      const result = await postTicketCreatedEvent(fetch, {
        eventDateTime: apiEventDateTime,
        instrumentID: instrument.instrumentID,
        reason: `Create ${side.toLowerCase()} ticket`,
        side,
        userID: currentUser.userID
      });

      return responseSuccess('createTicket', 'Ticket created.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('createTicket', readError(error)));
    }
  },

  addAccount: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const accountIDs = uniqueStrings([
      ...getFormStrings(formData, 'accountIDs'),
      getFormString(formData, 'accountID')
    ]);
    const eventDateTime = getFormString(formData, 'eventDateTime');

    if (!ticketNumber || accountIDs.length === 0 || !eventDateTime)
      return fail(400, responseFailure('addAccount', 'Ticket, at least one account, and event date are required.'));

    try {
      const eventIDs: string[] = [];
      const apiEventDateTime = toApiDateTime(eventDateTime);

      for (const accountID of accountIDs) {
        const result = await postTicketAccountAddedEvent(fetch, {
          accountID,
          eventDateTime: apiEventDateTime,
          reason: `Add account to ticket ${ticketNumber}`,
          ticketNumber,
          userID: currentUser.userID
        });

        eventIDs.push(result.eventID);
      }

      return responseSuccess('addAccount', accountIDs.length === 1 ? 'Account added.' : `${accountIDs.length} accounts added.`, eventIDs.at(-1) ?? '');
    } catch (error) {
      return fail(502, responseFailure('addAccount', readError(error)));
    }
  },

  removeAccount: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const accountID = getFormString(formData, 'accountID');
    const eventDateTime = getFormString(formData, 'eventDateTime');

    if (!ticketNumber || !accountID || !eventDateTime)
      return fail(400, responseFailure('removeAccount', 'Ticket, account, and event date are required.'));

    try {
      const result = await postTicketAccountRemovedEvent(fetch, {
        accountID,
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `Remove account from ticket ${ticketNumber}`,
        ticketNumber,
        userID: currentUser.userID
      });

      return responseSuccess('removeAccount', 'Account removed.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('removeAccount', readError(error)));
    }
  },

  saveProposal: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const targetPrice = getFormNumber(formData, 'targetPrice');
    const tradeCurrency = getFormString(formData, 'tradeCurrency');
    const allocations = getProposalAllocations(formData);
    const hasProposal = getFormString(formData, 'hasProposal') === 'true';

    if (!ticketNumber || !eventDateTime || !targetPrice || allocations.length === 0)
      return fail(400, responseFailure('saveProposal', 'Proposal price and allocations are required.'));

    const ticketProposalRequest: TicketProposalRequest = {
      allocations,
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `${hasProposal ? 'Modify' : 'Create'} proposal for ticket ${ticketNumber}`,
      targetPrice,
      ticketNumber,
      tradeCurrency: tradeCurrency || undefined,
      userID: currentUser.userID
    };

    try {
      const result = hasProposal
        ? await postTicketProposalModifiedEvent(fetch, ticketProposalRequest)
        : await postTicketProposalCreatedEvent(fetch, ticketProposalRequest);

      return responseSuccess('saveProposal', 'Proposal saved.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('saveProposal', readError(error)));
    }
  },

  saveTicketFields: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');

    if (!ticketNumber || !eventDateTime)
      return fail(400, responseFailure('saveTicketFields', 'Ticket and event date are required.'));

    const apiEventDateTime = toApiDateTime(eventDateTime);

    try {
      const tickets = await getTickets(fetch, apiEventDateTime, null, true);
      const ticket = tickets.items.find((item) => item.ticketNumber === ticketNumber);

      if (!ticket)
        return fail(404, responseFailure('saveTicketFields', `No matching ticket found for TicketNumber '${ticketNumber}'.`, ticketNumber));

      const eventIDs: string[] = [];

      if (hasProposalFields(formData)) {
        const targetPrice = submittedOrCurrentNumber(formData, 'targetPrice', ticket.proposalTargetPrice);
        const allocations = formData.has('proposalAllocationAccountID')
          ? getProposalAllocations(formData, 'proposalAllocationAccountID')
          : ticket.proposalAllocations;

        if (!numbersEqual(targetPrice, ticket.proposalTargetPrice) ||
            !proposalAllocationsEqual(allocations, ticket.proposalAllocations)) {
          if (!targetPrice || allocations.length === 0)
            return fail(400, responseFailure('saveTicketFields', 'Proposal price and allocations are required.', ticketNumber));

          const proposalRequest: TicketProposalRequest = {
            allocations,
            eventDateTime: apiEventDateTime,
            reason: `${ticket.proposalAllocations.length > 0 ? 'Modify' : 'Create'} proposal for ticket ${ticketNumber}`,
            targetPrice,
            ticketNumber,
            tradeCurrency: ticket.tradeCurrency,
            userID: currentUser.userID
          };
          const result = ticket.proposalAllocations.length > 0
            ? await postTicketProposalModifiedEvent(fetch, proposalRequest)
            : await postTicketProposalCreatedEvent(fetch, proposalRequest);

          eventIDs.push(result.eventID);
        }
      }

      if (changedTextField(formData, ticket, 'proposalReason')) {
        const result = await postTicketProposalReasonSetEvent(fetch, {
          eventDateTime: apiEventDateTime,
          reason: `Set proposal reason for ticket ${ticketNumber}`,
          ticketNumber,
          userID: currentUser.userID,
          value: getFormString(formData, 'proposalReason')
        });

        eventIDs.push(result.eventID);
      }

      if (changedTextField(formData, ticket, 'proposalAllocation')) {
        const result = await postTicketProposalAllocationSetEvent(fetch, {
          eventDateTime: apiEventDateTime,
          reason: `Set proposal allocation for ticket ${ticketNumber}`,
          ticketNumber,
          userID: currentUser.userID,
          value: getFormString(formData, 'proposalAllocation')
        });

        eventIDs.push(result.eventID);
      }

      if (hasTradeFields(formData)) {
        const tradedPrice = submittedOrCurrentNumber(formData, 'tradedPrice', ticket.tradePrice);
        const tradeDateTime = submittedOrCurrentString(formData, 'tradeDateTime', ticket.tradeDateTime ?? '');
        const settlementDate = getSettlementDate(formData, ticket.settlementDateTime ?? '');
        const allocations = formData.has('tradeAllocationAccountID')
          ? getTradeAllocations(formData, 'tradeAllocationAccountID')
          : ticket.tradeAllocations;

        if (!numbersEqual(tradedPrice, ticket.tradePrice) ||
            !dateTimesEqual(tradeDateTime, ticket.tradeDateTime) ||
            !settlementDatesEqual(settlementDate, ticket.settlementDateTime) ||
            !tradeAllocationsEqual(allocations, ticket.tradeAllocations)) {
          if (!tradedPrice || !tradeDateTime || !settlementDate || allocations.length === 0)
            return fail(400, responseFailure('saveTicketFields', 'Trade price, trade date, settlement date, and allocations are required.', ticketNumber));
          const settlementDateError = validateSettlementDate(settlementDate, tradeDateTime);
          if (settlementDateError)
            return fail(400, responseFailure('saveTicketFields', settlementDateError, ticketNumber));

          const tradeTotal = allocationTotal(ticket.proposalAllocations);
          const tradeTotalError = validateAllocationTotal(allocations, tradeTotal, 'Trade allocations');
          if (tradeTotalError)
            return fail(400, responseFailure('saveTicketFields', tradeTotalError, ticketNumber));
          const accounts = await getAccounts(fetch, apiEventDateTime, null);
          const cashHoldingError = validateTradeCashHoldings(allocations, accounts.items);
          if (cashHoldingError)
            return fail(400, responseFailure('saveTicketFields', cashHoldingError, ticketNumber));

          const tradeRequest: TicketTradeRequest = {
            allocations: toTradeRequestAllocations(allocations),
            eventDateTime: apiEventDateTime,
            reason: `${ticket.tradeAllocations.length > 0 ? 'Modify' : 'Create'} trade for ticket ${ticketNumber}`,
            settlementDateTime: dateInputToApiStartOfDay(settlementDate),
            ticketNumber,
            tradeDateTime: toApiDateTime(tradeDateTime),
            tradedPrice,
            userID: currentUser.userID
          };
          const result = ticket.tradeAllocations.length > 0
            ? await postTicketTradeModifiedEvent(fetch, tradeRequest)
            : await postTicketTradeCreatedEvent(fetch, tradeRequest);

          eventIDs.push(result.eventID);
        }
      }

      if (changedTextField(formData, ticket, 'tradeInstructionNotes')) {
        const result = await postTicketTradeInstructionNotesSetEvent(fetch, {
          eventDateTime: apiEventDateTime,
          reason: `Set trade instruction notes for ticket ${ticketNumber}`,
          ticketNumber,
          userID: currentUser.userID,
          value: getFormString(formData, 'tradeInstructionNotes')
        });

        eventIDs.push(result.eventID);
      }

      if (changedTextField(formData, ticket, 'tradeProgressNotes')) {
        const result = await postTicketTradeProgressNotesSetEvent(fetch, {
          eventDateTime: apiEventDateTime,
          reason: `Set trade progress notes for ticket ${ticketNumber}`,
          ticketNumber,
          userID: currentUser.userID,
          value: getFormString(formData, 'tradeProgressNotes')
        });

        eventIDs.push(result.eventID);
      }

      return responseSuccess(
        'saveTicketFields',
        eventIDs.length === 0 ? 'No changes to save.' : 'Changed fields saved.',
        eventIDs[eventIDs.length - 1] ?? ''
      );
    } catch (error) {
      const accounts = await getAccounts(fetch, apiEventDateTime, null).catch(() => null);
      return fail(502, responseFailure('saveTicketFields', friendlyTradeCashHoldingError(readError(error), accounts?.items ?? []), ticketNumber));
    }
  },

  setProposalReason: async ({ fetch, locals, request }) => setTicketText(
    fetch,
    request,
    requireCurrentUser(locals).userID,
    'proposalReason',
    'setProposalReason',
    'Set proposal reason',
    'Proposal reason saved.',
    postTicketProposalReasonSetEvent
  ),

  setProposalAllocation: async ({ fetch, locals, request }) => setTicketText(
    fetch,
    request,
    requireCurrentUser(locals).userID,
    'proposalAllocation',
    'setProposalAllocation',
    'Set proposal allocation',
    'Proposal allocation saved.',
    postTicketProposalAllocationSetEvent
  ),

  proposalRequestDecision: async ({ fetch, locals, request }) => requestProposalDecision(fetch, request, requireCurrentUser(locals).userID),
  proposalApprove: async ({ fetch, locals, request }) => approveProposal(fetch, request, true, requireCurrentUser(locals).userID),
  proposalNotApprove: async ({ fetch, locals, request }) => approveProposal(fetch, request, false, requireCurrentUser(locals).userID),

  saveTrade: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const tradedPrice = getFormNumber(formData, 'tradedPrice');
    const tradeDateTime = getFormString(formData, 'tradeDateTime');
    const settlementDate = getSettlementDate(formData);
    const allocations = getTradeAllocations(formData);
    const hasTrade = getFormString(formData, 'hasTrade') === 'true';

    if (!ticketNumber || !eventDateTime || !tradedPrice || !tradeDateTime || !settlementDate || allocations.length === 0)
      return fail(400, responseFailure('saveTrade', 'Trade price, trade date, settlement date, and allocations are required.'));

    const settlementDateError = validateSettlementDate(settlementDate, tradeDateTime);
    if (settlementDateError)
      return fail(400, responseFailure('saveTrade', settlementDateError, ticketNumber));

    const apiEventDateTime = toApiDateTime(eventDateTime);

    try {
      const tickets = await getTickets(fetch, apiEventDateTime, null, true);
      const ticket = tickets.items.find((item) => item.ticketNumber === ticketNumber);

      if (!ticket)
        return fail(404, responseFailure('saveTrade', `No matching ticket found for TicketNumber '${ticketNumber}'.`, ticketNumber));

      const tradeTotalError = validateAllocationTotal(allocations, allocationTotal(ticket.proposalAllocations), 'Trade allocations');
      if (tradeTotalError)
        return fail(400, responseFailure('saveTrade', tradeTotalError, ticketNumber));
      const accounts = await getAccounts(fetch, apiEventDateTime, null);
      const cashHoldingError = validateTradeCashHoldings(allocations, accounts.items);
      if (cashHoldingError)
        return fail(400, responseFailure('saveTrade', cashHoldingError, ticketNumber));

      const ticketTradeRequest: TicketTradeRequest = {
        allocations: toTradeRequestAllocations(allocations),
        eventDateTime: apiEventDateTime,
        reason: `${hasTrade ? 'Modify' : 'Create'} trade for ticket ${ticketNumber}`,
        settlementDateTime: dateInputToApiStartOfDay(settlementDate),
        ticketNumber,
        tradeDateTime: toApiDateTime(tradeDateTime),
        tradedPrice,
        userID: currentUser.userID
      };

      const result = hasTrade
        ? await postTicketTradeModifiedEvent(fetch, ticketTradeRequest)
        : await postTicketTradeCreatedEvent(fetch, ticketTradeRequest);

      return responseSuccess('saveTrade', 'Trade saved.', result.eventID);
    } catch (error) {
      const accounts = await getAccounts(fetch, apiEventDateTime, null).catch(() => null);
      return fail(502, responseFailure('saveTrade', friendlyTradeCashHoldingError(readError(error), accounts?.items ?? []), ticketNumber));
    }
  },

  addFill: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const brokerLEI = getFormString(formData, 'brokerLEI');
    const price = getFormNumber(formData, 'fillPrice');
    const quantity = getFormNumber(formData, 'fillQuantity');
    const settlementAmount = getFormNumber(formData, 'fillSettlementAmount');
    const note = getFormString(formData, 'fillNote');

    if (!ticketNumber || !eventDateTime || !brokerLEI || !price || !quantity || !settlementAmount)
      return fail(400, responseFailure('addFill', 'Fill broker, price, quantity, and settlement amount are required.', ticketNumber));

    try {
      const apiEventDateTime = toApiDateTime(eventDateTime);
      const tickets = await getTickets(fetch, apiEventDateTime, null, true);
      const ticket = tickets.items.find((item) => item.ticketNumber === ticketNumber);

      if (!ticket)
        return fail(404, responseFailure('addFill', `No matching ticket found for TicketNumber '${ticketNumber}'.`, ticketNumber));

      const brokers = await getBrokers(fetch, apiEventDateTime, null);
      if (!brokers.items.some((broker) => broker.active && broker.lei === brokerLEI))
        return fail(400, responseFailure('addFill', 'Select an active broker.', ticketNumber));
      if (!isWholeQuantity(quantity))
        return fail(400, responseFailure('addFill', 'Fill quantity must be a positive whole number.', ticketNumber));

      const remainingQuantity = allocationTotal(ticket.tradeAllocations) - allocationTotal(ticket.fills);
      if (quantity > remainingQuantity + 0.00000001)
        return fail(400, responseFailure('addFill', `Fill quantity exceeds the remaining trade quantity of ${formatValidationNumber(remainingQuantity)}.`, ticketNumber));
      const remainingSettlementAmount = settlementAmountTotal(ticket.tradeAllocations) - settlementAmountTotal(ticket.fills);
      if (settlementAmount > remainingSettlementAmount + 0.00000001)
        return fail(400, responseFailure('addFill', `Fill settlement amount exceeds the remaining trade settlement amount of ${formatValidationNumber(remainingSettlementAmount)}.`, ticketNumber));

      const result = await postTicketTradeFillAddedEvent(fetch, {
        brokerLEI,
        eventDateTime: apiEventDateTime,
        note,
        price,
        quantity,
        reason: `Add fill to ticket ${ticketNumber}`,
        settlementAmount,
        ticketNumber,
        userID: currentUser.userID
      });

      return responseSuccess('addFill', 'Fill added.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('addFill', readError(error), ticketNumber));
    }
  },

  removeFill: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const fillID = getFormString(formData, 'fillID');

    if (!ticketNumber || !eventDateTime || !fillID)
      return fail(400, responseFailure('removeFill', 'Ticket, fill, and event date are required.'));

    try {
      const result = await postTicketTradeFillRemovedEvent(fetch, {
        eventDateTime: toApiDateTime(eventDateTime),
        fillID,
        reason: `Remove fill from ticket ${ticketNumber}`,
        ticketNumber,
        userID: currentUser.userID
      });

      return responseSuccess('removeFill', 'Fill removed.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('removeFill', readError(error)));
    }
  },

  tradeRequestDecision: async ({ fetch, locals, request }) => requestTradeDecision(fetch, request, requireCurrentUser(locals).userID),
  sendFoleoTraderOrder: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const brokerLEI = getFormString(formData, 'brokerLEI');

    if (!ticketNumber || !eventDateTime || !brokerLEI)
      return fail(400, responseFailure('sendFoleoTraderOrder', 'Ticket, event date, and FIX broker are required.'));

    try {
      const result = await postFoleoTraderOrder(fetch, {
        eventDateTime: toApiDateTime(eventDateTime),
        ticketNumber,
        brokerLEI,
        userID: currentUser.userID
      });

      return responseSuccess('sendFoleoTraderOrder', `FoleoTrader order sent (${result.clOrdID}).`, result.eventID);
    } catch (error) {
      return fail(502, responseFailure('sendFoleoTraderOrder', readError(error)));
    }
  },
  sendTradeFileTicket: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const brokerLEI = getFormString(formData, 'brokerLEI');
    if (!ticketNumber || !eventDateTime || !brokerLEI)
      return fail(400, responseFailure('sendTradeFileTicket', 'Ticket, event date, and TradeFile broker are required.'));
    try {
      const result = await postTradeFilePending(fetch, {
        userID: currentUser.userID,
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `Assign ticket ${ticketNumber} to TradeFile broker`,
        ticketNumber,
        brokerLEI
      });
      return responseSuccess('sendTradeFileTicket', 'Ticket added to the Trade Files queue.', result.eventID ?? '');
    } catch (error) {
      return fail(502, responseFailure('sendTradeFileTicket', readError(error), ticketNumber));
    }
  },
  sendTradeFileBatch: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const brokerLEI = getFormString(formData, 'brokerLEI');
    const ticketNumbers = getFormStrings(formData, 'ticketNumbers').map(Number).filter(Number.isFinite);
    if (!eventDateTime || !brokerLEI || ticketNumbers.length === 0)
      return fail(400, responseFailure('sendTradeFileBatch', 'Broker and at least one ticket are required.'));
    try {
      const result = await postTradeFileRequest(fetch, {
        userID: currentUser.userID,
        eventDateTime: toApiDateTime(eventDateTime),
        reason: 'Request TradeFile',
        brokerLEI,
        ticketNumbers
      });
      return responseSuccess('sendTradeFileBatch', 'TradeFile requested.', result.tradeFileID ?? '');
    } catch (error) {
      return fail(502, responseFailure('sendTradeFileBatch', readError(error)));
    }
  },
  tradeApprove: async ({ fetch, locals, request }) => approveTrade(fetch, request, true, requireCurrentUser(locals).userID),
  tradeNotApprove: async ({ fetch, locals, request }) => approveTrade(fetch, request, false, requireCurrentUser(locals).userID),

  setTradeInstructionNotes: async ({ fetch, locals, request }) => setTicketText(
    fetch,
    request,
    requireCurrentUser(locals).userID,
    'tradeInstructionNotes',
    'setTradeInstructionNotes',
    'Set trade instruction notes',
    'Trade instruction notes saved.',
    postTicketTradeInstructionNotesSetEvent
  ),

  setTradeProgressNotes: async ({ fetch, locals, request }) => setTicketText(
    fetch,
    request,
    requireCurrentUser(locals).userID,
    'tradeProgressNotes',
    'setTradeProgressNotes',
    'Set trade progress notes',
    'Trade progress notes saved.',
    postTicketTradeProgressNotesSetEvent
  ),

  deleteTicket: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');

    if (!ticketNumber || !eventDateTime)
      return fail(400, responseFailure('deleteTicket', 'Ticket and event date are required.'));

    try {
      const result = await postTicketCancelledEvent(fetch, {
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `Delete ticket ${ticketNumber}`,
        ticketNumber,
        userID: currentUser.userID
      });

      return responseSuccess('deleteTicket', 'Ticket deleted.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('deleteTicket', readError(error)));
    }
  }
};

async function setTicketText(
  fetchApi: typeof fetch,
  request: Request,
  userID: string,
  fieldName: string,
  intent: string,
  reasonPrefix: string,
  successMessage: string,
  submit: (fetchApi: typeof fetch, request: TicketTextSetRequest) => Promise<{ eventID: string }>
) {
  const formData = await request.formData();
  const ticketNumber = getFormNumber(formData, 'ticketNumber');
  const eventDateTime = getFormString(formData, 'eventDateTime');

  if (!ticketNumber || !eventDateTime)
    return fail(400, responseFailure(intent, 'Ticket and event date are required.'));

  try {
    const result = await submit(fetchApi, {
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `${reasonPrefix} for ticket ${ticketNumber}`,
      ticketNumber,
      userID,
      value: getFormString(formData, fieldName)
    });

    return responseSuccess(intent, successMessage, result.eventID);
  } catch (error) {
    return fail(502, responseFailure(intent, readError(error)));
  }
}

async function approveProposal(fetchApi: typeof fetch, request: Request, approved: boolean, userID: string) {
  const formData = await request.formData();
  const ticketNumber = getFormNumber(formData, 'ticketNumber');
  const eventDateTime = getFormString(formData, 'eventDateTime');

  if (!ticketNumber || !eventDateTime)
    return fail(400, responseFailure(approved ? 'proposalApprove' : 'proposalNotApprove', 'Ticket and event date are required.'));

  try {
    if (approved) {
      const tickets = await getTickets(fetchApi, toApiDateTime(eventDateTime), null, true);
      const ticket = tickets.items.find((item) => item.ticketNumber === ticketNumber);

      if (!ticket)
        return fail(404, responseFailure('proposalApprove', `No matching ticket found for TicketNumber '${ticketNumber}'.`));

      if (ticket.proposalAllocations.length === 0)
        return fail(400, responseFailure('proposalApprove', 'Proposal allocations are required.'));
    }

    const submit = approved ? postTicketProposalApprovedEvent : postTicketProposalNotApprovedEvent;
    const result = await submit(fetchApi, {
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `${approved ? 'Approve' : 'Not approve'} proposal for ticket ${ticketNumber}`,
      ticketNumber,
      userID
    });

    return responseSuccess(approved ? 'proposalApprove' : 'proposalNotApprove', `Proposal ${approved ? 'approved' : 'not approved'}.`, result.eventID);
  } catch (error) {
    return fail(502, responseFailure(approved ? 'proposalApprove' : 'proposalNotApprove', readError(error)));
  }
}

async function requestProposalDecision(fetchApi: typeof fetch, request: Request, userID: string) {
  const formData = await request.formData();
  const ticketNumber = getFormNumber(formData, 'ticketNumber');
  const eventDateTime = getFormString(formData, 'eventDateTime');

  if (!ticketNumber || !eventDateTime)
    return fail(400, responseFailure('proposalRequestDecision', 'Ticket and event date are required.'));

  try {
    const tickets = await getTickets(fetchApi, toApiDateTime(eventDateTime), null, true);
    const ticket = tickets.items.find((item) => item.ticketNumber === ticketNumber);

    if (!ticket)
      return fail(404, responseFailure('proposalRequestDecision', `No matching ticket found for TicketNumber '${ticketNumber}'.`));

    if (ticket.proposalAllocations.length === 0)
      return fail(400, responseFailure('proposalRequestDecision', 'Proposal allocations are required.'));

    const result = await postTicketProposalDecisionRequestedEvent(fetchApi, {
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `Request proposal decision for ticket ${ticketNumber}`,
      ticketNumber,
      userID
    });

    return responseSuccess('proposalRequestDecision', 'Proposal decision requested.', result.eventID);
  } catch (error) {
    return fail(502, responseFailure('proposalRequestDecision', readError(error)));
  }
}

async function requestTradeDecision(fetchApi: typeof fetch, request: Request, userID: string) {
  const formData = await request.formData();
  const ticketNumber = getFormNumber(formData, 'ticketNumber');
  const eventDateTime = getFormString(formData, 'eventDateTime');

  if (!ticketNumber || !eventDateTime)
    return fail(400, responseFailure('tradeRequestDecision', 'Ticket and event date are required.'));

  try {
    const tickets = await getTickets(fetchApi, toApiDateTime(eventDateTime), null, true);
    const ticket = tickets.items.find((item) => item.ticketNumber === ticketNumber);

    if (!ticket)
      return fail(404, responseFailure('tradeRequestDecision', `No matching ticket found for TicketNumber '${ticketNumber}'.`, ticketNumber));

    const tradeTotalError = validateAllocationTotal(ticket.tradeAllocations, allocationTotal(ticket.proposalAllocations), 'Trade allocations');
    if (tradeTotalError)
      return fail(400, responseFailure('tradeRequestDecision', tradeTotalError, ticketNumber));
    if (!ticket.tradeDateTime || !ticket.settlementDateTime)
      return fail(400, responseFailure('tradeRequestDecision', 'Trade date and settlement date are required.', ticketNumber));
    const accounts = await getAccounts(fetchApi, toApiDateTime(eventDateTime), null);
    const cashHoldingError = validateTradeCashHoldings(ticket.tradeAllocations, accounts.items);
    if (cashHoldingError)
      return fail(400, responseFailure('tradeRequestDecision', cashHoldingError, ticketNumber));

    const result = await postTicketTradeDecisionRequestedEvent(fetchApi, {
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `Request trade decision for ticket ${ticketNumber}`,
      ticketNumber,
      userID
    });

    return responseSuccess('tradeRequestDecision', 'Trade decision requested.', result.eventID);
  } catch (error) {
    return fail(502, responseFailure('tradeRequestDecision', readError(error), ticketNumber));
  }
}

async function approveTrade(fetchApi: typeof fetch, request: Request, approved: boolean, userID: string) {
  const formData = await request.formData();
  const ticketNumber = getFormNumber(formData, 'ticketNumber');
  const eventDateTime = getFormString(formData, 'eventDateTime');

  if (!ticketNumber || !eventDateTime)
    return fail(400, responseFailure(approved ? 'tradeApprove' : 'tradeNotApprove', 'Ticket and event date are required.'));

  try {
    if (approved) {
      const tickets = await getTickets(fetchApi, toApiDateTime(eventDateTime), null, true);
      const ticket = tickets.items.find((item) => item.ticketNumber === ticketNumber);

      if (!ticket)
        return fail(404, responseFailure('tradeApprove', `No matching ticket found for TicketNumber '${ticketNumber}'.`));

      const tradeTotalError = validateAllocationTotal(ticket.tradeAllocations, allocationTotal(ticket.proposalAllocations), 'Trade allocations');
      if (tradeTotalError)
        return fail(400, responseFailure('tradeApprove', tradeTotalError));
      if (!ticket.tradeDateTime || !ticket.settlementDateTime)
        return fail(400, responseFailure('tradeApprove', 'Trade date and settlement date are required.', ticketNumber));
      const accounts = await getAccounts(fetchApi, toApiDateTime(eventDateTime), null);
      const cashHoldingError = validateTradeCashHoldings(ticket.tradeAllocations, accounts.items);
      if (cashHoldingError)
        return fail(400, responseFailure('tradeApprove', cashHoldingError, ticketNumber));

      const fillTotalError = validateAllocationTotal(ticket.fills, allocationTotal(ticket.tradeAllocations), 'Fills');
      if (fillTotalError)
        return fail(400, responseFailure('tradeApprove', fillTotalError));
      const fillSettlementAmountError = validateSettlementAmountTotal(ticket.fills, settlementAmountTotal(ticket.tradeAllocations), 'Fills');
      if (fillSettlementAmountError)
        return fail(400, responseFailure('tradeApprove', fillSettlementAmountError));
    }

    const result = approved
      ? await postTicketTradeApprovedEvent(fetchApi, {
          eventDateTime: toApiDateTime(eventDateTime),
          reason: `Approve trade for ticket ${ticketNumber}`,
          ticketNumber,
          userID
        })
      : await postTicketTradeNotApprovedEvent(fetchApi, {
          eventDateTime: toApiDateTime(eventDateTime),
          reason: `Not approve trade for ticket ${ticketNumber}`,
          ticketNumber,
          userID
        });

    return responseSuccess(approved ? 'tradeApprove' : 'tradeNotApprove', `Trade ${approved ? 'approved' : 'not approved'}.`, result.eventID);
  } catch (error) {
    return fail(502, responseFailure(approved ? 'tradeApprove' : 'tradeNotApprove', readError(error)));
  }
}

function getSettlementDate(formData: FormData, fallbackDateTime = '') {
  const settlementDate = getFormString(formData, 'settlementDate');
  if (settlementDate)
    return settlementDate;

  const settlementDateTime = getFormString(formData, 'settlementDateTime') || fallbackDateTime;
  return settlementDateTime.slice(0, 10);
}

function validateSettlementDate(settlementDate: string, tradeDateTime: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(settlementDate))
    return 'Settlement date is invalid.';

  const tradeDate = tradeDateTime.slice(0, 10);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(tradeDate))
    return 'Trade date is invalid.';

  return settlementDate < tradeDate ? 'Settlement date cannot be before the trade date.' : '';
}

function hasProposalFields(formData: FormData) {
  return formData.has('targetPrice') || formData.has('proposalAllocationAccountID');
}

function hasTradeFields(formData: FormData) {
  return formData.has('tradedPrice') || formData.has('tradeDateTime') || formData.has('settlementDate') || formData.has('tradeAllocationAccountID') || Array.from(formData.keys()).some((key) => key.startsWith('tradeCashHoldingID-'));
}

function changedTextField(
  formData: FormData,
  ticket: Ticket,
  fieldName: 'proposalReason' | 'proposalAllocation' | 'tradeInstructionNotes' | 'tradeProgressNotes'
) {
  return formData.has(fieldName) && getFormString(formData, fieldName) !== ticket[fieldName];
}

function submittedOrCurrentNumber(formData: FormData, fieldName: string, current: number | null | undefined) {
  const submitted = getOptionalFormNumber(formData, fieldName);
  return submitted === undefined ? current ?? null : submitted;
}

function getOptionalFormNumber(formData: FormData, key: string) {
  if (!formData.has(key))
    return undefined;

  const value = getFormString(formData, key);

  if (!value)
    return null;

  const number = parseFormNumber(value);
  return Number.isFinite(number) ? number : null;
}

function numbersEqual(left: number | null | undefined, right: number | null | undefined) {
  if (left == null || right == null)
    return left == null && right == null;

  return Math.abs(left - right) < 0.00000001;
}

function proposalAllocationsEqual(left: TicketProposalAllocation[], right: TicketProposalAllocation[]) {
  const leftSorted = sortProposalAllocations(left);
  const rightSorted = sortProposalAllocations(right);

  return leftSorted.length === rightSorted.length &&
    leftSorted.every((allocation, index) =>
      allocation.accountID === rightSorted[index].accountID &&
      numbersEqual(allocation.quantity, rightSorted[index].quantity)
    );
}

function tradeAllocationsEqual(left: TicketTradeAllocation[], right: TicketTradeAllocation[]) {
  const leftSorted = sortTradeAllocations(left);
  const rightSorted = sortTradeAllocations(right);

  return leftSorted.length === rightSorted.length &&
    leftSorted.every((allocation, index) =>
      allocation.accountID === rightSorted[index].accountID &&
      (allocation.cashHoldingID ?? '') === (rightSorted[index].cashHoldingID ?? '') &&
      numbersEqual(allocation.quantity, rightSorted[index].quantity) &&
      numbersEqual(allocation.settlementAmount, rightSorted[index].settlementAmount) &&
      numbersEqual(allocation.bookCostOverride ?? 0, rightSorted[index].bookCostOverride ?? 0)
    );
}

function sortProposalAllocations(allocations: TicketProposalAllocation[]) {
  return [...allocations].sort((left, right) => left.accountID.localeCompare(right.accountID));
}

function sortTradeAllocations(allocations: TicketTradeAllocation[]) {
  return [...allocations].sort((left, right) => left.accountID.localeCompare(right.accountID));
}

function getProposalAllocations(formData: FormData, accountIDField = 'allocationAccountID') {
  return getFormStrings(formData, accountIDField)
    .map((accountID) => ({
      accountID,
      quantity: parseFormNumber(getFormStrings(formData, `proposalQuantity-${accountID}`)[0] || '')
    }))
    .filter((allocation) => allocation.accountID && allocation.quantity > 0);
}

function getTradeAllocations(formData: FormData, accountIDField = 'allocationAccountID') {
  return getFormStrings(formData, accountIDField)
    .map((accountID) => ({
      accountID,
      cashHoldingID: getFormStrings(formData, `tradeCashHoldingID-${accountID}`)[0] || '',
      quantity: parseFormNumber(getFormStrings(formData, `tradeQuantity-${accountID}`)[0] || ''),
      settlementAmount: parseFormNumber(getFormStrings(formData, `tradeSettlementAmount-${accountID}`)[0] || '')
    }))
    .filter((allocation) => allocation.accountID && allocation.quantity > 0 && allocation.settlementAmount > 0);
}

function validateTradeCashHoldings(allocations: TicketTradeAllocation[], accounts: Account[] = []) {
  const missingAccounts = allocations.filter((allocation) => !allocation.cashHoldingID);
  if (missingAccounts.length === 0)
    return '';

  const names = missingAccounts.map((allocation) => accountDisplayName(accounts, allocation.accountID));
  return `Select a cash holding for ${formatList(names)}.`;
}

function submittedOrCurrentString(formData: FormData, fieldName: string, current: string) {
  if (!formData.has(fieldName))
    return current;

  return getFormString(formData, fieldName);
}

function dateTimesEqual(left: string, right: string | null | undefined) {
  if (!left || !right)
    return !left && !right;

  const leftTime = new Date(left).getTime();
  const rightTime = new Date(right).getTime();
  return Number.isFinite(leftTime) && Number.isFinite(rightTime) && leftTime === rightTime;
}

function settlementDatesEqual(left: string, right: string | null | undefined) {
  if (!left || !right)
    return !left && !right;

  return left === right.slice(0, 10);
}

function accountDisplayName(accounts: Account[], accountID: string) {
  return accounts.find((account) => account.accountID === accountID)?.name ?? accountID;
}

function formatList(values: string[]) {
  if (values.length <= 1)
    return values[0] ?? '';

  return `${values.slice(0, -1).join(', ')} and ${values[values.length - 1]}`;
}

function friendlyTradeCashHoldingError(message: string, accounts: Account[]) {
  if (!message.includes('Trade allocation cash holding is required for AccountID'))
    return message;

  const names = [...message.matchAll(/AccountID '([^']+)'/g)]
    .map((match) => match[1])
    .map((accountID) => accountDisplayName(accounts, accountID));

  return names.length > 0
    ? `Select a cash holding for ${formatList(names)}.`
    : message;
}

function toTradeRequestAllocations(allocations: TicketTradeAllocation[]): TicketTradeRequest['allocations'] {
  return allocations.map((allocation) => ({
    accountID: allocation.accountID,
    bookCostOverride: allocation.bookCostOverride,
    cashHoldingID: allocation.cashHoldingID ?? '',
    quantity: allocation.quantity,
    settlementAmount: allocation.settlementAmount
  }));
}

function allocationTotal(allocations: { quantity: number }[]) {
  return allocations.reduce((total, allocation) => total + allocation.quantity, 0);
}

function settlementAmountTotal(items: { settlementAmount: number }[]) {
  return items.reduce((total, item) => total + item.settlementAmount, 0);
}

function validateAllocationTotal(allocations: { quantity: number }[], expectedTotal: number, label: string) {
  if (!expectedTotal || expectedTotal <= 0)
    return `${label} need a positive total quantity.`;

  const total = allocationTotal(allocations);

  return numbersEqual(total, expectedTotal)
    ? ''
    : `${label} must sum to ${formatValidationNumber(expectedTotal)}. Current total is ${formatValidationNumber(total)}.`;
}

function isWholeQuantity(value: number) {
  return Number.isInteger(value) && value > 0;
}

function validateSettlementAmountTotal(items: { settlementAmount: number }[], expectedTotal: number, label: string) {
  if (!expectedTotal || expectedTotal <= 0)
    return `${label} need a positive total settlement amount.`;

  const total = settlementAmountTotal(items);

  return numbersEqual(total, expectedTotal)
    ? ''
    : `${label} settlement amount must sum to ${formatValidationNumber(expectedTotal)}. Current total is ${formatValidationNumber(total)}.`;
}

function formatValidationNumber(value: number) {
  if (!Number.isFinite(value))
    return '0';

  return value.toLocaleString(undefined, { maximumFractionDigits: 8 });
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function getFormStrings(formData: FormData, key: string) {
  return formData.getAll(key).filter((value): value is string => typeof value === 'string').map((value) => value.trim());
}

function uniqueStrings(values: string[]) {
  return Array.from(new Set(values.filter(Boolean)));
}

function getFormNumber(formData: FormData, key: string) {
  const value = parseFormNumber(getFormString(formData, key));
  return Number.isFinite(value) ? value : 0;
}

function parseFormNumber(value: string) {
  const normalized = value.replace(/,/g, '').trim();
  return normalized ? Number(normalized) : 0;
}

function responseSuccess(intent: string, message: string, eventID: string) {
  return {
    eventID,
    intent,
    message,
    status: 'success'
  };
}

function responseFailure(intent: string, message: string, ticketNumber?: number | null) {
  return {
    intent,
    message,
    status: 'failure',
    ...(ticketNumber ? { ticketNumber } : {})
  };
}

function readError(error: unknown) {
  return error instanceof Error ? error.message : 'Unable to update ticket.';
}

function resolveInstrument(instruments: Instrument[], value: string) {
  return instruments.find((instrument) =>
    instrument.instrumentID === value ||
    instrumentOptionLabel(instrument) === value ||
    instrument.name === value
  );
}

function instrumentOptionLabel(instrument: Instrument) {
  const ticker = instrument.identifiers.find((identifier) => identifier.type === 'Ticker' || identifier.type === 0)?.value ?? '';
  return [ticker, instrument.name, instrument.exchange].filter(Boolean).join(' - ');
}
