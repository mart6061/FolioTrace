import { authKit } from '@workos/authkit-sveltekit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ setHeaders, url }) => {
  setHeaders({
    'Cache-Control': 'no-store'
  });

  const returnTo = getSafeReturnTo(url.searchParams.get('returnTo'));
  const signInUrl = await authKit.getSignInUrl({ returnTo });

  return {
    signInUrl
  };
};

function getSafeReturnTo(returnTo: string | null) {
  if (!returnTo || !returnTo.startsWith('/') || returnTo.startsWith('//'))
    return '/';

  return returnTo;
}
