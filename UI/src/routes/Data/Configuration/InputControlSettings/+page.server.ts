import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { getFormString } from '$lib/server/forms';
import { fail } from '@sveltejs/kit';
import { requireCurrentUser } from '$lib/server/currentUser';
import {
  getAccounts,
  getApiBaseUrl,
  getInputControlSettings,
  getDateControlSettings,
  getInputPolicies,
  postInputControlSettingsModifiedEvent,
  postDateControlSettingsModifiedEvent
} from '$lib/server/api';
import type { DateControlConfiguration, InputControlKind, InputControlSetting } from '$lib/types';
import type { PageServerLoad, Actions } from './$types';

const controlKinds: InputControlKind[] = ['Quantity', 'Money', 'Price', 'Percent'];

export const load: PageServerLoad = async ({ fetch, parent, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const eventDateTime = toApiDateTime(valuationDate);
  const asOfDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  const { currentUser } = await parent();
  const previewCurrency = url.searchParams.get('previewCurrency') || 'GBP';
  const previewAccountID = url.searchParams.get('previewAccountID') || '';

  try {
    const [accounts, settings, policies, dateControlSettings] = await Promise.all([
      getAccounts(fetch, eventDateTime, asOfDateTime),
      getInputControlSettings(fetch, eventDateTime, asOfDateTime),
      // Resolved alongside the stored rules so the page can show what actually wins, which is the part
      // that is hard to work out by reading the rules alone.
      getInputPolicies(fetch, {
        accountID: previewAccountID || null,
        auditDateTime: asOfDateTime,
        controlKinds,
        currency: previewCurrency,
        eventDateTime,
        userID: currentUser?.userID
      }),
      getDateControlSettings(fetch, eventDateTime, asOfDateTime)
    ]);

    return {
      accounts,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      policies,
      previewAccountID,
      previewCurrency,
      settings,
      dateControlSettings,
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load input control settings.',
      policies: [],
      previewAccountID,
      previewCurrency,
      settings: null,
      dateControlSettings: null,
      valuationDate
    };
  }
};

export const actions: Actions = {
  saveSettings: async ({ fetch, locals, request }) => {
    const userID = requireCurrentUser(locals).userID;
    const formData = await request.formData();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const settingsJson = getFormString(formData, 'settingsJson');

    if (!eventDateTime)
      return fail(400, { message: 'Event date is required.', status: 'failure' });

    let settings: InputControlSetting[];

    try {
      settings = JSON.parse(settingsJson) as InputControlSetting[];
    } catch {
      return fail(400, { message: 'Settings are not valid JSON.', status: 'failure' });
    }

    if (!Array.isArray(settings) || settings.length === 0)
      return fail(400, { message: 'At least one setting is required.', status: 'failure' });

    try {
      const result = await postInputControlSettingsModifiedEvent(fetch, settings, toApiDateTime(eventDateTime), userID);

      return { eventID: result.eventID, message: 'Input control settings were saved.', status: 'success' };
    } catch (error) {
      return fail(502, {
        message: error instanceof Error ? error.message : 'Unable to save input control settings.',
        status: 'failure'
      });
    }
  },
  saveDateControls: async ({ fetch, locals, request }) => {
    const userID = requireCurrentUser(locals).userID;
    const formData = await request.formData();
    let configuration: DateControlConfiguration;
    try { configuration = JSON.parse(getFormString(formData, 'configuration')) as DateControlConfiguration; }
    catch { return fail(400, { message: 'Date control configuration is not valid JSON.', status: 'failure' }); }
    try {
      const result = await postDateControlSettingsModifiedEvent(fetch, userID, configuration);
      return { eventID: result.eventID, message: 'Date and range control settings were saved.', status: 'success' };
    } catch (error) {
      return fail(502, { message: error instanceof Error ? error.message : 'Unable to save date control settings.', status: 'failure' });
    }
  }
};
