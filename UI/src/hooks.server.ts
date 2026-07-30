import { redirect, type Handle, type HandleFetch } from '@sveltejs/kit';
import { isPublicPagePath } from '$lib/publicRoutes';
import { getApiBaseUrl } from '$lib/server/api';
import { getApiReadiness } from '$lib/server/apiReadiness';
import { isCurrentUser, type CurrentUser } from '$lib/currentUser';

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

  let currentUser: CurrentUser;
  try {
    currentUser = await getCurrentUser(event.fetch);
  } catch (error) {
    if (error instanceof CurrentUserError)
      return new Response(error.message, { status: error.status });

    throw error;
  }

  event.locals.currentUser = currentUser;
  return resolve(event);
};

export const handleFetch: HandleFetch = async ({ event, request, fetch }) => {
  if (!isConfiguredApiUrl(request.url))
    return fetch(request);

  const downstreamRequestId = crypto.randomUUID();
  request.headers.set(requestIdHeader, downstreamRequestId);
  request.headers.set(parentRequestIdHeader, event.locals.requestTraceId);

  return fetch(request);
};

async function getCurrentUser(fetchApi: typeof fetch) {
  let response: Response;
  try {
    response = await fetchApi(`${apiBaseUrl}/Users/Current`);
  } catch {
    throw new CurrentUserError(
      503,
      `FolioTrace API is not reachable at ${apiBaseUrl}. Start the API and refresh the page.`
    );
  }

  if (!response.ok)
    throw new CurrentUserError(response.status, `Current user request returned ${response.status} ${response.statusText}`);

  const body: unknown = await response.json();
  if (!isCurrentUser(body))
    throw new CurrentUserError(502, 'Current user response did not match the expected shape.');

  return body;
}

function isPublicPath(pathname: string) {
  return pathname === '/health'
    || pathname === '/StartPending'
    || pathname.startsWith('/_app/')
    || pathname.startsWith('/brand/')
    || pathname === '/favicon.ico'
    || pathname === '/robots.txt';
}

function isConfiguredApiUrl(url: string) {
  return url.startsWith(`${apiBaseUrl}/`);
}

class CurrentUserError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
  }
}
