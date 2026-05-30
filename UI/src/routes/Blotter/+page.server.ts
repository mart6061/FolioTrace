import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import {
  getAccounts,
  getInstruments,
  getTicketStatusOptions,
  getTickets,
  postTicketAccountAddedEvent,
  postTicketAccountRemovedEvent,
  postTicketCancelledEvent,
  postTicketCreatedEvent,
  postTicketProposalApprovedEvent,
  postTicketProposalCreatedEvent,
  postTicketProposalModifiedEvent,
  postTicketProposalNotApprovedEvent,
  postTicketTradeApprovedEvent,
  postTicketTradeCreatedEvent,
  postTicketTradeFillAddedEvent,
  postTicketTradeFillRemovedEvent,
  postTicketTradeModifiedEvent,
  postTicketTradeNotApprovedEvent,
  type TicketProposalRequest,
  type TicketTradeRequest
} from '$lib/server/api';
import type { Instrument, TicketSide } from '$lib/types';
import { fail } from '@sveltejs/kit';

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const load = async ({ fetch, url }) => {
  const valuationDate = todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const eventDateTime = toApiDateTime(valuationDate);
  const asOfDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const [tickets, accounts, instruments, ticketStatusOptions] = await Promise.all([
      getTickets(fetch, eventDateTime, asOfDateTime),
      getAccounts(fetch, eventDateTime, asOfDateTime),
      getInstruments(fetch, eventDateTime, asOfDateTime),
      getTicketStatusOptions(fetch)
    ]);

    return {
      accounts,
      auditDateTime,
      error: '',
      instruments,
      ticketStatusOptions,
      tickets,
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load the blotter.',
      instruments: null,
      ticketStatusOptions: [],
      tickets: null,
      valuationDate
    };
  }
};

