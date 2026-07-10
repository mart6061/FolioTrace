import { fail } from '@sveltejs/kit';
import { endOfDayForInput, startOfDayForInput } from '$lib/dates';
import { getApiBaseUrl, getRequestTraces, purgeRequestTraces, putRequestTraceSettings } from '$lib/server/api';
import type { RequestTraceSettings } from '$lib/types';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
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
      traces: await getRequestTraces(fetch, filters),
      filters
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      error: error instanceof Error ? error.message : 'Unable to load request trace.',
      traces: null,
      filters
    };
  }
};

export const actions: Actions = {
  saveSettings: async ({ fetch, request }) => {
    const form = await request.formData();

    try {
      await putRequestTraceSettings(fetch, {
        enabled: form.get('enabled') === 'on',
        captureApi: form.get('captureApi') === 'on',
        captureUi: form.get('captureUi') === 'on',
        captureBodies: form.get('captureBodies') === 'on',
        capture500StackTraces: form.get('capture500StackTraces') === 'on',
        captureLogMessages: form.get('captureLogMessages') === 'on',
        minimumLogLevel: String(form.get('minimumLogLevel') || 'Warning'),
        maximumBodyCharacters: Number(form.get('maximumBodyCharacters') || 32000),
        capturedContentTypePrefixes: splitLines(form.get('capturedContentTypePrefixes')),
        excludedPathPrefixes: splitLines(form.get('excludedPathPrefixes')),
        redactedHeaders: splitLines(form.get('redactedHeaders'))
      } satisfies RequestTraceSettings);

      return {
        intent: 'saveSettings',
        message: 'Request trace settings saved.',
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'saveSettings',
        message: error instanceof Error ? error.message : 'Unable to save request trace settings.',
        status: 'failure'
      });
    }
  },

  purgeTrace: async ({ fetch, request }) => {
    const form = await request.formData();
    const confirmation = String(form.get('confirmation') || '');
    const beforeUtc = String(form.get('beforeUtc') || '');

    try {
      const result = await purgeRequestTraces(fetch, confirmation, beforeUtc || null);

      return {
        intent: 'purgeTrace',
        message: `Purged ${result.deletedCount} trace event${result.deletedCount === 1 ? '' : 's'}.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'purgeTrace',
        message: error instanceof Error ? error.message : 'Unable to purge request trace events.',
        status: 'failure'
      });
    }
  }
};

function splitLines(value: FormDataEntryValue | null) {
  return String(value || '')
    .split(/\r?\n|,/)
    .map((item) => item.trim())
    .filter(Boolean);
}
