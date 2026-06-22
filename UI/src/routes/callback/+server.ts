import { authKit } from '@workos/authkit-sveltekit';
import type { RequestHandler } from './$types';

const pkceCookiePrefix = 'wos-auth-verifier';

export const GET: RequestHandler = (event) => {
  const state = event.url.searchParams.get('state');
  const code = event.url.searchParams.get('code');
  const expectedCookieName = state ? getPKCECookieNameForState(state) : null;

  if (code && expectedCookieName && !hasCookie(event.request.headers.get('cookie'), expectedCookieName)) {
    console.warn('WorkOS callback arrived without the expected PKCE verifier cookie.', {
      host: getRequestHost(event.request.headers) ?? event.url.host,
      pathname: event.url.pathname,
      expectedCookieName,
      verifierCookieNames: getCookieNames(event.request.headers.get('cookie'))
        .filter((name) => name.startsWith(pkceCookiePrefix)),
      refererHost: getHeaderHost(event.request.headers.get('referer')),
      hasState: true,
      hasCode: true
    });

    return new Response(null, {
      status: 302,
      headers: {
        Location: '/auth/error?code=PKCE_COOKIE_MISSING',
        'Cache-Control': 'no-store'
      }
    });
  }

  return authKit.handleCallback()(event);
};

function hasCookie(cookieHeader: string | null, name: string) {
  return getCookieNames(cookieHeader).includes(name);
}

function getCookieNames(cookieHeader: string | null) {
  if (!cookieHeader)
    return [];

  return cookieHeader
    .split(';')
    .map((part) => part.trim().split('=')[0])
    .filter(Boolean);
}

function getRequestHost(headers: Headers) {
  const forwardedHost = headers.get('x-forwarded-host')?.split(',')[0]?.trim();
  if (forwardedHost)
    return forwardedHost;

  return headers.get('host')?.trim() || null;
}

function getHeaderHost(value: string | null) {
  if (!value)
    return null;

  try {
    return new URL(value).host;
  } catch {
    return null;
  }
}

function getPKCECookieNameForState(state: string) {
  return `${pkceCookiePrefix}-${fnv1a32Hex(state)}`;
}

function fnv1a32Hex(input: string) {
  let hash = 2166136261;
  const bytes = new TextEncoder().encode(input);
  for (const byte of bytes)
    hash = Math.imul(hash ^ byte, 16777619) >>> 0;

  return hash.toString(16).padStart(8, '0');
}
