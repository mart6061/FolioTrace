import { clampFutureInputDateTime, nowForInput, toApiDateTime } from '$lib/dates';
import { defaultUserMenuPreferences, menuPreferenceDefinitions, systemUserID } from '$lib/menuPreferences';
import { defaultUserValuationPreferences, normalizeValuationDateBasis, normalizeValuationDateOption } from '$lib/valuationPreferences';
import {
  getApiBaseUrl,
  getUserMenuPreferences,
  getUserValuationPreferences,
  postUserMenuPreferencesCreatedEvent,
  postUserMenuPreferencesModifiedEvent,
  postUserValuationPreferencesCreatedEvent,
  postUserValuationPreferencesModifiedEvent,
  type EventSubmissionResponse,
  type UserMenuPreferencesRequest,
  type UserValuationPreferencesRequest
} from '$lib/server/api';
import { fail } from '@sveltejs/kit';

export const load = async ({ fetch, url }) => {
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const eventDateTime = nowForInput();
  const apiEventDateTime = toApiDateTime(eventDateTime);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const [menuPreferences, valuationPreferences] = await Promise.all([
      getUserMenuPreferences(fetch, systemUserID, apiEventDateTime, apiAuditDateTime),
      getUserValuationPreferences(fetch, systemUserID, apiEventDateTime, apiAuditDateTime)
    ]);

    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      eventDateTime,
      menuPreferences,
      valuationPreferences
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load user preferences.',
      eventDateTime,
      menuPreferences: defaultUserMenuPreferences(),
      valuationPreferences: defaultUserValuationPreferences()
    };
  }
};

export const actions = {
  savePreferences: async ({ fetch, request }) => {
    const formData = await request.formData();
    const hasStoredMenuPreferences = getFormString(formData, 'hasStoredMenuPreferences') === 'true';
    const hasStoredValuationPreferences = getFormString(formData, 'hasStoredValuationPreferences') === 'true';
    const items = menuPreferenceDefinitions.map((item) => {
      const values = formData.getAll(`menu:${item.id}`).map(String);
      return {
        menuItemID: item.id,
        visible: values.length === 0 ? true : values[values.length - 1] === 'true'
      };
    });
    const originalItems = menuPreferenceDefinitions.map((item) => ({
      menuItemID: item.id,
      visible: getFormString(formData, `originalMenu:${item.id}`) !== 'false'
    }));
    const valuationDateOption = normalizeValuationDateOption(getFormString(formData, 'valuationDateOption'));
    const valuationDateBasis = normalizeValuationDateBasis(getFormString(formData, 'valuationDateBasis'));
    const showZeroBalances = getFormString(formData, 'showZeroBalances') === 'true';
    const originalValuationDateOption = normalizeValuationDateOption(getFormString(formData, 'originalValuationDateOption'));
    const originalValuationDateBasis = normalizeValuationDateBasis(getFormString(formData, 'originalValuationDateBasis'));
    const originalShowZeroBalances = getFormString(formData, 'originalShowZeroBalances') === 'true';
    const menuChanged = !areMenuItemsEqual(items, originalItems);
    const valuationChanged = valuationDateOption !== originalValuationDateOption
      || valuationDateBasis !== originalValuationDateBasis
      || showZeroBalances !== originalShowZeroBalances;

    try {
      const eventIDs: string[] = [];
      const eventDateTime = toApiDateTime(nowForInput());

      if (menuChanged) {
        const menuPreferencesRequest: UserMenuPreferencesRequest = {
          userID: systemUserID,
          eventDateTime,
          reason: 'Modify user menu preferences',
          items
        };
        const result = hasStoredMenuPreferences
          ? await postUserMenuPreferencesModifiedEvent(fetch, menuPreferencesRequest)
          : await postUserMenuPreferencesCreatedEvent(fetch, {
              ...menuPreferencesRequest,
              reason: 'Create user menu preferences'
            });
        addEventID(eventIDs, result);
      }

      if (valuationChanged) {
        const valuationPreferencesRequest: UserValuationPreferencesRequest = {
          userID: systemUserID,
          eventDateTime,
          reason: 'Modify user valuation preferences',
          valuationDateOption,
          valuationDateBasis,
          showZeroBalances
        };
        const result = hasStoredValuationPreferences
          ? await postUserValuationPreferencesModifiedEvent(fetch, valuationPreferencesRequest)
          : await postUserValuationPreferencesCreatedEvent(fetch, {
              ...valuationPreferencesRequest,
              reason: 'Create user valuation preferences'
            });
        addEventID(eventIDs, result);
      }

      return {
        eventIDs,
        intent: 'savePreferences',
        message: eventIDs.length === 0 ? 'No preference changes to save.' : 'Preferences saved.',
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'savePreferences',
        message: error instanceof Error ? error.message : 'Unable to save preferences.',
        status: 'failure'
      });
    }
  }
};

function getFormString(formData: FormData, name: string) {
  const value = formData.get(name);
  return typeof value === 'string' ? value.trim() : '';
}

function areMenuItemsEqual(left: { menuItemID: string; visible: boolean }[], right: { menuItemID: string; visible: boolean }[]) {
  return menuPreferenceDefinitions.every((item) =>
    left.find((candidate) => candidate.menuItemID === item.id)?.visible === right.find((candidate) => candidate.menuItemID === item.id)?.visible);
}

function addEventID(eventIDs: string[], response: EventSubmissionResponse) {
  if (response.eventID)
    eventIDs.push(response.eventID);
}
