import { error } from '@sveltejs/kit';
import { createHash } from 'node:crypto';
import { nowForInput, toApiDateTime } from '$lib/dates';
import { getUserEvents, postUserCreatedEvent } from '$lib/server/api';
import type { CurrentUser } from '$lib/authTypes';

type WorkOSUserLike = {
  id?: string | null;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
};

const userIDNamespace = 'FolioTrace.WorkOS.EmailUserID.v1:';
const pendingUserCreates = new Map<string, Promise<void>>();

export function requireCurrentUser(locals: App.Locals) {
  if (!locals.currentUser)
    throw error(401, 'Authentication required.');

  return locals.currentUser;
}

export function currentUserFromWorkOSUser(user: WorkOSUserLike): CurrentUser {
  const email = normalizeEmail(user.email);
  const workosUserID = user.id?.trim() ?? '';

  if (!email)
    throw new Error('Authenticated WorkOS user does not have an email address.');

  if (!workosUserID)
    throw new Error('Authenticated WorkOS user does not have an id.');

  const displayName = [user.firstName, user.lastName].filter(Boolean).join(' ').trim() || email;

  return {
    userID: deriveUserIDFromEmail(email),
    workosUserID,
    email,
    displayName
  };
}

export async function ensureFolioTraceUser(fetchApi: typeof fetch, currentUser: CurrentUser) {
  const existingCreate = pendingUserCreates.get(currentUser.userID);

  if (existingCreate) {
    await existingCreate;
    return;
  }

  const createPromise = ensureFolioTraceUserInner(fetchApi, currentUser)
    .finally(() => pendingUserCreates.delete(currentUser.userID));

  pendingUserCreates.set(currentUser.userID, createPromise);
  await createPromise;
}

function normalizeEmail(email: string | null | undefined) {
  return email?.trim().toLowerCase() ?? '';
}

function deriveUserIDFromEmail(email: string) {
  const bytes = createHash('sha256').update(`${userIDNamespace}${email}`).digest().subarray(0, 16);
  bytes[6] = (bytes[6] & 0x0f) | 0x50;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = bytes.toString('hex');

  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

async function ensureFolioTraceUserInner(fetchApi: typeof fetch, currentUser: CurrentUser) {
  const events = await getUserEvents(fetchApi, currentUser.userID);
  const hasCreatedEvent = events.some((event) =>
    event.$type === 'UserCreatedEvent'
      || event.type === 'UserCreatedEvent'
      || event.Type === 'UserCreatedEvent');

  if (hasCreatedEvent)
    return;

  const eventDateTime = toApiDateTime(nowForInput());

  await postUserCreatedEvent(fetchApi, {
    userID: currentUser.userID,
    eventDateTime,
    reason: 'Create WorkOS user',
    displayName: currentUser.displayName,
    displayPreferences: {
      darkMode: false,
      rememberTraceDate: false
    },
    valuationPreferences: {
      valuationDate: eventDateTime,
      showIncome: true,
      showBook: true
    }
  });
}
