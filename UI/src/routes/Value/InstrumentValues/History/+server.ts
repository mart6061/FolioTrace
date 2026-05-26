import { getInstrumentIncomeEvents, getInstrumentPriceEvents } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const instrumentID = (url.searchParams.get('instrumentID') || '').trim();
  const valuationDateTime = parseOptionalDate(url.searchParams.get('valuationDateTime'));
  const priceValuationDateTime = parseOptionalDate(url.searchParams.get('priceValuationDateTime'));
  const auditDateTime = parseOptionalDate(url.searchParams.get('auditDateTime'));

  if (!instrumentID)
    return json({ message: 'Instrument ID is required.' }, { status: 400 });

  const [priceEvents, incomeEvents] = await Promise.all([
    getInstrumentPriceEvents(fetch, instrumentID),
    getInstrumentIncomeEvents(fetch, instrumentID)
  ]);

  const history = [
    ...priceEvents.map((event) => normalizeInstrumentValueEvent(event as Record<string, unknown>, 'Price')),
    ...incomeEvents.map((event) => normalizeInstrumentValueEvent(event as Record<string, unknown>, 'Income'))
  ]
    .filter((event) => event.instrumentID.toLowerCase() === instrumentID.toLowerCase())
    .filter((event) => isInValuationScope(event, valuationDateTime))
    .filter((event) => isDisplayedPriceEvent(event, priceValuationDateTime))
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

function isDisplayedPriceEvent(event: { eventDateTime: string; valueKind: 'Price' | 'Income' }, priceValuationDateTime: Date | null) {
  if (event.valueKind !== 'Price')
    return false;

  if (!priceValuationDateTime)
    return true;

  return new Date(event.eventDateTime).getTime() === priceValuationDateTime.getTime();
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

function normalizeInstrumentValueEvent(event: Record<string, unknown>, valueKind: 'Price' | 'Income') {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    instrumentID: readString(event, 'instrumentID', 'InstrumentID'),
    valueKind,
    summary: summarizeValueEvent(event, valueKind)
  };
}

function summarizeValueEvent(event: Record<string, unknown>, valueKind: 'Price' | 'Income') {
  const payload = readRecord(event, valueKind === 'Price' ? 'price' : 'income', valueKind === 'Price' ? 'Price' : 'Income');
  const type = readString(payload, '$type', 'type', 'priceType', 'incomeType', 'PriceType', 'IncomeType');

  if (type === 'InstrumentPriceEquity')
    return [
      `Bid ${readAmount(payload, 'bid', 'Bid')}`,
      `Mid ${readAmount(payload, 'mid', 'Mid')}`,
      `Ask ${readAmount(payload, 'ask', 'Ask')}`,
      `NAV ${readAmount(payload, 'nav', 'Nav')}`
    ].join(' | ');

  if (type === 'InstrumentPriceFixedIncome')
    return `Clean ${readAmount(payload, 'cleanPrice', 'CleanPrice')}`;

  if (type === 'InstrumentPriceCash')
    return `Price ${readAmount(payload, 'price', 'Price')}`;

  if (type === 'InstrumentIncomeEquity')
    return `Dividend ${readAmount(payload, 'dividendAmount', 'DividendAmount')} ${readString(payload, 'dividendType', 'DividendType')}`.trim();

  if (type === 'InstrumentIncomeFixedIncome')
    return `Accrued ${readAmount(payload, 'accruedInterest', 'AccruedInterest')}`;

  if (type === 'InstrumentIncomeCash') {
    const income = readRecord(payload, 'income', 'Income');
    const value = readNumber(income, 'value', 'Value');
    return `Income ${formatNumber(value)}`;
  }

  return type || valueKind;
}

function readAmount(source: Record<string, unknown>, ...keys: string[]) {
  const value = readRecord(source, ...keys);
  return formatNumber(readNumber(value, 'amount', 'Amount'));
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
