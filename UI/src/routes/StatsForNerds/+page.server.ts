import { fail } from '@sveltejs/kit';
import { ApiError, getApiBaseUrl, getMemoryDiagnostics, postSystemBuild, postSystemClearCacheAndProjections } from '$lib/server/api';
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

  clearCacheAndProjections: async ({ fetch }) => {
    try {
      const result = await postSystemClearCacheAndProjections(fetch);

      return {
        intent: 'clearCacheAndProjections',
        message: result.message || 'Caches and projections cleared.',
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'clearCacheAndProjections',
        message: error instanceof Error ? error.message : 'Unable to clear caches and projections.',
        status: 'failure'
      });
    }
  }
};
