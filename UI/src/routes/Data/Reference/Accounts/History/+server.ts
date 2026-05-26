import { getAccountEvents } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const accountID = (url.searchParams.get('accountID') || '').trim().toLowerCase();
  const valuationDateTime = parseOptionalDate(url.searchParams.get('valuationDateTime'));
  const auditDateTime = parseOptionalDate(url.searchParams.get('auditDateTime'));

  if (!accountID)
    return json({ message: 'Account ID is required.' }, { status: 400 });

  const events = await getAccountEvents(fetch);
  const history = events
    .map(normalizeAccountEvent)
    .filter((event) => event.accountID.toLowerCase() === accountID)
    .filter((event) => isInValuationScope(event, valuationDateTime))
    .map((event) => addApplicationStatus(event, auditDateTime))
    .sort(compareEvents);

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

function isInValuationScope(event: { eventDateTime: string }, valuationDateTime: Date | null) {
  if (!valuationDateTime)
    return true;

  return new Date(event.eventDateTime).getTime() <= valuationDateTime.getTime();
}

function addApplicationStatus<TEvent extends { auditDateTime: string }>(event: TEvent, auditDateTime: Date | null) {
  if (!auditDateTime)
    return { ...event, applicationStatus: 'applied' as const };

  return {
    ...event,
    applicationStatus: new Date(event.auditDateTime).getTime() <= auditDateTime.getTime()
      ? 'applied' as const
      : 'omitted' as const
  };
}

function normalizeAccountEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    accountID: readString(event, 'accountID', 'accountId', 'AccountID'),
    name: readString(event, 'name', 'Name'),
    formalName: readString(event, 'formalName', 'FormalName'),
    bookCurrency: readString(event, 'bookCurrency', 'BookCurrency'),
    active: readOptionalBoolean(event, 'active', 'Active')
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
