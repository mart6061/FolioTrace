import { getHoldingEvents, getTransactionEvents, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';
import type { HoldingHistoryEvent, HoldingKind, HoldingReferenceEvent, TransactionReferenceEvent, ValuationHistoryEvent } from '$lib/types';
import type { RequestHandler } from './$types';

const holdingKinds: HoldingKind[] = [
  'PositionMemo',
  'PositionCash',
  'PositionAsset',
  'CashDebt',
  'CashInvestable',
  'CashNonInvestable',
  'NominalInflow',
  'NominalOutflow',
  'NominalInSpecieIn',
  'NominalInSpecieOut',
  'NominalFeesCustodian',
  'NominalFeesAdministrator',
  'NominalFeesBank',
  'NominalIncome',
  'NominalInterest'
];

export const GET: RequestHandler = async ({ fetch, url }) => {
  const accountID = (url.searchParams.get('accountID') || '').trim();
  const valuationDateTime = parseOptionalDate(url.searchParams.get('valuationDateTime'));
  const auditDateTime = parseOptionalDate(url.searchParams.get('auditDateTime'));

  const [holdingEvents, transactionEvents] = await Promise.all([
    getHoldingEvents(fetch, accountID ? { accountID } : undefined),
    getTransactionEvents(fetch, accountID ? { accountID } : undefined)
  ]);
  const accountHoldingEvents = holdingEvents.map(normalizeHoldingEvent);
  const movementByEventID = new Map(
    transactionEvents
      .filter(isTransactionMovementEvent)
      .map((event) => [event.eventID, event])
  );
  const transactionHistory = transactionEvents.map((event) => enrichCancellationEvent(event, movementByEventID));
  const history: ValuationHistoryEvent[] = [];

  for (const event of [...accountHoldingEvents, ...transactionHistory]) {
    const excludedEvent = addExclusionStatus(event, valuationDateTime, auditDateTime);

    if (excludedEvent)
      history.push(excludedEvent);
  }

  history.sort(compareEvents);

  return json(history);
};

function compareEvents(left: { eventDateTime: string; auditDateTime: string; eventID: string }, right: { eventDateTime: string; auditDateTime: string; eventID: string }) {
  return (
    new Date(left.eventDateTime).getTime() - new Date(right.eventDateTime).getTime() ||
    new Date(left.auditDateTime).getTime() - new Date(right.auditDateTime).getTime() ||
    left.eventID.localeCompare(right.eventID)
  );
}

function parseOptionalDate(value: string | null) {
  if (!value)
    return null;

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

function addExclusionStatus(event: HoldingHistoryEvent, valuationDateTime: Date | null, auditDateTime: Date | null): ValuationHistoryEvent | null {
  if (valuationDateTime && new Date(event.eventDateTime).getTime() > valuationDateTime.getTime()) {
    return {
      ...event,
      applicationStatus: 'omitted' as const,
      exclusionKind: 'valuationDate' as const,
      exclusionReason: 'After valuation date'
    };
  }

  if (auditDateTime && new Date(event.auditDateTime).getTime() > auditDateTime.getTime()) {
    return {
      ...event,
      applicationStatus: 'omitted' as const,
      exclusionKind: 'auditDate' as const,
      exclusionReason: 'After audit date'
    };
  }

  return null;
}

function isTransactionMovementEvent(event: TransactionReferenceEvent) {
  return event.$type === 'TransactionCreditEvent' || event.$type === 'TransactionDebitEvent';
}

function enrichCancellationEvent(event: TransactionReferenceEvent, movementByEventID: Map<string, TransactionReferenceEvent>): TransactionReferenceEvent {
  if (event.$type !== 'TransactionCancellationEvent' || event.holdingID)
    return event;

  const cancelledMovement = [
    event.cancelledEventID ?? '',
    ...(event.cancelledIDGroup ?? [])
  ].map((eventID) => movementByEventID.get(eventID)).find(Boolean);

  if (!cancelledMovement)
    return event;

  return {
    ...event,
    accountID: event.accountID ?? cancelledMovement.accountID,
    holdingID: cancelledMovement.holdingID,
    instrumentID: event.instrumentID ?? cancelledMovement.instrumentID
  };
}

function normalizeHoldingEvent(event: Record<string, unknown>): HoldingReferenceEvent {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    holdingID: readString(event, 'holdingID', 'holdingId', 'HoldingID'),
    accountID: readString(event, 'accountID', 'accountId', 'AccountID'),
    instrumentID: readString(event, 'instrumentID', 'instrumentId', 'InstrumentID'),
    holdingKind: readHoldingKind(event),
    name: readString(event, 'name', 'Name'),
    active: readOptionalBoolean(event, 'active', 'Active'),
    default: readOptionalBoolean(event, 'default', 'Default'),
    bankName: readString(event, 'bankName', 'BankName'),
    accountName: readString(event, 'accountName', 'AccountName'),
    sortCode: readString(event, 'sortCode', 'SortCode'),
    accountNumber: readString(event, 'accountNumber', 'AccountNumber'),
    propertyDetails: readEventPropertyDetails(event)
  };
}

function readString(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'string')
      return value;
  }

  return '';
}

function readHoldingKind(source: Record<string, unknown>) {
  const value = readString(source, 'holdingKind', 'HoldingKind');
  return holdingKinds.includes(value as HoldingKind) ? value as HoldingKind : undefined;
}

function readOptionalBoolean(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'boolean')
      return value;
  }

  return undefined;
}
