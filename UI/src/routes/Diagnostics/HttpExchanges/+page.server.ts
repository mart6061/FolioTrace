import { getApiBaseUrl, getApiExchanges } from '$lib/server/api';

export const load = async ({ fetch, url }) => {
  const filters = {
    fromUtc: url.searchParams.get('fromUtc') || '',
    toUtc: url.searchParams.get('toUtc') || '',
    method: url.searchParams.get('method') || '',
    path: url.searchParams.get('path') || '',
    statusCode: url.searchParams.get('statusCode') || '',
    minimumDurationMilliseconds: url.searchParams.get('minimumDurationMilliseconds') || '',
    maximumDurationMilliseconds: url.searchParams.get('maximumDurationMilliseconds') || '',
    text: url.searchParams.get('text') || '',
    page: url.searchParams.get('page') || '1',
    pageSize: url.searchParams.get('pageSize') || '50'
  };

  try {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: '',
      exchanges: await getApiExchanges(fetch, filters),
      filters
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: error instanceof Error ? error.message : 'Unable to load API exchanges.',
      exchanges: null,
      filters
    };
  }
};
