import { getApiBaseUrl, getMemoryDiagnostics } from '$lib/server/api';

export const load = async ({ fetch }) => {
  try {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: '',
      memoryDiagnostics: await getMemoryDiagnostics(fetch)
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: error instanceof Error ? error.message : 'Unable to load dashboard analytics.',
      memoryDiagnostics: null
    };
  }
};
