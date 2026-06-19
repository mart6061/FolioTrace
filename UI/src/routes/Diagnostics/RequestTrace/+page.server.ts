import { endOfDayForInput, startOfDayForInput } from '$lib/dates';
import { getApiBaseUrl, getApiExchanges } from '$lib/server/api';

export const load = async ({ fetch, url }) => {
  const today = new Date();
  const filters = {
    fromUtc: url.searchParams.get('fromUtc') || startOfDayForInput(today),
    toUtc: url.searchParams.get('toUtc') || endOfDayForInput(today),
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
      error: error instanceof Error ? error.message : 'Unable to load request trace.',
      exchanges: null,
      filters
    };
  }
};
