import type { PageServerLoad } from './$types';

const sessionCookieName = 'wos-session';

export const load: PageServerLoad = ({ cookies, request, url }) => {
  if (url.searchParams.get('code') !== 'SESSION_NOT_ACCEPTED')
    return;

  for (const cookieName of getStaleAuthCookieNames(request.headers.get('cookie')))
    cookies.delete(cookieName, { path: '/' });
};

function getStaleAuthCookieNames(cookieHeader: string | null) {
  return getCookieNames(cookieHeader).filter((name) => name === sessionCookieName || name.startsWith('wos-auth-verifier-'));
}

function getCookieNames(cookieHeader: string | null) {
  if (!cookieHeader)
    return [];

  return cookieHeader
    .split(';')
    .map((part) => part.trim().split('=')[0])
    .filter(Boolean);
}
