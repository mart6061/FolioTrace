import { fail } from '@sveltejs/kit';
import { ApiError, getApiBaseUrl, getMemoryDiagnostics, postSystemBuild, postSystemClearCache } from '$lib/server/api';
import type { PageServerLoad, Actions } from './$types';

export const load: PageServerLoad = async ({ fetch }) => {
  try {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: '',
      memoryDiagnostics: await getMemoryDiagnostics(fetch)
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: error instanceof Error ? error.message : 'Unable to load stats.',
      memoryDiagnostics: null
    };
  }
};

export const actions: Actions = {
  build: async ({ fetch }) => {
    try {
      const result = await postSystemBuild(fetch);

      return {
        intent: 'build',
        message: result.message || 'Database rebuild started.',
        status: 'success'
      };
    } catch (error) {
      return fail(error instanceof ApiError ? error.status : 502, {
        intent: 'build',
        message: error instanceof Error ? error.message : 'Unable to rebuild the database.',
        status: 'failure'
      });
    }
  },

  clearCache: async ({ fetch }) => {
    try {
      const result = await postSystemClearCache(fetch);

      return {
        intent: 'clearCache',
        message: result.message || 'Caches cleared.',
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'clearCache',
        message: error instanceof Error ? error.message : 'Unable to clear caches.',
        status: 'failure'
      });
    }
  }
};
