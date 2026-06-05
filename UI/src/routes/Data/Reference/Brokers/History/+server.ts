import { getBrokerEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const lei = (url.searchParams.get('lei') || '').trim().toUpperCase();

  if (!lei)
    return json({ message: 'LEI is required.' }, { status: 400 });

  const history = (await getBrokerEvents(fetch, {
    lei,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  })).map(normalizeBrokerEvent);

  return json(history);
};

function normalizeBrokerEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    lei: readString(event, 'lei', 'LEI'),
    name: readString(event, 'name', 'Name'),
    commission: readNumber(event, 'commission', 'Commission'),
    active: readBoolean(event, 'active', 'Active'),
    approvedDateTime: readString(event, 'approvedDateTime', 'ApprovedDateTime'),
    nextReview: readString(event, 'nextReview', 'NextReview'),
    notes: readString(event, 'notes', 'Notes'),
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

function readBoolean(source: Record<string, unknown>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === 'boolean')
      return value;
  }

  return false;
}
