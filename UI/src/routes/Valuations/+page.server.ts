import { redirect } from '@sveltejs/kit';

export const load = ({ url }) => {
  const target = new URL('/Viewer', url);
  target.search = url.search;
  target.searchParams.set('viewer', 'Asset');
  throw redirect(307, `${target.pathname}${target.search}`);
};
