import { authKitHandle, configureAuthKit } from '@workos/authkit-sveltekit';
import { dev } from '$app/environment';
import { env } from '$env/dynamic/private';
import { redirect, type Handle, type RequestEvent } from '@sveltejs/kit';
import { sequence } from '@sveltejs/kit/hooks';
import { isPublicPagePath } from '$lib/publicRoutes';
import { currentUserFromWorkOSUser, ensureFolioTraceUser } from '$lib/server/auth';
import { getAuthKitConfig } from '$lib/server/workos';

const authKitConfig = getAuthKitConfig();
const authKitConfigured = authKitConfig !== null;
const devAuthConfigured = hasDevAuthConfigured();

if (authKitConfig)
  configureAuthKit(authKitConfig);

const workosHandle: Handle = authKitConfigured && !devAuthConfigured
  ? authKitHandle({
      onError: (error) => {
        console.error('WorkOS AuthKit request authentication failed.', getErrorDetails(error));
      }
    })
  : async ({ event, resolve }) => {
      event.locals.auth = null;
      event.locals.currentUser = null;
      return resolve(event);
    };

const authGateHandle: Handle = async ({ event, resolve }) => {
  event.locals.currentUser = null;

  const canonicalAuthRedirect = getCanonicalAuthRedirect(event);
  if (canonicalAuthRedirect)
    throw redirect(302, canonicalAuthRedirect);

  if (isPublicPagePath(event.url.pathname) || isPublicPath(event.url.pathname))
    return resolve(event);

  const devCurrentUser = getDevCurrentUser();
  if (devCurrentUser) {
    event.locals.currentUser = devCurrentUser;
    await ensureFolioTraceUser(event.fetch, devCurrentUser);
    return resolve(event);
  }

  if (!authKitConfigured)
    return new Response('WorkOS AuthKit is not configured.', { status: 503 });

  if (!event.locals.auth?.user) {
    if (hasCookie(event.request.headers.get('cookie'), 'wos-session')) {
      console.warn('WorkOS session cookie was present but no authenticated user was available.', {
        host: getRequestHost(event.request.headers) ?? event.url.host,
        pathname: event.url.pathname,
        hasSessionCookie: true
      });

      if (event.url.pathname.startsWith('/API/'))
        return new Response('Authentication session was not accepted.', { status: 401 });

      throw redirect(302, '/auth/error?code=SESSION_NOT_ACCEPTED');
    }

    if (event.url.pathname.startsWith('/API/'))
      return new Response('Authentication required.', { status: 401 });

    const signInUrl = new URL('/sign-in', event.url);
    signInUrl.searchParams.set('returnTo', `${event.url.pathname}${event.url.search}`);
    throw redirect(302, `${signInUrl.pathname}${signInUrl.search}`);
  }

  const currentUser = currentUserFromWorkOSUser(event.locals.auth.user);
  event.locals.currentUser = currentUser;
  await ensureFolioTraceUser(event.fetch, currentUser);

  return resolve(event);
};

export const handle = sequence(workosHandle, authGateHandle);

function isPublicPath(pathname: string) {
  return pathname === '/callback'
    || pathname === '/sign-in'
    || pathname === '/sign-out'
    || pathname === '/auth/error'
    || pathname === '/health'
    || pathname.startsWith('/_app/')
    || pathname.startsWith('/brand/')
    || pathname === '/favicon.ico'
    || pathname === '/robots.txt';
}

function getCanonicalAuthRedirect(event: RequestEvent) {
  const { url } = event;
  if (!authKitConfig || url.pathname.startsWith('/API/'))
    return null;

  if (url.pathname !== '/sign-in' && url.pathname !== '/callback' && (isPublicPagePath(url.pathname) || isPublicPath(url.pathname)))
    return null;

  const redirectUri = new URL(authKitConfig.redirectUri);
  const requestHost = getRequestHost(event.request.headers) ?? url.host;
  if (requestHost.toLowerCase() === redirectUri.host.toLowerCase())
    return null;

  const canonicalUrl = new URL(url);
  canonicalUrl.protocol = redirectUri.protocol;
  canonicalUrl.host = redirectUri.host;
  return canonicalUrl.toString();
}

function getRequestHost(headers: Headers) {
  const forwardedHost = headers.get('x-forwarded-host')?.split(',')[0]?.trim();
  if (forwardedHost)
    return forwardedHost;

  return headers.get('host')?.trim() || null;
}

function hasCookie(cookieHeader: string | null, name: string) {
  if (!cookieHeader)
    return false;

  return cookieHeader
    .split(';')
    .some((part) => part.trim().split('=')[0] === name);
}

function getErrorDetails(error: unknown) {
  if (!(error instanceof Error))
    return { message: String(error) };

  return {
    name: error.name,
    message: error.message,
    causeName: error.cause instanceof Error ? error.cause.name : undefined,
    causeMessage: error.cause instanceof Error ? error.cause.message : undefined
  };
}

function hasDevAuthConfigured() {
  return dev && Boolean(env.FOLIOTRACE_DEV_AUTH_EMAIL?.trim());
}

function getDevCurrentUser() {
  if (!hasDevAuthConfigured())
    return null;

  const email = env.FOLIOTRACE_DEV_AUTH_EMAIL?.trim() ?? '';
  if (!email)
    return null;

  return currentUserFromWorkOSUser({
    id: `foliotrace-dev:${email.toLowerCase()}`,
    email,
    firstName: env.FOLIOTRACE_DEV_AUTH_NAME?.trim() || 'FolioTrace Dev',
    lastName: ''
  });
}
