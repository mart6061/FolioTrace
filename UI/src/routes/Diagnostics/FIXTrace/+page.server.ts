import { endOfDayForInput, startOfDayForInput } from '$lib/dates';
import { getApiBaseUrl, getFIXOperations } from '$lib/server/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
  const today = new Date();
  const filters = {
    fromUtc: url.searchParams.get('fromUtc') || startOfDayForInput(today),
    toUtc: url.searchParams.get('toUtc') || endOfDayForInput(today),
    direction: url.searchParams.get('direction') || '',
    channel: url.searchParams.get('channel') || '',
    msgType: url.searchParams.get('msgType') || '',
    clOrdID: url.searchParams.get('clOrdID') || '',
    execID: url.searchParams.get('execID') || '',
    text: url.searchParams.get('text') || '',
    page: url.searchParams.get('page') || '1',
    pageSize: url.searchParams.get('pageSize') || '50'
  };

  try {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: '',
      operations: await getFIXOperations(fetch, filters),
      filters
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: error instanceof Error ? error.message : 'Unable to load FIX trace.',
      operations: null,
      filters
    };
  }
};
