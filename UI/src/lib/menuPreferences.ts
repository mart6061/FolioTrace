import type { UserMenuPreferenceItem, UserMenuPreferences } from '$lib/types';

const legacyMenuPreferenceIDs = new Map([
  ['value-valuations', 'asset'],
  ['reference-valuation-setting', 'configuration-asset-allocation-tools'],
  ['configuration-valuation-setting', 'configuration-asset-allocation-tools']
]);

export type MenuPreferenceDefinition = {
  id: string;
  label: string;
  parentID?: string;
};

export const menuPreferenceDefinitions: MenuPreferenceDefinition[] = [
  { id: 'bookmarks', label: 'Bookmarks' },
  { id: 'blotter', label: 'Blotter' },
  { id: 'viewer', label: 'Viewer' },
  { id: 'asset', label: 'Asset' },
  { id: 'report', label: 'Report' },
  { id: 'account', label: 'Account' },
  { id: 'system', label: 'System' },
  { id: 'data', label: 'Data', parentID: 'system' },
  { id: 'value', label: 'Value', parentID: 'data' },
  { id: 'reference', label: 'Reference', parentID: 'data' },
  { id: 'configuration', label: 'Configuration', parentID: 'data' },
  { id: 'configuration-asset-allocation', label: 'Asset Allocation', parentID: 'configuration' },
  { id: 'tools', label: 'Tools', parentID: 'system' },
  { id: 'configuration-account-tools', label: 'Account Tools', parentID: 'tools' },
  { id: 'configuration-asset-allocation-tools', label: 'Asset Allocation Tools', parentID: 'tools' },
  { id: 'configuration-report-tools', label: 'Report Tools', parentID: 'tools' },
  { id: 'internals', label: 'Internals', parentID: 'system' },
  { id: 'system-logs', label: 'Request Trace', parentID: 'internals' },
  { id: 'system-fix-trace', label: 'FIX Trace', parentID: 'internals' },
  { id: 'system-stats', label: 'Stats for Nerds', parentID: 'internals' }
];

export const controlledMenuItemIDs = menuPreferenceDefinitions.map((item) => item.id);

export function defaultMenuPreferenceItems(): UserMenuPreferenceItem[] {
  return controlledMenuItemIDs.map((menuItemID) => ({ menuItemID, visible: true }));
}

export function defaultUserMenuPreferences(userID = ''): UserMenuPreferences {
  return {
    userID,
    items: defaultMenuPreferenceItems(),
    hasStoredPreferences: false,
    valuationDateTime: '',
    asOfDateTime: '',
    lastEventID: '',
    lastAuditDateTime: ''
  };
}

export function normalizeMenuPreferenceItems(items: UserMenuPreferenceItem[] | null | undefined) {
  const byID = new Map((items ?? []).map((item) => [legacyMenuPreferenceIDs.get(item.menuItemID) ?? item.menuItemID, item.visible]));
  return defaultMenuPreferenceItems().map((item) => ({
    ...item,
    visible: byID.get(item.menuItemID) ?? item.visible
  }));
}
