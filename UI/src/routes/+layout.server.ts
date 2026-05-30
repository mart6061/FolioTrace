import { clampFutureInputDateTime, nowForInput, toApiDateTime } from '$lib/dates';
import { defaultUserBookmarks } from '$lib/bookmarks';
import { defaultUserMenuPreferences, systemUserID } from '$lib/menuPreferences';
import { getSystemVersion, getUserBookmarks, getUserMenuPreferences } from '$lib/server/api';
import { getUiVersion } from '$lib/server/version';

let cachedApiVersion: string | null = null;
let apiVersionRequest: Promise<string> | null = null;

export const load = async ({ fetch, url }) => {
  const uiVersion = getUiVersion();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  let apiVersion = 'unavailable';
  let menuPreferences = defaultUserMenuPreferences();
  let userBookmarks = defaultUserBookmarks();

  try {
    apiVersion = await getApiVersion(fetch);
  } catch {
    apiVersion = 'unavailable';
  }

  try {
    const eventDateTime = toApiDateTime(nowForInput());
    const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
    [menuPreferences, userBookmarks] = await Promise.all([
      getUserMenuPreferences(fetch, systemUserID, eventDateTime, apiAuditDateTime),
      getUserBookmarks(fetch, systemUserID, eventDateTime, apiAuditDateTime)
    ]);
  } catch {
    menuPreferences = defaultUserMenuPreferences();
    userBookmarks = defaultUserBookmarks();
  }

  return {
    apiVersion,
    menuPreferences,
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
