import { formatBookmarkType, normalizeBookmarkPath, normalizeBookmarkType } from '$lib/bookmarks';
import { nowForInput, toApiDateTime } from '$lib/dates';
import { systemUserID } from '$lib/menuPreferences';
import {
  getUserBookmarks,
  postUserBookmarkCreatedEvent,
  postUserBookmarkDisplayOrderSetEvent,
  postUserBookmarkModifiedEvent
} from '$lib/server/api';
import { json, type RequestHandler } from '@sveltejs/kit';

export const POST: RequestHandler = async ({ fetch, request }) => {
  const body = await request.json() as { path?: string; bookmarkType?: string };
  const url = normalizeBookmarkPath(body.path);
  const bookmarkType = normalizeBookmarkType(body.bookmarkType);
  const eventDateTime = toApiDateTime(nowForInput());
  const bookmarks = await getUserBookmarks(fetch, systemUserID, eventDateTime, null);
  const existing = bookmarks.items.find((item) => item.bookmarkType === bookmarkType && item.url === url);
  const displayOrder = existing?.displayOrder ?? Math.max(0, ...bookmarks.items.map((item) => item.displayOrder)) + 1;
  const bookmarkID = existing?.bookmarkID ?? crypto.randomUUID();
  const bookmarkTypeLabel = formatBookmarkType(bookmarkType);
  const requestBody = {
    userID: systemUserID,
    eventDateTime,
    reason: existing ? `Modify ${bookmarkTypeLabel} bookmark` : `Create ${bookmarkTypeLabel} bookmark`,
    bookmarkID,
    bookmarkType,
    url,
    displayOrder
  };
  const bookmarkEvent = existing
    ? await postUserBookmarkModifiedEvent(fetch, requestBody)
    : await postUserBookmarkCreatedEvent(fetch, requestBody);
  const displayOrderEvent = await postUserBookmarkDisplayOrderSetEvent(fetch, {
    userID: systemUserID,
    eventDateTime,
    reason: `Set ${bookmarkTypeLabel} bookmark display order`,
    bookmarkID,
    displayOrder
  });

  return json({
    bookmarkID,
    eventIDs: [bookmarkEvent.eventID, displayOrderEvent.eventID].filter(Boolean)
  });
};
