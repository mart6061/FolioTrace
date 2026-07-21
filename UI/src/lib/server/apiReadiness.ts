import { getApiBaseUrl } from '$lib/server/api';

const readyCacheMilliseconds = 10_000;
const apiBaseUrl = getApiBaseUrl();

type ApiReadiness = {
  ready: boolean;
};

let readyUntil = 0;
let readinessRequest: Promise<ApiReadiness> | null = null;

export function getApiReadiness(fetchApi: typeof fetch): Promise<ApiReadiness> {
  if (Date.now() < readyUntil)
    return Promise.resolve({ ready: true });

  if (readinessRequest)
    return readinessRequest;

  const request = requestApiReadiness(fetchApi);
  readinessRequest = request;

  void request.finally(() => {
    if (readinessRequest === request)
      readinessRequest = null;
  });

  return request;
}

async function requestApiReadiness(fetchApi: typeof fetch): Promise<ApiReadiness> {
  try {
    const response = await fetchApi(`${apiBaseUrl}/System/Health`);
    if (!response.ok)
      return { ready: false };

    const body: unknown = await response.json();
    const ready = typeof body === 'object' && body !== null && 'ready' in body && body.ready === true;
    readyUntil = ready ? Date.now() + readyCacheMilliseconds : 0;

    return { ready };
  } catch {
    readyUntil = 0;
    return { ready: false };
  }
}
