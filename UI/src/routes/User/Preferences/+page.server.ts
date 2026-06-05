import { clampFutureInputDateTime, nowForInput, toApiDateTime } from '$lib/dates';
import { defaultUserBookmarks } from '$lib/bookmarks';
import { defaultUserMenuPreferences, menuPreferenceDefinitions } from '$lib/menuPreferences';
import { defaultUserValuationPreferences, normalizeHoldingDateBasis, normalizeValuationDateOption } from '$lib/valuationPreferences';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getApiBaseUrl,
  getUserBookmarks,
  getUserMenuPreferences,
  getUserValuationPreferences,
  postUserBookmarkDeletedEvent,
  postUserMenuPreferencesCreatedEvent,
  postUserMenuPreferencesModifiedEvent,
  postUserBookmarkDisplayOrderSetEvent,
  postUserValuationPreferencesCreatedEvent,
  postUserValuationPreferencesModifiedEvent,
  type EventSubmissionResponse,
  type UserMenuPreferencesRequest,
  type UserValuationPreferencesRequest
} from '$lib/server/api';
import { fail } from '@sveltejs/kit';
import type { UserBookmarkItem } from '$lib/types';

export const load = async ({ fetch, locals, url }) => {
  const currentUser = requireCurrentUser(locals);
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const eventDateTime = nowForInput();
  const apiEventDateTime = toApiDateTime(eventDateTime);
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;

  try {
    const [menuPreferences, valuationPreferences, userBookmarks] = await Promise.all([
      getUserMenuPreferences(fetch, currentUser.userID, apiEventDateTime, apiAuditDateTime),
      getUserValuationPreferences(fetch, currentUser.userID, apiEventDateTime, apiAuditDateTime),
      getUserBookmarks(fetch, currentUser.userID, apiEventDateTime, apiAuditDateTime)
    ]);

    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      eventDateTime,
      currentUser,
      menuPreferences,
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
      userBookmarks: defaultUserBookmarks(currentUser.userID),
      valuationPreferences: defaultUserValuationPreferences(currentUser.userID)
    };
  }
};

export const actions = {
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
    const valuationDateOption = normalizeValuationDateOption(getFormString(formData, 'valuationDateOption'));
    const holdingDateBasis = normalizeHoldingDateBasis(getFormString(formData, 'holdingDateBasis'));
    const showZeroBalances = getFormString(formData, 'showZeroBalances') === 'true';
    const originalValuationDateOption = normalizeValuationDateOption(getFormString(formData, 'originalValuationDateOption'));
    const originalHoldingDateBasis = normalizeHoldingDateBasis(getFormString(formData, 'originalHoldingDateBasis'));
    const originalShowZeroBalances = getFormString(formData, 'originalShowZeroBalances') === 'true';
    const bookmarks = parseBookmarks(getFormString(formData, 'bookmarks'));
    const originalBookmarks = parseBookmarks(getFormString(formData, 'originalBookmarks'));
    const menuChanged = !areMenuItemsEqual(items, originalItems);
    const valuationChanged = valuationDateOption !== originalValuationDateOption
      || holdingDateBasis !== originalHoldingDateBasis
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
          holdingDateBasis,
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
  }
};

function getFormString(formData: FormData, name: string) {
  const value = formData.get(name);
  return typeof value === 'string' ? value.trim() : '';
}

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
