import { getSystemVersion } from '$lib/server/api';
import { getUiVersion } from '$lib/server/version';

let cachedApiVersion: string | null = null;
let apiVersionRequest: Promise<string> | null = null;

export const load = async ({ fetch }) => {
  const uiVersion = getUiVersion();

  try {
    const apiVersion = await getApiVersion(fetch);

    return {
      apiVersion,
      uiVersion
    };
  } catch {
    return {
      apiVersion: 'unavailable',
      uiVersion
    };
  }
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
