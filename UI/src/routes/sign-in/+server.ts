import { authKit } from '@workos/authkit-sveltekit';
import { redirect } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ url }) => {
  const returnTo = url.searchParams.get('returnTo') || '/';
  throw redirect(302, await authKit.getSignInUrl({ returnTo }));
};
