namespace FolioTrace.Aggregates;

public static class UserMenuPreferenceDefaults
{
    public const string Bookmarks = "bookmarks";
    public const string Blotter = "blotter";
    public const string Account = "account";
    public const string Compliance = "compliance";
    public const string Administration = "administration";
    public const string Data = "data";
    public const string System = "system";
    public const string Value = "value";
    public const string Reference = "reference";
    public const string SystemLogs = "system-logs";
    public const string SystemStats = "system-stats";
    public const string Todo = "todo";

    public static readonly IReadOnlyList<string> ControlledMenuItemIDs =
    [
        Bookmarks,
        Blotter,
        Account,
        Compliance,
        Administration,
        Data,
        Value,
        Reference,
        System,
        SystemLogs,
        SystemStats,
        Todo
    ];

    private static readonly HashSet<string> ControlledMenuItemIDSet = ControlledMenuItemIDs.ToHashSet(StringComparer.Ordinal);

    public static bool IsControlled(string menuItemID) =>
        ControlledMenuItemIDSet.Contains(menuItemID);

    public static IReadOnlyList<UserMenuPreferenceItem> CreateVisibleItems() =>
        ControlledMenuItemIDs.Select(menuItemID => new UserMenuPreferenceItem(menuItemID, true)).ToList();

    public static IReadOnlyList<UserMenuPreferenceItem> Normalize(IReadOnlyList<UserMenuPreferenceItem>? items)
    {
        var byID = (items ?? [])
            .Where(item => item is not null && IsControlled(item.MenuItemID))
            .GroupBy(item => item.MenuItemID, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Visible, StringComparer.Ordinal);

        return ControlledMenuItemIDs
            .Select(menuItemID => new UserMenuPreferenceItem(menuItemID, byID.TryGetValue(menuItemID, out var visible) ? visible : true))
            .ToList();
    }
}
