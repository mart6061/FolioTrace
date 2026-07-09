import { getFXRateEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ fetch, url }) => {
  const pair = (url.searchParams.get('pair') || '').trim();

  if (!pair)
    return json({ message: 'Pair is required.' }, { status: 400 });

  const history = (await getFXRateEvents(fetch, {
    pair,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime'),
    rateValuationDateTime: url.searchParams.get('rateValuationDateTime')
  }))
    .map((event) => normalizeFXRateEvent(event as Record<string, unknown>));

  return json(history);
};

function normalizeFXRateEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    pair: readString(event, 'pair', 'Pair'),
    displayPair: readString(event, 'displayPair', 'DisplayPair'),
    summary: summarizeFXRateEvent(event),
    propertyDetails: readEventPropertyDetails(event)
  };
}

function summarizeFXRateEvent(event: Record<string, unknown>) {
  const price = readRecord(event, 'price', 'Price');
  return [
    `Bid ${formatNumber(readPriceNumber(price, 'bid', 'Bid'))}`,
    `Mid ${formatNumber(readPriceNumber(price, 'mid', 'Mid'))}`,
    `Ask ${formatNumber(readPriceNumber(price, 'ask', 'Ask'))}`
  ].join(' | ');
}

function readPriceNumber(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'number')
      return value;

    if (value && typeof value === 'object') {
      const nestedValue = readNumber(value as Record<string, unknown>, 'value', 'Value');
      if (typeof nestedValue === 'number')
        return nestedValue;
    }
  }

  return undefined;
}

function formatNumber(value: number | undefined) {
  return typeof value === 'number' ? value.toLocaleString(undefined, { maximumFractionDigits: 8 }) : '-';
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

  return undefined;
}

function readRecord(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (value && typeof value === 'object')
      return value as Record<string, unknown>;
  }

  return {};
}
