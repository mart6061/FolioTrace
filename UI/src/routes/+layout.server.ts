import { clampFutureInputDateTime, nowForInput, toApiDateTime } from '$lib/dates';
import { defaultUserBookmarks } from '$lib/bookmarks';
import { defaultUserMenuPreferences } from '$lib/menuPreferences';
import { isPublicPagePath } from '$lib/publicRoutes';
import { requireCurrentUser } from '$lib/server/auth';
import { getSystemVersion, getUserBookmarks, getUserMenuPreferences } from '$lib/server/api';
import { getUiVersion } from '$lib/server/version';
import type { LayoutServerLoad } from './$types';

let cachedApiVersion: string | null = null;
let apiVersionRequest: Promise<string> | null = null;

export const load: LayoutServerLoad = async ({ fetch, locals, url }) => {
  const uiVersion = getUiVersion();

  if (isPublicPagePath(url.pathname))
    return {
      apiVersion: 'unavailable',
      currentUser: null,
      menuPreferences: null,
      publicPage: true,
      userBookmarks: null,
      uiVersion
    };

  const currentUser = requireCurrentUser(locals);
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  let apiVersion = 'unavailable';
  let menuPreferences = defaultUserMenuPreferences(currentUser.userID);
  let userBookmarks = defaultUserBookmarks(currentUser.userID);

  try {
    apiVersion = await getApiVersion(fetch);
  } catch {
    apiVersion = 'unavailable';
  }

  try {
    const eventDateTime = toApiDateTime(nowForInput());
    const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    [menuPreferences, userBookmarks] = await Promise.all([
      getUserMenuPreferences(fetch, currentUser.userID, eventDateTime, apiAuditDateTime),
      getUserBookmarks(fetch, currentUser.userID, eventDateTime, apiAuditDateTime)
    ]);
  } catch {
    menuPreferences = defaultUserMenuPreferences(currentUser.userID);
    userBookmarks = defaultUserBookmarks(currentUser.userID);
  }

  return {
    apiVersion,
    currentUser,
    menuPreferences,
    publicPage: false,
    userBookmarks,
    uiVersion
  };
};

async function getApiVersion(fetchApi: typeof fetch) {
  if (cachedApiVersion)
    return cachedApiVersion;

  apiVersionRequest ??= getSystemVersion(fetchApi).then((systemVersion) => {
    cachedApiVersion = systemVersion.apiVersion;
    return cachedApiVersion;
  }).finally(() => {
    apiVersionRequest = null;
  });

  return apiVersionRequest;
}
