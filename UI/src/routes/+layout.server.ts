import { clampFutureInputDateTime, nowForInput, toApiDateTime } from '$lib/dates';
import { defaultUserMenuPreferences, systemUserID } from '$lib/menuPreferences';
import { getSystemVersion, getUserMenuPreferences } from '$lib/server/api';
import { getUiVersion } from '$lib/server/version';

let cachedApiVersion: string | null = null;
let apiVersionRequest: Promise<string> | null = null;

export const load = async ({ fetch, url }) => {
  const uiVersion = getUiVersion();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  let apiVersion = 'unavailable';
  let menuPreferences = defaultUserMenuPreferences();

  try {
    apiVersion = await getApiVersion(fetch);
  } catch {
    apiVersion = 'unavailable';
  }

  try {
    menuPreferences = await getUserMenuPreferences(
      fetch,
      systemUserID,
      toApiDateTime(nowForInput()),
      auditDateTime ? toApiDateTime(auditDateTime) : null
    );
  } catch {
    menuPreferences = defaultUserMenuPreferences();
  }

  return {
    apiVersion,
    menuPreferences,
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
