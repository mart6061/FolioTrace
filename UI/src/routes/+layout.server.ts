import { clampFutureInputDateTime, nowForInput, toApiDateTime } from '$lib/dates';
import { defaultUserBookmarks } from '$lib/bookmarks';
import { defaultUserMenuPreferences } from '$lib/menuPreferences';
import { defaultUserValuationPreferences } from '$lib/valuationPreferences';
import { isPublicPagePath } from '$lib/publicRoutes';
import { requireCurrentUser } from '$lib/server/auth';
import { getApiBaseUrl, getSystemVersion, getUserBookmarks, getUserMenuPreferences, getUserValuationPreferences } from '$lib/server/api';
import { getUiVersion } from '$lib/server/version';
import type { LayoutServerLoad } from './$types';

const apiVersionTtlMs = 5 * 60 * 1000;

let cachedApiVersion: string | null = null;
let cachedApiVersionAt = 0;
let apiVersionRequest: Promise<string> | null = null;

export const load: LayoutServerLoad = async ({ fetch, locals, url }) => {
  const uiVersion = getUiVersion();

  if (isPublicPagePath(url.pathname))
    return {
      apiBaseUrl: getApiBaseUrl(),
      apiVersion: 'unavailable',
      currentUser: null,
      menuPreferences: null,
      publicPage: true,
      userBookmarks: null,
      valuationPreferences: null,
      uiVersion
    };

  const currentUser = requireCurrentUser(locals);
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  let apiVersion = 'unavailable';
  let menuPreferences = defaultUserMenuPreferences(currentUser.userID);
  let userBookmarks = defaultUserBookmarks(currentUser.userID);
  let valuationPreferences = defaultUserValuationPreferences(currentUser.userID);

  try {
    apiVersion = await getApiVersion(fetch);
  } catch {
    apiVersion = 'unavailable';
  }

  try {
    const eventDateTime = toApiDateTime(nowForInput());
    const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    [menuPreferences, userBookmarks, valuationPreferences] = await Promise.all([
      getUserMenuPreferences(fetch, currentUser.userID, eventDateTime, apiAuditDateTime),
      getUserBookmarks(fetch, currentUser.userID, eventDateTime, apiAuditDateTime),
      getUserValuationPreferences(fetch, currentUser.userID, eventDateTime, apiAuditDateTime)
    ]);
  } catch {
    menuPreferences = defaultUserMenuPreferences(currentUser.userID);
    userBookmarks = defaultUserBookmarks(currentUser.userID);
    valuationPreferences = defaultUserValuationPreferences(currentUser.userID);
  }

  return {
    apiVersion,
    apiBaseUrl: getApiBaseUrl(),
    currentUser,
    menuPreferences,
    publicPage: false,
    userBookmarks,
    valuationPreferences,
    uiVersion
  };
};

async function getApiVersion(fetchApi: typeof fetch) {
  if (cachedApiVersion && Date.now() - cachedApiVersionAt < apiVersionTtlMs)
    return cachedApiVersion;

  apiVersionRequest ??= getSystemVersion(fetchApi).then((systemVersion) => {
    cachedApiVersion = systemVersion.apiVersion;
    cachedApiVersionAt = Date.now();
    return cachedApiVersion;
  }).finally(() => {
    apiVersionRequest = null;
  });

  return apiVersionRequest;
}
