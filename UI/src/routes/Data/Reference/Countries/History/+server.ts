import { getCountryEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const alpha2 = (url.searchParams.get('alpha2') || '').trim().toUpperCase();

  if (!alpha2)
    return json({ message: 'Alpha-2 code is required.' }, { status: 400 });

  const history = (await getCountryEvents(fetch, {
    alpha2,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  })).map(normalizeCountryEvent);

  return json(history);
};

function normalizeCountryEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    alpha2: readString(event, 'alpha2', 'Alpha2'),
    alpha3: readString(event, 'alpha3', 'Alpha3'),
    numeric: readOptionalNumber(event, 'numeric', 'Numeric'),
    name: readString(event, 'name', 'Name'),
    flag: readRecord(event, 'flag', 'Flag'),
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
