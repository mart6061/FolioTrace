import { getCountryEvents } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const alpha2 = (url.searchParams.get('alpha2') || '').trim().toUpperCase();
  const valuationDateTime = parseOptionalDate(url.searchParams.get('valuationDateTime'));
  const auditDateTime = parseOptionalDate(url.searchParams.get('auditDateTime'));

  if (!alpha2)
    return json({ message: 'Alpha-2 code is required.' }, { status: 400 });

  const events = await getCountryEvents(fetch);
  const history = events
    .map(normalizeCountryEvent)
    .filter((event) => event.alpha2.toUpperCase() === alpha2)
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

function normalizeCountryEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    alpha2: readString(event, 'alpha2', 'Alpha2'),
    alpha3: readString(event, 'alpha3', 'Alpha3'),
    numeric: readOptionalNumber(event, 'numeric', 'Numeric'),
    name: readString(event, 'name', 'Name'),
    flag: readRecord(event, 'flag', 'Flag')
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

function readOptionalNumber(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'number')
      return value;
  }

  return undefined;
}

function readRecord(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (value && typeof value === 'object')
      return value as Record<string, unknown>;
  }

  return null;
}
