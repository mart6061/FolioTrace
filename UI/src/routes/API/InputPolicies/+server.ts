import { getInputPolicies } from '$lib/server/api';
import { requireCurrentUser } from '$lib/server/auth';
import type { InputControlKind } from '$lib/types';
import { json, type RequestHandler } from '@sveltejs/kit';

export const GET: RequestHandler = async ({ fetch, locals, url }) => {
  const currentUser = requireCurrentUser(locals);
  const eventDateTime = url.searchParams.get('eventDateTime') ?? '';

  if (!eventDateTime)
    return json({ message: 'eventDateTime is required.' }, { status: 400 });

  const policies = await getInputPolicies(fetch, {
    accountID: url.searchParams.get('accountID'),
    allowNegative: readBoolean(url.searchParams.get('allowNegative')),
    auditDateTime: url.searchParams.get('auditDateTime'),
    controlKinds: readControlKinds(url.searchParams.get('controlKinds')),
    currency: url.searchParams.get('currency'),
    eventDateTime,
    userID: url.searchParams.get('userID') ?? currentUser.userID
  });

  return json(policies);
};

function readBoolean(value: string | null) {
  if (value === null)
    return null;

  return value.toLowerCase() === 'true';
}

function readControlKinds(value: string | null): InputControlKind[] {
  if (!value)
    return ['Quantity', 'Money'];

  return value
    .split(',')
    .map((item) => item.trim())
    .filter((item): item is InputControlKind => item === 'Quantity' || item === 'Money');
}
