import { authKit } from '@workos/authkit-sveltekit';
import type { RequestHandler } from './$types';

const pkceCookiePrefix = 'wos-auth-verifier';

export const GET: RequestHandler = async (event) => {
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

  const response = await authKit.handleCallback()(event);
  const location = response.headers.get('Location');

  if (response.status !== 302 || !location || location.startsWith('/auth/error'))
    return response;

  const redirectLocation = getSafeRedirectLocation(location);
  const handoff = new Response(renderSessionHandoffPage(redirectLocation), {
    status: 200,
    headers: {
      'Cache-Control': 'no-store',
      'Content-Type': 'text/html; charset=utf-8',
      Refresh: `0; url=${redirectLocation}`
    }
  });

  for (const cookie of getSetCookies(response))
    handoff.headers.append('Set-Cookie', cookie);

  return handoff;
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

function getSafeRedirectLocation(location: string) {
  if (!location.startsWith('/') || location.startsWith('//'))
    return '/';

  return location;
}

function getSetCookies(response: Response) {
  const headers = response.headers as Headers & { getSetCookie?: () => string[] };
  const cookies = headers.getSetCookie?.();

  if (cookies?.length)
    return cookies;

  const cookie = response.headers.get('Set-Cookie');
  return cookie ? [cookie] : [];
}

function renderSessionHandoffPage(location: string) {
  const escapedLocation = escapeHtml(location);
  const jsLocation = JSON.stringify(location);

  return `<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta http-equiv="refresh" content="0; url=${escapedLocation}">
    <title>Opening FolioTrace</title>
    <script>window.location.replace(${jsLocation})</script>
  </head>
  <body>
    <a href="${escapedLocation}">Continue to FolioTrace</a>
  </body>
</html>`;
}

function escapeHtml(value: string) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('"', '&quot;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
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
