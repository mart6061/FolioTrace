import { getCurrencyEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const alphabeticCode = (url.searchParams.get('alphabeticCode') || '').trim().toUpperCase();

  if (!alphabeticCode)
    return json({ message: 'Alphabetic code is required.' }, { status: 400 });

  const history = (await getCurrencyEvents(fetch, {
    alphabeticCode,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  })).map(normalizeCurrencyEvent);

  return json(history);
};

function normalizeCurrencyEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    alphabeticCode: readString(event, 'alphabeticCode', 'AlphabeticCode'),
    numericCode: readNumber(event, 'numericCode', 'NumericCode'),
    decimalPlace: readNumber(event, 'decimalPlace', 'DecimalPlace'),
    name: readString(event, 'name', 'Name'),
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

function readNumber(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'number')
      return value;
  }

  return 0;
}
