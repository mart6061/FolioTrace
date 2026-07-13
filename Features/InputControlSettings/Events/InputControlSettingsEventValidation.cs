using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class InputControlSettingsEventValidation
{
    public static IReadOnlyList<string> Validate(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, IReadOnlyList<InputControlSettingDefinition>? settings)
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

        ValidateSettings(messages, settings);

        return messages;
    }

    private static void ValidateSettings(List<string> messages, IReadOnlyList<InputControlSettingDefinition>? settings)
    {
        if (settings is null)
        {
            messages.Add("Settings are required.");
            return;
        }

        if (settings.Count == 0)
            messages.Add("Settings must contain at least one item.");

        if (settings.Any(setting => setting is null))
        {
            messages.Add("Settings must not contain null values.");
            return;
        }

        foreach (var setting in settings)
            ValidateSetting(messages, setting);

        foreach (var duplicate in settings
            .GroupBy(setting => InputControlSettingKeys.SettingKey(setting.ControlKind, setting.Scope, setting.AccountID, setting.UserID), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            messages.Add($"Input control setting '{duplicate}' is duplicated.");
        }
    }

    private static void ValidateSetting(List<string> messages, InputControlSettingDefinition setting)
    {
        if (!Enum.IsDefined(setting.ControlKind))
            messages.Add("ControlKind is invalid.");

        if (!Enum.IsDefined(setting.Scope))
            messages.Add("Scope is invalid.");

        switch (setting.Scope)
        {
            case InputControlSettingScope.Global:
                if (setting.AccountID is not null || setting.UserID is not null)
                    messages.Add("Global input control settings must not specify AccountID or UserID.");
                break;
            case InputControlSettingScope.Account:
                if (setting.AccountID is null)
                    messages.Add("Account input control settings require AccountID.");
                if (setting.UserID is not null)
                    messages.Add("Account input control settings must not specify UserID.");
                break;
            case InputControlSettingScope.User:
                if (setting.UserID is null)
                    messages.Add("User input control settings require UserID.");
                if (setting.AccountID is not null)
                    messages.Add("User input control settings must not specify AccountID.");
                break;
        }

        if (setting.DecimalPlaces is < 0 or > 8)
            messages.Add("DecimalPlaces must be between 0 and 8.");

        if (setting.ControlKind == InputControlKind.Money && setting.DecimalPlaces.HasValue)
            messages.Add("Money input control settings must not specify DecimalPlaces; currency decimal places are used.");

        if (setting.MinValue.HasValue && decimal.Round(setting.MinValue.Value, 8) != setting.MinValue.Value)
            messages.Add("MinValue can have at most 8 decimal places.");

        if (setting.MaxValue.HasValue && decimal.Round(setting.MaxValue.Value, 8) != setting.MaxValue.Value)
            messages.Add("MaxValue can have at most 8 decimal places.");

        if (setting.MinValue.HasValue && setting.MaxValue.HasValue && setting.MinValue.Value > setting.MaxValue.Value)
            messages.Add("MinValue must be less than or equal to MaxValue.");

        if (!string.IsNullOrWhiteSpace(setting.FormatPattern) && !InputDecimalFormatPattern.IsValid(setting.FormatPattern))
            messages.Add($"FormatPattern '{setting.FormatPattern}' is invalid.");

        if (setting.DecimalPlaces is null &&
            setting.MinValue is null &&
            setting.MaxValue is null &&
            string.IsNullOrWhiteSpace(setting.FormatPattern) &&
            setting.AllowNegative is null)
        {
            messages.Add("Input control settings must specify at least one setting value.");
        }
    }
}
