import { getAccountEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const accountID = (url.searchParams.get('accountID') || '').trim();

  if (!accountID)
    return json({ message: 'Account ID is required.' }, { status: 400 });

  const history = (await getAccountEvents(fetch, {
    accountID,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  })).map(normalizeAccountEvent);

  return json(history);
};

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
