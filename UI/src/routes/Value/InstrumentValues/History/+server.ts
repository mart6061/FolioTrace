import { getInstrumentPriceEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ fetch, url }) => {
  const instrumentID = (url.searchParams.get('instrumentID') || '').trim();

  if (!instrumentID)
    return json({ message: 'Instrument ID is required.' }, { status: 400 });

  const history = (await getInstrumentPriceEvents(fetch, {
    instrumentID,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime'),
    priceValuationDateTime: url.searchParams.get('priceValuationDateTime')
  })).map((event) => normalizeInstrumentValueEvent(event as Record<string, unknown>, 'Price'));

  return json(history);
};

function normalizeInstrumentValueEvent(event: Record<string, unknown>, valueKind: 'Price' | 'Income') {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    instrumentID: readString(event, 'instrumentID', 'InstrumentID'),
    valueKind,
    summary: summarizeValueEvent(event, valueKind),
    propertyDetails: readEventPropertyDetails(event)
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
