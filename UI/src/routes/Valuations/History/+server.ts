import { redirect } from '@sveltejs/kit';

export const GET = ({ url }) => {
  const target = new URL('/Asset/History', url);
  target.search = url.search;
  throw redirect(307, `${target.pathname}${target.search}`);
};
