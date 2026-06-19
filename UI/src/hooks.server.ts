import { authKit, authKitHandle, configureAuthKit } from '@workos/authkit-sveltekit';
import { dev } from '$app/environment';
import { env } from '$env/dynamic/private';
import { redirect, type Handle } from '@sveltejs/kit';
import { sequence } from '@sveltejs/kit/hooks';
import { currentUserFromWorkOSUser, ensureFolioTraceUser } from '$lib/server/auth';
import { getAuthKitConfig } from '$lib/server/workos';

const authKitConfig = getAuthKitConfig();
const authKitConfigured = authKitConfig !== null;

if (authKitConfig)
  configureAuthKit(authKitConfig);

const workosHandle: Handle = authKitConfigured
  ? authKitHandle()
  : async ({ event, resolve }) => {
      event.locals.auth = null;
      event.locals.currentUser = null;
      return resolve(event);
    };

const authGateHandle: Handle = async ({ event, resolve }) => {
  event.locals.currentUser = null;

  if (isPublicPath(event.url.pathname))
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
    if (event.url.pathname.startsWith('/API/'))
      return new Response('Authentication required.', { status: 401 });

    const returnTo = `${event.url.pathname}${event.url.search}`;
    throw redirect(302, await authKit.getSignInUrl({ returnTo }));
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
    || pathname.startsWith('/_app/')
    || pathname.startsWith('/brand/')
    || pathname === '/favicon.ico'
    || pathname === '/robots.txt';
}

function getDevCurrentUser() {
  if (!dev)
    return null;

  const email = env.FOLIOTRACE_DEV_AUTH_EMAIL?.trim();
  if (!email)
    return null;

  return currentUserFromWorkOSUser({
    id: `foliotrace-dev:${email.toLowerCase()}`,
    email,
    firstName: env.FOLIOTRACE_DEV_AUTH_NAME?.trim() || 'FolioTrace Dev',
    lastName: ''
  });
}
