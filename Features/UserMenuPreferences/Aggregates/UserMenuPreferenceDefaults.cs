namespace FolioTrace.Aggregates;

public static class UserMenuPreferenceDefaults
{
    public const string Bookmarks = "bookmarks";
    public const string Blotter = "blotter";
    public const string Asset = "asset";
    public const string Report = "report";
    public const string Account = "account";
    public const string Administration = "administration";
    public const string System = "system";
    public const string Data = "data";
    public const string Value = "value";
    public const string Reference = "reference";
    public const string SystemLogs = "system-logs";
    public const string SystemStats = "system-stats";
    public const string Todo = "todo";

    public static readonly IReadOnlyList<string> ControlledMenuItemIDs =
    [
        Bookmarks,
        Blotter,
        Asset,
        Report,
        Account,
        Administration,
        System,
        Data,
        Value,
        Reference,
        SystemLogs,
        SystemStats,
        Todo
    ];

    private static readonly HashSet<string> ControlledMenuItemIDSet = ControlledMenuItemIDs.ToHashSet(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> LegacyMenuItemIDs = new(StringComparer.Ordinal)
    {
        ["value-valuations"] = Asset
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
