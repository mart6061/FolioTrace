import { clampFutureInputDateTime, nowForInput, toApiDateTime } from '$lib/dates';
import { getFormString } from '$lib/server/forms';
import type { PageServerLoad, Actions } from './$types';
import { defaultUserBookmarks } from '$lib/bookmarks';
import { defaultUserMenuPreferences, menuPreferenceDefinitions } from '$lib/menuPreferences';
import { defaultDateControlConfiguration } from '$lib/dateRules';
import { defaultEndValuationDateOption, defaultStartValuationDateOption, defaultUserValuationPreferences, normalizeHoldingDateBasis, normalizeValuationDateOption, normalizeValuationPriceConvention } from '$lib/valuationPreferences';
import { requireCurrentUser } from '$lib/server/currentUser';
import {
  getApiBaseUrl,
  getUserBookmarks,
  getUserMenuPreferences,
  getUserValuationPreferences,
  getDateControlSettings,
  getUserDateControlSettings,
  postUserBookmarkDeletedEvent,
  postUserMenuPreferencesCreatedEvent,
  postUserMenuPreferencesModifiedEvent,
  postUserBookmarkDisplayOrderSetEvent,
  postUserValuationPreferencesCreatedEvent,
  postUserValuationPreferencesModifiedEvent,
  postUserDateControlSettingsCreatedEvent,
  postUserDateControlSettingsModifiedEvent,
  postUserDateControlSettingsClearedEvent,
  type EventSubmissionResponse,
  type UserMenuPreferencesRequest,
  type UserValuationPreferencesRequest
} from '$lib/server/api';
import { fail } from '@sveltejs/kit';
import type { DateControlConfiguration, UserBookmarkItem, UserDateControlSettings } from '$lib/types';

export const load: PageServerLoad = async ({ fetch, locals, url }) => {
  const currentUser = requireCurrentUser(locals);
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const eventDateTime = nowForInput();
  const apiEventDateTime = toApiDateTime(eventDateTime);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const [menuPreferences, valuationPreferences, userBookmarks, dateControlSettings, userDateControlSettings] = await Promise.all([
      getUserMenuPreferences(fetch, currentUser.userID, apiEventDateTime, apiAuditDateTime),
      getUserValuationPreferences(fetch, currentUser.userID, apiEventDateTime, apiAuditDateTime),
      getUserBookmarks(fetch, currentUser.userID, apiEventDateTime, apiAuditDateTime),
      getDateControlSettings(fetch, apiEventDateTime, apiAuditDateTime),
      getUserDateControlSettings(fetch, currentUser.userID, apiEventDateTime, apiAuditDateTime)
    ]);

    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      eventDateTime,
      currentUser,
      menuPreferences,
      dateControlSettings,
      userDateControlSettings,
      userBookmarks,
      valuationPreferences
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load user preferences.',
      eventDateTime,
      currentUser,
      menuPreferences: defaultUserMenuPreferences(currentUser.userID),
      dateControlSettings: { configuration: defaultDateControlConfiguration },
      userDateControlSettings: { configuration: { ...defaultDateControlConfiguration, dateOptions: [], rangeOptions: [] }, hasStoredConfiguration: false } as Pick<UserDateControlSettings, 'configuration' | 'hasStoredConfiguration'>,
      userBookmarks: defaultUserBookmarks(currentUser.userID),
      valuationPreferences: defaultUserValuationPreferences(currentUser.userID)
    };
  }
};

