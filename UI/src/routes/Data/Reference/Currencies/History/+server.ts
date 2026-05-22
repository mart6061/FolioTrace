import { getCurrencyEvents } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const alphabeticCode = (url.searchParams.get('alphabeticCode') || '').trim().toUpperCase();
  const valuationDateTime = parseOptionalDate(url.searchParams.get('valuationDateTime'));
  const auditDateTime = parseOptionalDate(url.searchParams.get('auditDateTime'));

  if (!alphabeticCode)
    return json({ message: 'Alphabetic code is required.' }, { status: 400 });

  const events = await getCurrencyEvents(fetch);
  const history = events
    .map(normalizeCurrencyEvent)
    .filter((event) => event.alphabeticCode.toUpperCase() === alphabeticCode)
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

function normalizeCurrencyEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    alphabeticCode: readString(event, 'alphabeticCode', 'AlphabeticCode'),
    numericCode: readNumber(event, 'numericCode', 'NumericCode'),
    decimalPlace: readNumber(event, 'decimalPlace', 'DecimalPlace'),
    name: readString(event, 'name', 'Name')
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

function readNumber(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'number')
      return value;
  }

  return 0;
}
