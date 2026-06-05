import { getTicketEvents, readApplicationStatus, readEventPropertyDetails } from '$lib/server/api';
import { json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
  const ticketNumber = readTicketNumber(url.searchParams.get('ticketNumber'));

  if (!ticketNumber)
    return json({ message: 'Ticket number is required.' }, { status: 400 });

  const history = (await getTicketEvents(fetch, {
    ticketNumber,
    valuationDateTime: url.searchParams.get('valuationDateTime'),
    auditDateTime: url.searchParams.get('auditDateTime')
  })).map(normalizeTicketEvent);

  return json(history);
};

function readTicketNumber(value: string | null) {
  const ticketNumber = Number(value);
  return Number.isInteger(ticketNumber) && ticketNumber > 0 ? ticketNumber : 0;
}

function normalizeTicketEvent(event: Record<string, unknown>) {
  return {
    $type: readString(event, '$type', 'type', 'Type'),
    applicationStatus: readApplicationStatus(event),
    eventID: readString(event, 'eventID', 'eventId', 'EventID', 'id', 'Id'),
    userID: readString(event, 'userID', 'userId', 'UserID'),
    eventDateTime: readString(event, 'eventDateTime', 'EventDateTime'),
    auditDateTime: readString(event, 'auditDateTime', 'AuditDateTime'),
    reason: readString(event, 'reason', 'Reason'),
    ticketNumber: readNumber(event, 'ticketNumber', 'TicketNumber'),
    details: readRecord(event, 'details', 'Details'),
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

function readRecord(source: Record<string, unknown>, ...keys: string[]): Record<string, unknown> {
  for (const key of keys) {
    const value = source[key];

    if (value && typeof value === 'object' && !Array.isArray(value))
      return value as Record<string, unknown>;
  }

  return {};
}
