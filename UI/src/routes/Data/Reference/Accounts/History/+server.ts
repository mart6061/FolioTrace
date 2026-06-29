import { getAccountEvents, getTransactionEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const accountID = (url.searchParams.get('accountID') || '').trim();

  if (!accountID)
    return json({ message: 'Account ID is required.' }, { status: 400 });

  const historyFilters = {
    accountID,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  };
  const [accountEvents, transactionEvents] = await Promise.all([
    getAccountEvents(fetch, historyFilters),
    getTransactionEvents(fetch, historyFilters)
  ]);

  const history = [
    ...accountEvents.map(normalizeAccountEvent),
    ...transactionEvents
  ].sort(compareEvents);

  return json(history);
};

function compareEvents(left: { eventDateTime: string; auditDateTime: string; eventID: string }, right: { eventDateTime: string; auditDateTime: string; eventID: string }) {
  return (
    new Date(left.eventDateTime).getTime() - new Date(right.eventDateTime).getTime() ||
    new Date(left.auditDateTime).getTime() - new Date(right.auditDateTime).getTime() ||
    left.eventID.localeCompare(right.eventID)
  );
}

function normalizeAccountEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    accountID: readString(event, 'accountID', 'accountId', 'AccountID'),
    name: readString(event, 'name', 'Name'),
    formalName: readString(event, 'formalName', 'FormalName'),
    bookCurrency: readString(event, 'bookCurrency', 'BookCurrency'),
    bookCostBasis: readString(event, 'bookCostBasis', 'BookCostBasis'),
    active: readOptionalBoolean(event, 'active', 'Active'),
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
