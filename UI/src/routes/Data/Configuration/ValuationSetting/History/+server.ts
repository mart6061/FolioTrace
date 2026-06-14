import { redirect } from '@sveltejs/kit';

export const GET = ({ url }) => {
  const target = new URL('/Data/Configuration/AssetAllocationTools/History', url);
  target.search = url.search;
  throw redirect(307, `${target.pathname}${target.search}`);
};

