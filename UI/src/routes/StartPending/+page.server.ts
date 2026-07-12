import { env } from '$env/dynamic/private';
import { redirect } from '@sveltejs/kit';
import { getApiBaseUrl } from '$lib/server/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
  const returnTo = normalizeReturnTo(url.searchParams.get('returnTo'));
  const retrySeconds = positiveInteger(env.API_READY_RETRY_SECONDS, 3);

  try {
    const response = await fetch(`${getApiBaseUrl()}/System/Health`);
    if (response.ok) {
      const status = (await response.json()) as { ready?: boolean };
      if (status.ready)
        throw redirect(302, returnTo);
    }
  } catch (error) {
    if (error && typeof error === 'object' && 'status' in error)
      throw error;
  }

  return { returnTo, retrySeconds };
};

function normalizeReturnTo(value: string | null) {
  return value?.startsWith('/') && !value.startsWith('//') && !value.startsWith('/StartPending') ? value : '/';
}

function positiveInteger(value: string | undefined, fallback: number) {
  const parsed = Number.parseInt(value ?? '', 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}