export const actions = {
  createTicket: async ({ fetch, request }) => {
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
        userID: systemUserID
      });

      return responseSuccess('createTicket', 'Ticket created.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('createTicket', readError(error)));
    }
  },

  addAccount: async ({ fetch, request }) => {
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const accountID = getFormString(formData, 'accountID');
    const eventDateTime = getFormString(formData, 'eventDateTime');

    if (!ticketNumber || !accountID || !eventDateTime)
      return fail(400, responseFailure('addAccount', 'Ticket, account, and event date are required.'));

    try {
      const result = await postTicketAccountAddedEvent(fetch, {
        accountID,
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `Add account to ticket ${ticketNumber}`,
        ticketNumber,
        userID: systemUserID
      });

      return responseSuccess('addAccount', 'Account added.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('addAccount', readError(error)));
    }
  },

  removeAccount: async ({ fetch, request }) => {
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
        userID: systemUserID
      });

      return responseSuccess('removeAccount', 'Account removed.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('removeAccount', readError(error)));
    }
  },

  saveProposal: async ({ fetch, request }) => {
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const targetPrice = getFormNumber(formData, 'targetPrice');
    const totalAmount = getFormNumber(formData, 'totalAmount');
    const allocations = getProposalAllocations(formData);
    const hasProposal = getFormString(formData, 'hasProposal') === 'true';

    if (!ticketNumber || !eventDateTime || !targetPrice || !totalAmount || allocations.length === 0)
      return fail(400, responseFailure('saveProposal', 'Proposal price, amount, and allocations are required.'));

    const ticketProposalRequest: TicketProposalRequest = {
      allocations,
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `${hasProposal ? 'Modify' : 'Create'} proposal for ticket ${ticketNumber}`,
      targetPrice,
      ticketNumber,
      totalAmount,
      userID: systemUserID
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

  proposalApprove: async ({ fetch, request }) => approveProposal(fetch, request, true),
  proposalNotApprove: async ({ fetch, request }) => approveProposal(fetch, request, false),

  saveTrade: async ({ fetch, request }) => {
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const tradedPrice = getFormNumber(formData, 'tradedPrice');
    const allocations = getTradeAllocations(formData);
    const hasTrade = getFormString(formData, 'hasTrade') === 'true';

    if (!ticketNumber || !eventDateTime || !tradedPrice || allocations.length === 0)
      return fail(400, responseFailure('saveTrade', 'Trade price and allocations are required.'));

    const ticketTradeRequest: TicketTradeRequest = {
      allocations,
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `${hasTrade ? 'Modify' : 'Create'} trade for ticket ${ticketNumber}`,
      ticketNumber,
      tradedPrice,
      userID: systemUserID
    };

    try {
      const result = hasTrade
        ? await postTicketTradeModifiedEvent(fetch, ticketTradeRequest)
        : await postTicketTradeCreatedEvent(fetch, ticketTradeRequest);

      return responseSuccess('saveTrade', 'Trade saved.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('saveTrade', readError(error)));
    }
  },

  addFill: async ({ fetch, request }) => {
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const price = getFormNumber(formData, 'fillPrice');
    const quantity = getFormNumber(formData, 'fillQuantity');
    const note = getFormString(formData, 'fillNote');

    if (!ticketNumber || !eventDateTime || !price || !quantity)
      return fail(400, responseFailure('addFill', 'Fill price and quantity are required.'));

    try {
      const result = await postTicketTradeFillAddedEvent(fetch, {
        eventDateTime: toApiDateTime(eventDateTime),
        note,
        price,
        quantity,
        reason: `Add fill to ticket ${ticketNumber}`,
        ticketNumber,
        userID: systemUserID
      });

      return responseSuccess('addFill', 'Fill added.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('addFill', readError(error)));
    }
  },

  removeFill: async ({ fetch, request }) => {
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
        userID: systemUserID
      });

      return responseSuccess('removeFill', 'Fill removed.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('removeFill', readError(error)));
    }
  },

  tradeApprove: async ({ fetch, request }) => approveTrade(fetch, request, true),
  tradeNotApprove: async ({ fetch, request }) => approveTrade(fetch, request, false),

  cancelTicket: async ({ fetch, request }) => {
    const formData = await request.formData();
    const ticketNumber = getFormNumber(formData, 'ticketNumber');
    const eventDateTime = getFormString(formData, 'eventDateTime');

    if (!ticketNumber || !eventDateTime)
      return fail(400, responseFailure('cancelTicket', 'Ticket and event date are required.'));

    try {
      const result = await postTicketCancelledEvent(fetch, {
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `Cancel ticket ${ticketNumber}`,
        ticketNumber,
        userID: systemUserID
      });

      return responseSuccess('cancelTicket', 'Ticket cancelled.', result.eventID);
    } catch (error) {
      return fail(502, responseFailure('cancelTicket', readError(error)));
    }
  }
};

async function approveProposal(fetchApi: typeof fetch, request: Request, approved: boolean) {
  const formData = await request.formData();
  const ticketNumber = getFormNumber(formData, 'ticketNumber');
  const eventDateTime = getFormString(formData, 'eventDateTime');

  if (!ticketNumber || !eventDateTime)
    return fail(400, responseFailure(approved ? 'proposalApprove' : 'proposalNotApprove', 'Ticket and event date are required.'));

  try {
    const submit = approved ? postTicketProposalApprovedEvent : postTicketProposalNotApprovedEvent;
    const result = await submit(fetchApi, {
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `${approved ? 'Approve' : 'Not approve'} proposal for ticket ${ticketNumber}`,
      ticketNumber,
      userID: systemUserID
    });

    return responseSuccess(approved ? 'proposalApprove' : 'proposalNotApprove', `Proposal ${approved ? 'approved' : 'not approved'}.`, result.eventID);
  } catch (error) {
    return fail(502, responseFailure(approved ? 'proposalApprove' : 'proposalNotApprove', readError(error)));
  }
}

async function approveTrade(fetchApi: typeof fetch, request: Request, approved: boolean) {
  const formData = await request.formData();
  const ticketNumber = getFormNumber(formData, 'ticketNumber');
  const eventDateTime = getFormString(formData, 'eventDateTime');

  if (!ticketNumber || !eventDateTime)
    return fail(400, responseFailure(approved ? 'tradeApprove' : 'tradeNotApprove', 'Ticket and event date are required.'));

  try {
    const submit = approved ? postTicketTradeApprovedEvent : postTicketTradeNotApprovedEvent;
    const result = await submit(fetchApi, {
      eventDateTime: toApiDateTime(eventDateTime),
      reason: `${approved ? 'Approve' : 'Not approve'} trade for ticket ${ticketNumber}`,
      ticketNumber,
      userID: systemUserID
    });

    return responseSuccess(approved ? 'tradeApprove' : 'tradeNotApprove', `Trade ${approved ? 'approved' : 'not approved'}.`, result.eventID);
  } catch (error) {
    return fail(502, responseFailure(approved ? 'tradeApprove' : 'tradeNotApprove', readError(error)));
  }
}

function getProposalAllocations(formData: FormData) {
  return getFormStrings(formData, 'allocationAccountID')
    .map((accountID) => ({
      accountID,
      quantity: Number(getFormStrings(formData, `proposalQuantity-${accountID}`)[0] || 0)
    }))
    .filter((allocation) => allocation.accountID && allocation.quantity > 0);
}

function getTradeAllocations(formData: FormData) {
  return getFormStrings(formData, 'allocationAccountID')
    .map((accountID) => ({
      accountID,
      bookCost: Number(getFormStrings(formData, `tradeBookCost-${accountID}`)[0] || 0),
      quantity: Number(getFormStrings(formData, `tradeQuantity-${accountID}`)[0] || 0)
    }))
    .filter((allocation) => allocation.accountID && allocation.quantity > 0 && allocation.bookCost > 0);
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function getFormStrings(formData: FormData, key: string) {
  return formData.getAll(key).filter((value): value is string => typeof value === 'string').map((value) => value.trim());
}

function getFormNumber(formData: FormData, key: string) {
  const value = Number(getFormString(formData, key));
  return Number.isFinite(value) ? value : 0;
}

function responseSuccess(intent: string, message: string, eventID: string) {
  return {
    eventID,
    intent,
    message,
    status: 'success'
  };
}

function responseFailure(intent: string, message: string) {
  return {
    intent,
    message,
    status: 'failure'
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
