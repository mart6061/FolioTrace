import { getHoldingEvents, getTransactionEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ fetch, url }) => {
  const holdingID = (url.searchParams.get('holdingID') || '').trim();

  if (!holdingID)
    return json({ message: 'Holding ID is required.' }, { status: 400 });

  const historyFilters = {
    holdingID,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  };
  const [holdingEvents, transactionEvents] = await Promise.all([
    getHoldingEvents(fetch, historyFilters),
    getTransactionEvents(fetch, historyFilters)
  ]);

  const holdingHistory = holdingEvents
    .map(normalizeHoldingEvent);
  const transactionHistory = transactionEvents;
  const history = [...holdingHistory, ...transactionHistory].sort(compareEvents);

  return json(history);
};

function compareEvents(left: { eventDateTime: string; auditDateTime: string; eventID: string }, right: { eventDateTime: string; auditDateTime: string; eventID: string }) {
  return (
    new Date(left.eventDateTime).getTime() - new Date(right.eventDateTime).getTime() ||
    new Date(left.auditDateTime).getTime() - new Date(right.auditDateTime).getTime() ||
    left.eventID.localeCompare(right.eventID)
  );
}

function normalizeHoldingEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    holdingID: readString(event, 'holdingID', 'holdingId', 'HoldingID'),
    accountID: readString(event, 'accountID', 'accountId', 'AccountID'),
    instrumentID: readString(event, 'instrumentID', 'instrumentId', 'InstrumentID'),
    holdingKind: readString(event, 'holdingKind', 'HoldingKind'),
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

function readOptionalBoolean(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'boolean')
      return value;
  }

  return undefined;
}
