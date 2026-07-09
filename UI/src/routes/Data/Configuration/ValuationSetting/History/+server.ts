import { redirect } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = ({ url }) => {
  const target = new URL('/Data/Configuration/AssetAllocationTools/History', url);
  target.search = url.search;
  throw redirect(307, `${target.pathname}${target.search}`);
};

