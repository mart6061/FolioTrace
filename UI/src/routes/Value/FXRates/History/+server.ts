import { getFXRateEvents } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const pair = (url.searchParams.get('pair') || '').trim();
  const valuationDateTime = parseOptionalDate(url.searchParams.get('valuationDateTime'));
  const rateValuationDateTime = parseOptionalDate(url.searchParams.get('rateValuationDateTime'));
  const auditDateTime = parseOptionalDate(url.searchParams.get('auditDateTime'));

  if (!pair)
    return json({ message: 'Pair is required.' }, { status: 400 });

  const events = await getFXRateEvents(fetch);
  const history = events
    .map((event) => normalizeFXRateEvent(event as Record<string, unknown>))
    .filter((event) => event.pair.toLowerCase() === pair.toLowerCase())
    .filter((event) => isInValuationScope(event, valuationDateTime))
    .filter((event) => isDisplayedRateEvent(event, rateValuationDateTime))
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

function isDisplayedRateEvent(event: { eventDateTime: string }, rateValuationDateTime: Date | null) {
  if (!rateValuationDateTime)
    return true;

  return new Date(event.eventDateTime).getTime() === rateValuationDateTime.getTime();
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

function normalizeFXRateEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    pair: readString(event, 'pair', 'Pair'),
    displayPair: readString(event, 'displayPair', 'DisplayPair'),
    summary: summarizeFXRateEvent(event)
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
