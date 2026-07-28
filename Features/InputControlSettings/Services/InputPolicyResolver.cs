using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InputPolicyResolver
{
    private const short TypeDecimalPlaces = 8;

    public static InputControlPolicy Resolve(
        InputControlKind controlKind,
        IReadOnlyList<InputControlSetting> settings,
        IReadOnlyList<Currency> currencies,
        AccountID? accountID,
        UserID? userID,
        Alpha3? currency,
        bool? allowNegative)
    {
        var applicableSettings = ApplicableSettings(controlKind, settings, accountID, userID).ToList();
        var defaultPolicy = DefaultPolicy(controlKind, allowNegative);
        var decimalPlaces = UsesCurrencyDecimalPlaces(controlKind)
            ? defaultPolicy.DecimalPlaces
            : ResolveDecimalPlaces(defaultPolicy.DecimalPlaces, applicableSettings);
        var minValue = ResolveMinValue(defaultPolicy.MinValue, applicableSettings);
        var maxValue = ResolveMaxValue(defaultPolicy.MaxValue, applicableSettings);
        var absolute = ResolveAbsoluteSettings(defaultPolicy, applicableSettings);
        var validationMessages = new List<string>();

        if (RequiresCurrency(controlKind))
        {
            if (currency is null)
            {
                validationMessages.Add($"Currency is required for {controlKind} input policies.");
            }
            else
            {
                var currencyDefinition = currencies.SingleOrDefault(item => item.AlphabeticCode == currency);
                if (currencyDefinition is null)
                {
                    validationMessages.Add($"Currency '{currency.Value}' was not found.");
                }
                else if (UsesCurrencyDecimalPlaces(controlKind))
                {
                    decimalPlaces = currencyDefinition.DecimalPlace;
                }
            }
        }

        if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            validationMessages.Add("Resolved MinValue is greater than resolved MaxValue.");

        if (!InputDecimalFormatPattern.IsValid(absolute.FormatPattern))
            validationMessages.Add($"Resolved FormatPattern '{absolute.FormatPattern}' is invalid.");

        return new InputControlPolicy(
            controlKind,
            decimalPlaces,
            minValue,
            maxValue,
            absolute.FormatPattern,
            absolute.FormatSource,
            allowNegative ?? absolute.AllowNegative,
            RequiresCurrency(controlKind) ? currency?.Value : null,
            validationMessages);
    }

    public static IReadOnlyList<string> ValidateValue(string? value, InputControlPolicy policy)
    {
        var messages = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
            return messages;

        if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            messages.Add("Value must be a number.");
            return messages;
        }

        if (!policy.AllowNegative && parsed < 0)
            messages.Add("Value must not be negative.");

        if (policy.MinValue.HasValue && parsed < policy.MinValue.Value)
            messages.Add($"Value must be at least {policy.MinValue.Value:0.########}.");

        if (policy.MaxValue.HasValue && parsed > policy.MaxValue.Value)
            messages.Add($"Value must be no more than {policy.MaxValue.Value:0.########}.");

        if (DecimalPlaces(parsed) > policy.DecimalPlaces)
            messages.Add($"Value can have at most {policy.DecimalPlaces} decimal places.");

        return messages;
    }

    /// <summary>
    /// Only Money needs a currency to resolve, because only Money takes its precision from one. A price is
    /// denominated in a currency too, but that currency does not change the resolved policy, so requiring it
    /// here would demand information that cannot affect the answer. It also has to vary per row in places such
    /// as an FX rate grid, where each pair quotes in its own currency. The currency belongs on the control,
    /// which uses it to label the value.
    /// </summary>
    private static bool RequiresCurrency(InputControlKind controlKind) =>
        controlKind == InputControlKind.Money;

    /// <summary>
    /// Only Money takes its decimal places from the currency definition. Every other kind resolves them from the
    /// applicable Global, Account, and User settings, taking the most restrictive value.
    /// </summary>
    private static bool UsesCurrencyDecimalPlaces(InputControlKind controlKind) =>
        controlKind == InputControlKind.Money;

    private static IEnumerable<InputControlSetting> ApplicableSettings(InputControlKind controlKind, IReadOnlyList<InputControlSetting> settings, AccountID? accountID, UserID? userID) =>
        settings.Where(setting => setting.ControlKind == controlKind)
            .Where(setting => setting.Scope switch
            {
                InputControlSettingScope.Global => true,
                InputControlSettingScope.Account => accountID is not null && setting.AccountID == accountID,
                InputControlSettingScope.User => userID is not null && setting.UserID == userID,
                _ => false
            });

    private static InputControlPolicy DefaultPolicy(InputControlKind controlKind, bool? allowNegative)
    {
        var resolvedAllowNegative = allowNegative ?? false;
        return controlKind switch
        {
            InputControlKind.Quantity => new InputControlPolicy(controlKind, TypeDecimalPlaces, 0.00000001m, null, "#,##0.########", "TypeDefault", false, null, []),
            InputControlKind.Money => new InputControlPolicy(controlKind, TypeDecimalPlaces, resolvedAllowNegative ? null : 0m, null, "#,##0.00######", "TypeDefault", resolvedAllowNegative, null, []),
            // Prices are never negative, and zero is permitted because FX rates allow it.
            InputControlKind.Price => new InputControlPolicy(controlKind, TypeDecimalPlaces, 0m, null, "#,##0.00######", "TypeDefault", false, null, []),
            // Percent values are stored as fractions, so DecimalPlaces and Min/Max are expressed as fractions too:
            // a "no more than 100%" ceiling is MaxValue 1, not 100. Negatives are caller-controlled because
            // performance and yield figures can legitimately fall below zero.
            InputControlKind.Percent => new InputControlPolicy(controlKind, TypeDecimalPlaces, resolvedAllowNegative ? null : 0m, null, "#,##0.########", "TypeDefault", resolvedAllowNegative, null, []),
            _ => throw new ArgumentOutOfRangeException(nameof(controlKind), controlKind, null)
        };
    }

    private static short ResolveDecimalPlaces(short defaultValue, IReadOnlyList<InputControlSetting> settings)
    {
        var values = settings.Select(setting => setting.DecimalPlaces).OfType<short>().Append(defaultValue);
        return values.Min();
    }

    private static decimal? ResolveMinValue(decimal? defaultValue, IReadOnlyList<InputControlSetting> settings)
    {
        var values = settings.Select(setting => setting.MinValue).OfType<decimal>().ToList();
        if (defaultValue.HasValue)
            values.Add(defaultValue.Value);

        return values.Count == 0 ? null : values.Max();
    }

    private static decimal? ResolveMaxValue(decimal? defaultValue, IReadOnlyList<InputControlSetting> settings)
    {
        var values = settings.Select(setting => setting.MaxValue).OfType<decimal>().ToList();
        if (defaultValue.HasValue)
            values.Add(defaultValue.Value);

        return values.Count == 0 ? null : values.Min();
    }

    private static (string FormatPattern, string FormatSource, bool AllowNegative) ResolveAbsoluteSettings(InputControlPolicy defaultPolicy, IReadOnlyList<InputControlSetting> settings)
    {
        var formatSetting = HighestPriority(settings.Where(setting => !string.IsNullOrWhiteSpace(setting.FormatPattern)));
        var allowNegativeSetting = HighestPriority(settings.Where(setting => setting.AllowNegative.HasValue));

        return (
            formatSetting?.FormatPattern ?? defaultPolicy.FormatPattern,
            formatSetting?.Scope.ToString() ?? defaultPolicy.FormatSource,
            allowNegativeSetting?.AllowNegative ?? defaultPolicy.AllowNegative);
    }

    private static InputControlSetting? HighestPriority(IEnumerable<InputControlSetting> settings) =>
        settings
            .OrderByDescending(setting => setting.Scope switch
            {
                InputControlSettingScope.Account => 3,
                InputControlSettingScope.Global => 2,
                InputControlSettingScope.User => 1,
                _ => 0
            })
            .FirstOrDefault();

    private static int DecimalPlaces(decimal value)
    {
        value = Math.Abs(value);
        value -= decimal.Truncate(value);

        var places = 0;
        while (value != 0 && places < 29)
        {
            value *= 10;
            value -= decimal.Truncate(value);
            places++;
        }

        return places;
    }
}
