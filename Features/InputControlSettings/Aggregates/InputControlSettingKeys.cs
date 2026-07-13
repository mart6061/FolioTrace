using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InputControlSettingKeys
{
    public static string ScopeKey(InputControlSettingScope scope, AccountID? accountID, UserID? userID) =>
        scope switch
        {
            InputControlSettingScope.Global => "Global",
            InputControlSettingScope.Account => $"Account:{accountID?.Value}",
            InputControlSettingScope.User => $"User:{userID?.Value}",
            _ => scope.ToString()
        };

    public static string SettingKey(InputControlKind controlKind, InputControlSettingScope scope, AccountID? accountID, UserID? userID) =>
        $"{controlKind}:{ScopeKey(scope, accountID, userID)}";
}
