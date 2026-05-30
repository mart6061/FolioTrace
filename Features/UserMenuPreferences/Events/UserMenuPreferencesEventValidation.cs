using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class UserMenuPreferencesEventValidation
{
    public static IReadOnlyList<string> Validate(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, IReadOnlyList<UserMenuPreferenceItem>? items)
    {
        var messages = new List<string>();

        if (eventID is null)
            messages.Add("EventID is required.");

        if (userID is null)
            messages.Add("UserID is required.");

        if (eventDateTime is null)
            messages.Add("EventDateTime is required.");

        if (auditDateTime is null)
            messages.Add("AuditDateTime is required.");

        if (string.IsNullOrWhiteSpace(reason))
            messages.Add("Reason is required.");

        ValidateItems(messages, items);

        return messages;
    }

    private static void ValidateItems(List<string> messages, IReadOnlyList<UserMenuPreferenceItem>? items)
    {
        if (items is null)
        {
            messages.Add("Items are required.");
            return;
        }

        if (items.Any(item => item is null))
        {
            messages.Add("Items must not contain null values.");
            return;
        }

        var ids = items.Select(item => item.MenuItemID).ToList();

        foreach (var itemID in ids.Where(string.IsNullOrWhiteSpace))
            messages.Add("MenuItemID is required.");

        if (ids.Any(id => string.Equals(id, "home", StringComparison.Ordinal)))
            messages.Add("Home menu visibility cannot be configured.");

        foreach (var itemID in ids.Where(id => !string.IsNullOrWhiteSpace(id) && !UserMenuPreferenceDefaults.IsControlled(id)).Distinct(StringComparer.Ordinal))
            messages.Add($"Unknown menu item ID '{itemID}'.");

        foreach (var duplicate in ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key))
            messages.Add($"Menu item ID '{duplicate}' is duplicated.");

        foreach (var missing in UserMenuPreferenceDefaults.ControlledMenuItemIDs.Except(ids, StringComparer.Ordinal))
            messages.Add($"Menu item ID '{missing}' is required.");
    }
}
