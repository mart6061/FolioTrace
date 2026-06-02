import { getInstrumentEvents } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const instrumentID = (url.searchParams.get('instrumentID') || '').trim().toLowerCase();
  const valuationDateTime = parseOptionalDate(url.searchParams.get('valuationDateTime'));
  const auditDateTime = parseOptionalDate(url.searchParams.get('auditDateTime'));

  if (!instrumentID)
    return json({ message: 'Instrument ID is required.' }, { status: 400 });

  const events = await getInstrumentEvents(fetch);
  const history = events
    .map(normalizeInstrumentEvent)
    .filter((event) => event.instrumentID.toLowerCase() === instrumentID)
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

function normalizeInstrumentEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    instrumentID: readString(event, 'instrumentID', 'instrumentId', 'InstrumentID'),
    name: readString(event, 'name', 'Name'),
    formalName: readString(event, 'formalName', 'FormalName'),
    exchange: readString(event, 'exchange', 'Exchange'),
    cfi: readString(event, 'cfi', 'CFI'),
    logo: readOptionalObject(event, 'logo', 'Logo'),
    active: readOptionalBoolean(event, 'active', 'Active'),
    incomeCountry: readString(event, 'incomeCountry', 'IncomeCountry'),
    priceCountry: readString(event, 'priceCountry', 'PriceCountry'),
    priceCurrency: readString(event, 'priceCurrency', 'PriceCurrency'),
    identifier: readIdentifier(event),
    identifierType: readIdentifierType(event),
    terms: readOptionalObject(event, 'terms', 'Terms')
  };
}

function readIdentifier(source: Record<string, unknown>) {
  const identifier = readOptionalObject(source, 'identifier', 'Identifier');

  if (!identifier)
    return null;

  return {
    type: readIdentifierType(identifier),
    value: readString(identifier, 'value', 'Value')
  };
}

function readIdentifierType(source: Record<string, unknown>) {
  for (const key of ['identifierType', 'IdentifierType', 'type', 'Type']) {
    const value = source[key];

    if (typeof value === 'string' || typeof value === 'number')
      return value;
  }

  return '';
}

function readString(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'string')
      return value;

    if (typeof value === 'number')
      return String(value);
  }

  return '';
}

function readOptionalObject(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (value && typeof value === 'object' && !Array.isArray(value))
      return value as Record<string, unknown>;
  }

  return null;
}

function readOptionalBoolean(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'boolean')
      return value;
  }

  return undefined;
}