export const actions: Actions = {
  savePreferences: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
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
    const startValuationDateOption = normalizeValuationDateOption(getFormString(formData, 'startValuationDateOption'), defaultStartValuationDateOption);
    const endValuationDateOption = normalizeValuationDateOption(getFormString(formData, 'endValuationDateOption'), defaultEndValuationDateOption);
    const valuationDateOption = endValuationDateOption;
    const holdingDateBasis = normalizeHoldingDateBasis(getFormString(formData, 'holdingDateBasis'));
    const valuationPriceConvention = normalizeValuationPriceConvention(getFormString(formData, 'valuationPriceConvention'));
    const showZeroBalances = getFormString(formData, 'showZeroBalances') === 'true';
    const originalStartValuationDateOption = normalizeValuationDateOption(getFormString(formData, 'originalStartValuationDateOption'), defaultStartValuationDateOption);
    const originalEndValuationDateOption = normalizeValuationDateOption(getFormString(formData, 'originalEndValuationDateOption'), defaultEndValuationDateOption);
    const originalHoldingDateBasis = normalizeHoldingDateBasis(getFormString(formData, 'originalHoldingDateBasis'));
    const originalValuationPriceConvention = normalizeValuationPriceConvention(getFormString(formData, 'originalValuationPriceConvention'));
    const originalShowZeroBalances = getFormString(formData, 'originalShowZeroBalances') === 'true';
    const bookmarks = parseBookmarks(getFormString(formData, 'bookmarks'));
    const originalBookmarks = parseBookmarks(getFormString(formData, 'originalBookmarks'));
    const menuChanged = !areMenuItemsEqual(items, originalItems);
    const valuationChanged = startValuationDateOption !== originalStartValuationDateOption
      || endValuationDateOption !== originalEndValuationDateOption
      || holdingDateBasis !== originalHoldingDateBasis
      || valuationPriceConvention !== originalValuationPriceConvention
      || showZeroBalances !== originalShowZeroBalances;
    const bookmarkChanges = getBookmarkChanges(bookmarks, originalBookmarks);

    try {
      const eventIDs: string[] = [];
      const eventDateTime = toApiDateTime(nowForInput());

      if (menuChanged) {
        const menuPreferencesRequest: UserMenuPreferencesRequest = {
          userID: currentUser.userID,
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
          userID: currentUser.userID,
          eventDateTime,
          reason: 'Modify user valuation preferences',
          valuationDateOption,
          startValuationDateOption,
          endValuationDateOption,
          holdingDateBasis,
          valuationPriceConvention,
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

      for (const bookmark of bookmarkChanges.deleted) {
        const result = await postUserBookmarkDeletedEvent(fetch, {
          userID: currentUser.userID,
          eventDateTime,
          reason: 'Delete user bookmark',
          bookmarkID: bookmark.bookmarkID
        });
        addEventID(eventIDs, result);
      }

      for (const bookmark of bookmarkChanges.reordered) {
        const result = await postUserBookmarkDisplayOrderSetEvent(fetch, {
          userID: currentUser.userID,
          eventDateTime,
          reason: 'Set bookmark display order',
          bookmarkID: bookmark.bookmarkID,
          displayOrder: bookmark.displayOrder
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
  },
  saveDateControls: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const clear = getFormString(formData, 'clear') === 'true';
    const stored = getFormString(formData, 'hasStoredConfiguration') === 'true';
    try {
      const result = clear
        ? await postUserDateControlSettingsClearedEvent(fetch, currentUser.userID)
        : stored
          ? await postUserDateControlSettingsModifiedEvent(fetch, currentUser.userID, JSON.parse(getFormString(formData, 'configuration')) as DateControlConfiguration)
          : await postUserDateControlSettingsCreatedEvent(fetch, currentUser.userID, JSON.parse(getFormString(formData, 'configuration')) as DateControlConfiguration);
      return { eventID: result.eventID, intent: 'saveDateControls', message: clear ? 'Global date controls restored.' : 'Your date controls were saved.', status: 'success' };
    } catch (error) {
      return fail(502, { intent: 'saveDateControls', message: error instanceof Error ? error.message : 'Unable to save date controls.', status: 'failure' });
    }
  }
};

function parseBookmarks(value: string) {
  if (!value)
    return [];

  try {
    const parsed = JSON.parse(value) as UserBookmarkItem[];
    return parsed
      .filter((item) => item && item.bookmarkID && item.url && Number.isFinite(item.displayOrder))
      .map((item) => ({
        bookmarkID: item.bookmarkID,
        bookmarkType: item.bookmarkType,
        url: item.url,
        displayOrder: item.displayOrder
      }));
  } catch {
    return [];
  }
}

function areMenuItemsEqual(left: { menuItemID: string; visible: boolean }[], right: { menuItemID: string; visible: boolean }[]) {
  return menuPreferenceDefinitions.every((item) =>
    left.find((candidate) => candidate.menuItemID === item.id)?.visible === right.find((candidate) => candidate.menuItemID === item.id)?.visible);
}

function addEventID(eventIDs: string[], response: EventSubmissionResponse) {
  if (response.eventID)
    eventIDs.push(response.eventID);
}

function getBookmarkChanges(bookmarks: UserBookmarkItem[], originalBookmarks: UserBookmarkItem[]) {
  const currentByID = new Map(bookmarks.map((bookmark) => [bookmark.bookmarkID, bookmark]));
  const originalByID = new Map(originalBookmarks.map((bookmark) => [bookmark.bookmarkID, bookmark]));

  return {
    deleted: originalBookmarks.filter((bookmark) => !currentByID.has(bookmark.bookmarkID)),
    reordered: bookmarks.filter((bookmark) => originalByID.has(bookmark.bookmarkID) && originalByID.get(bookmark.bookmarkID)?.displayOrder !== bookmark.displayOrder)
  };
}
