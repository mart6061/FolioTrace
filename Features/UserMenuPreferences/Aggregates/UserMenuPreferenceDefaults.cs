namespace FolioTrace.Aggregates;

public static class UserMenuPreferenceDefaults
{
    public const string Bookmarks = "bookmarks";
    public const string Blotter = "blotter";
    public const string Viewer = "viewer";
    public const string Account = "account";
    public const string DataList = "data-list";
    public const string DataListFX = "data-list-fx";
    public const string DataListInstrument = "data-list-instrument";
    public const string DataListISO = "data-list-iso";
    public const string DataListHolding = "data-list-holding";
    public const string DataListBroker = "data-list-broker";
    public const string ConfigurationAssetAllocation = "configuration-asset-allocation";
    public const string Tools = "tools";
    public const string ConfigurationAccountTools = "configuration-account-tools";
    public const string ConfigurationAssetAllocationTools = "configuration-asset-allocation-tools";
    public const string ConfigurationReportTools = "configuration-report-tools";
    public const string Diagnostics = "diagnostics";
    public const string SystemLogs = "system-logs";
    public const string SystemFixTrace = "system-fix-trace";
    public const string SystemStats = "system-stats";
    public const string Ideas = "ideas";

    public static readonly IReadOnlyList<string> ControlledMenuItemIDs =
    [
        Bookmarks,
        Blotter,
        Viewer,
        Account,
        DataList,
        DataListFX,
        DataListInstrument,
        DataListISO,
        DataListHolding,
        DataListBroker,
        Tools,
        ConfigurationAccountTools,
        ConfigurationAssetAllocation,
        ConfigurationAssetAllocationTools,
        ConfigurationReportTools,
        Diagnostics,
        SystemLogs,
        SystemFixTrace,
        SystemStats,
        Ideas
    ];

    private static readonly HashSet<string> ControlledMenuItemIDSet = ControlledMenuItemIDs.ToHashSet(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> LegacyMenuItemIDs = new(StringComparer.Ordinal)
    {
        ["asset"] = Viewer,
        ["report"] = Viewer,
        ["administration"] = Tools,
        ["system"] = Diagnostics,
        ["data"] = DataList,
        ["value"] = Viewer,
        ["reference"] = DataList,
        ["configuration"] = Tools,
        ["internals"] = Diagnostics,
        ["value-valuations"] = Viewer,
        ["reference-valuation-setting"] = ConfigurationAssetAllocationTools,
        ["configuration-valuation-setting"] = ConfigurationAssetAllocationTools
    };

    public static bool IsControlled(string? menuItemID) =>
        !string.IsNullOrWhiteSpace(menuItemID) && (ControlledMenuItemIDSet.Contains(menuItemID) || LegacyMenuItemIDs.ContainsKey(menuItemID));

    public static IReadOnlyList<UserMenuPreferenceItem> CreateVisibleItems() =>
        ControlledMenuItemIDs.Select(menuItemID => new UserMenuPreferenceItem(menuItemID, true)).ToList();

    public static IReadOnlyList<UserMenuPreferenceItem> Normalize(IReadOnlyList<UserMenuPreferenceItem>? items)
    {
        var byID = (items ?? [])
            .Where(item => item is not null && IsControlled(item.MenuItemID))
            .GroupBy(item => NormalizeID(item.MenuItemID), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Visible, StringComparer.Ordinal);

        return ControlledMenuItemIDs
            .Select(menuItemID => new UserMenuPreferenceItem(menuItemID, byID.TryGetValue(menuItemID, out var visible) ? visible : true))
            .ToList();
    }

    public static string NormalizeID(string? menuItemID) =>
        menuItemID is null ? string.Empty : LegacyMenuItemIDs.GetValueOrDefault(menuItemID, menuItemID);
}
