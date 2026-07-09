import { redirect, type Handle, type HandleFetch } from '@sveltejs/kit';
import { isPublicPagePath } from '$lib/publicRoutes';
import { getApiBaseUrl } from '$lib/server/api';
import type { CurrentUser } from '$lib/authTypes';

const apiBaseUrl = getApiBaseUrl();

export const handle: Handle = async ({ event, resolve }) => {
  event.locals.currentUser = null;

  if (isPublicPagePath(event.url.pathname) || isPublicPath(event.url.pathname))
    return resolve(event);

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
  if (isConfiguredApiUrl(request.url)) {
    const cookie = event.request.headers.get('cookie');
    if (cookie)
      request.headers.set('cookie', cookie);
  }

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

  return (await response.json()) as CurrentUser;
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
