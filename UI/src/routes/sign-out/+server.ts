import { redirect } from '@sveltejs/kit';
import { getApiBaseUrl } from '$lib/server/api';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = ({ url }) => {
  const signOutUrl = new URL(`${getApiBaseUrl()}/Auth/SignOut`);
  signOutUrl.searchParams.set('returnTo', url.searchParams.get('returnTo') ?? '/');
  throw redirect(302, signOutUrl.toString());
};
