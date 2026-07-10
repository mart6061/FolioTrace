import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = ({ url }) => {
  const target = new URL('/Data/Configuration/AssetAllocationTools', url);
  target.search = url.search;
  throw redirect(307, `${target.pathname}${target.search}`);
};
