import { systemUserID } from '$lib/menuPreferences';
import type { UserBookmarks, UserBookmarkType } from '$lib/types';

export const bookmarkTypeOptions: { value: UserBookmarkType; label: string }[] = [
  { value: 'Base', label: 'Page' },
  { value: 'Query', label: 'Filter' }
];

export function defaultUserBookmarks(): UserBookmarks {
  return {
    userID: systemUserID,
    items: [],
    valuationDateTime: '',
    asOfDateTime: '',
    lastEventID: '',
    lastAuditDateTime: ''
  };
}

export function normalizeBookmarkType(value: string | null | undefined): UserBookmarkType {
  return value === 'Query' ? 'Query' : 'Base';
}

export function formatBookmarkType(value: UserBookmarkType): string {
  return value === 'Base' ? 'Page' : 'Filter';
}

export function formatBookmarkUrl(value: string): string {
  const path = value.split('?')[0]?.replace(/^\/+/, '') ?? '';

  if (!path)
    return 'Home';

  return path
    .split('/')
    .filter(Boolean)
    .map((part) => decodeURIComponent(part))
    .join(' > ');
}

export function formatBookmarkMenuUrl(value: string): string {
  const path = value.split('?')[0]?.replace(/\/+$/, '') ?? '';
  const lastPart = path.split('/').filter(Boolean).at(-1);

  return lastPart ? decodeURIComponent(lastPart) : 'Home';
}
