import type { UserMenuPreferenceItem, UserMenuPreferences } from '$lib/types';

export type MenuPreferenceDefinition = {
  id: string;
  label: string;
  parentID?: string;
};

export const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const menuPreferenceDefinitions: MenuPreferenceDefinition[] = [
  { id: 'bookmarks', label: 'Bookmarks' },
  { id: 'blotter', label: 'Blotter' },
  { id: 'account', label: 'Account' },
  { id: 'compliance', label: 'Compliance' },
  { id: 'administration', label: 'Administration' },
  { id: 'data', label: 'Data' },
  { id: 'value', label: 'Value', parentID: 'data' },
  { id: 'reference', label: 'Reference', parentID: 'data' },
  { id: 'system', label: 'System' },
  { id: 'system-logs', label: 'Logs', parentID: 'system' },
  { id: 'system-stats', label: 'Stats for Nerds', parentID: 'system' },
  { id: 'todo', label: 'To Do' }
];

export const controlledMenuItemIDs = menuPreferenceDefinitions.map((item) => item.id);

export function defaultMenuPreferenceItems(): UserMenuPreferenceItem[] {
  return controlledMenuItemIDs.map((menuItemID) => ({ menuItemID, visible: true }));
}

export function defaultUserMenuPreferences(): UserMenuPreferences {
  return {
    userID: systemUserID,
    items: defaultMenuPreferenceItems(),
    hasStoredPreferences: false,
    valuationDateTime: '',
    asOfDateTime: '',
    lastEventID: '',
    lastAuditDateTime: ''
  };
}

export function normalizeMenuPreferenceItems(items: UserMenuPreferenceItem[] | null | undefined) {
  const byID = new Map((items ?? []).map((item) => [item.menuItemID, item.visible]));
  return defaultMenuPreferenceItems().map((item) => ({
    ...item,
    visible: byID.get(item.menuItemID) ?? item.visible
  }));
}
