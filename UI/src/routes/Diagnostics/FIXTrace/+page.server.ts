import { getApiBaseUrl, getFIXOperations } from '$lib/server/api';

export const load = async ({ fetch, url }) => {
  const filters = {
    fromUtc: url.searchParams.get('fromUtc') || '',
    toUtc: url.searchParams.get('toUtc') || '',
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
