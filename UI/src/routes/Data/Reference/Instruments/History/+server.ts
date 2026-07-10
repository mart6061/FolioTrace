import { getInstrumentEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ fetch, url }) => {
  const instrumentID = (url.searchParams.get('instrumentID') || '').trim();

  if (!instrumentID)
    return json({ message: 'Instrument ID is required.' }, { status: 400 });

  const history = (await getInstrumentEvents(fetch, {
    instrumentID,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  })).map(normalizeInstrumentEvent);

  return json(history);
};

function normalizeInstrumentEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
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
    terms: readOptionalObject(event, 'terms', 'Terms'),
    propertyDetails: readEventPropertyDetails(event)
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
