import type { UserMenuPreferenceItem, UserMenuPreferences } from '$lib/types';

const legacyMenuPreferenceIDs = new Map([
  ['value-valuations', 'viewer'],
  ['internals', 'diagnostics'],
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
  { id: 'account', label: 'Account' },
  { id: 'data-list', label: 'Data List' },
  { id: 'data-list-fx', label: 'FX', parentID: 'data-list' },
  { id: 'data-list-instrument', label: 'Instrument', parentID: 'data-list' },
  { id: 'data-list-iso', label: 'ISO', parentID: 'data-list' },
  { id: 'data-list-holding', label: 'Holding', parentID: 'data-list' },
  { id: 'data-list-broker', label: 'Broker', parentID: 'data-list' },
  { id: 'tools', label: 'Tools' },
  { id: 'configuration-account-tools', label: 'Account Tools', parentID: 'tools' },
  { id: 'configuration-asset-allocation', label: 'Asset Allocation', parentID: 'tools' },
  { id: 'configuration-asset-allocation-tools', label: 'Asset Allocation Tools', parentID: 'tools' },
  { id: 'configuration-report-tools', label: 'Report Tools', parentID: 'tools' },
  { id: 'diagnostics', label: 'Diagnostics' },
  { id: 'system-logs', label: 'Request Trace', parentID: 'diagnostics' },
  { id: 'system-fix-trace', label: 'FIX Trace', parentID: 'diagnostics' },
  { id: 'system-stats', label: 'Stats for Nerds', parentID: 'diagnostics' }
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
