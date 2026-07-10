import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = ({ url }) => {
  const target = new URL('/Viewer', url);
  target.search = url.search;
  target.searchParams.set('viewer', 'Asset');
  throw redirect(307, `${target.pathname}${target.search}`);
};
