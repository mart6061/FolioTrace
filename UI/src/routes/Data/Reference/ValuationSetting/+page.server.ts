import { redirect } from '@sveltejs/kit';

export const load = ({ url }) => {
  const target = new URL('/Data/Configuration/ValuationSetting', url);
  target.search = url.search;
  throw redirect(307, `${target.pathname}${target.search}`);
};

