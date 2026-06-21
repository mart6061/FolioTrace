import { authKit } from '@workos/authkit-sveltekit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ url }) => {
  const returnTo = getSafeReturnTo(url.searchParams.get('returnTo'));
  const signInUrl = await authKit.getSignInUrl({ returnTo });

  return new Response(null, {
    status: 302,
    headers: {
      Location: signInUrl,
      'Cache-Control': 'no-store'
    }
  });
};

function getSafeReturnTo(returnTo: string | null) {
  if (!returnTo || !returnTo.startsWith('/') || returnTo.startsWith('//'))
    return '/';

  return returnTo;
}
