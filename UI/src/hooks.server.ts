import { redirect, type Handle, type HandleFetch } from '@sveltejs/kit';
import { isPublicPagePath } from '$lib/publicRoutes';
import { getApiBaseUrl } from '$lib/server/api';
import { getApiReadiness } from '$lib/server/apiReadiness';
import { isCurrentUser, type CurrentUser } from '$lib/authTypes';

const apiBaseUrl = getApiBaseUrl();
const requestIdHeader = 'X-FolioTrace-Request-Id';
const parentRequestIdHeader = 'X-FolioTrace-Parent-Request-Id';

export const handle: Handle = async ({ event, resolve }) => {
  event.locals.currentUser = null;
  event.locals.requestTraceId = crypto.randomUUID();

  if (isPublicPagePath(event.url.pathname) || isPublicPath(event.url.pathname))
    return resolve(event);

  const readiness = await getApiReadiness(event.fetch);
  if (!readiness.ready) {
    const pendingUrl = new URL('/StartPending', event.url);
    pendingUrl.searchParams.set('returnTo', `${event.url.pathname}${event.url.search}`);
    throw redirect(302, `${pendingUrl.pathname}${pendingUrl.search}`);
  }

  let currentUser: CurrentUser | null;
  try {
    currentUser = await getCurrentUser(event.fetch, event.request.headers.get('cookie'));
  } catch (error) {
    if (error instanceof ApiSessionError)
      return new Response(error.message, { status: error.status });

    throw error;
  }

  if (currentUser) {
    event.locals.currentUser = currentUser;
    return resolve(event);
  }

  if (event.url.pathname.startsWith('/API/'))
    return new Response('Authentication required.', { status: 401 });

  throw redirect(302, getSsoUrl(event.url));
};

export const handleFetch: HandleFetch = async ({ event, request, fetch }) => {
  if (!isConfiguredApiUrl(request.url))
    return fetch(request);

  const cookie = event.request.headers.get('cookie');
  if (cookie)
    request.headers.set('cookie', cookie);

  const downstreamRequestId = crypto.randomUUID();
  request.headers.set(requestIdHeader, downstreamRequestId);
  request.headers.set(parentRequestIdHeader, event.locals.requestTraceId);

  return fetch(request);
};

async function getCurrentUser(fetchApi: typeof fetch, cookie: string | null) {
  const headers = new Headers();
  if (cookie)
    headers.set('cookie', cookie);

  let response: Response;
  try {
    response = await fetchApi(`${apiBaseUrl}/Auth/Session`, {
      headers,
      credentials: 'include'
    });
  } catch {
    throw new ApiSessionError(
      503,
      `FolioTrace API is not reachable at ${apiBaseUrl}. Start the API and refresh the page.`
    );
  }

  if (response.status === 401)
    return null;

  if (response.status === 429)
    throw new ApiSessionError(429, 'Authentication is temporarily rate limited. Wait a few minutes before trying again.');

  if (!response.ok)
    throw new ApiSessionError(response.status, `API session check returned ${response.status} ${response.statusText}`);

  const body: unknown = await response.json();
  if (!isCurrentUser(body))
    throw new ApiSessionError(502, 'API session response did not match the expected shape.');

  return body;
}

function getSsoUrl(url: URL) {
  const ssoUrl = new URL(`${apiBaseUrl}/Auth/SSO`);
  ssoUrl.searchParams.set('returnTo', `${url.pathname}${url.search}`);
  return ssoUrl.toString();
}

function isPublicPath(pathname: string) {
  return pathname === '/sign-out'
    || pathname === '/auth/error'
    || pathname === '/health'
    || pathname === '/StartPending'
    || pathname.startsWith('/_app/')
    || pathname.startsWith('/brand/')
    || pathname === '/favicon.ico'
    || pathname === '/robots.txt';
}

function isConfiguredApiUrl(url: string) {
  return url.startsWith(`${apiBaseUrl}/`);
}

class ApiSessionError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
  }
}
