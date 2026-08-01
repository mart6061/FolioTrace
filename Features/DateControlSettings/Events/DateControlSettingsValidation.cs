namespace FolioTrace.Aggregates;

public static class DateControlSettingsValidation
{
    public static IReadOnlyList<string> Validate(DateControlConfiguration? configuration, bool allowEmpty)
    {
        var messages = new List<string>();
        if (configuration is null) return ["Configuration is required."];
        if (configuration.FinancialYearStartMonth is < 1 or > 12) messages.Add("Financial year start month must be between 1 and 12.");
        if (configuration.FinancialYearStartDay < 1 || configuration.FinancialYearStartDay > DateTime.DaysInMonth(2000, Math.Clamp(configuration.FinancialYearStartMonth, 1, 12))) messages.Add("Financial year start day is invalid for the selected month.");
        ValidateOptions(messages, configuration.DateOptions, allowEmpty, DateRuleResolver.IsValidDateExpression, "date");
        ValidateOptions(messages, configuration.RangeOptions, allowEmpty, DateRuleResolver.IsValidRangeExpression, "range");
        if (allowEmpty && (configuration.DateOptions.Count == 0) != (configuration.RangeOptions.Count == 0)) messages.Add("User date and range options must both be empty when clearing an override.");
        return messages;
    }
    private static void ValidateOptions<T>(List<string> messages, IReadOnlyList<T> items, bool allowEmpty, Func<string?, bool> valid, string label) where T : notnull
    {
        var values = items.Select(item => item switch { DateRuleOption x => (x.OptionID, x.Kind, x.Label, x.Expression, x.DisplayOrder, x.IsDefault), DateRangeRuleOption x => (x.OptionID, x.Kind, x.Label, x.Expression, x.DisplayOrder, x.IsDefault), _ => default }).ToList();
        if (values.Count == 0) { if (!allowEmpty) messages.Add($"At least one {label} option is required."); return; }
        if (values.Any(x => x.OptionID == Guid.Empty)) messages.Add($"Every {label} option requires an option ID.");
        if (values.GroupBy(x => x.OptionID).Any(x => x.Count() > 1)) messages.Add($"{label} option IDs must be unique.");
        if (values.Any(x => string.IsNullOrWhiteSpace(x.Label) && x.Kind != DateControlOptionKind.Separator)) messages.Add($"Every selectable {label} option requires a label.");
        if (values.Count(x => x.Kind == DateControlOptionKind.Custom) != 1) messages.Add($"Exactly one custom {label} option is required.");
        if (values.Count(x => x.Kind != DateControlOptionKind.Separator && x.IsDefault) != 1) messages.Add($"Exactly one selectable {label} option must be the default.");
        if (values.Any(x => x.Kind == DateControlOptionKind.Rule && !valid(x.Expression))) messages.Add($"One or more {label} expressions are invalid.");
        if (values.Any(x => x.Kind != DateControlOptionKind.Rule && !string.IsNullOrWhiteSpace(x.Expression))) messages.Add($"Only rule {label} options may have expressions.");
        if (values.Any(x => x.Kind == DateControlOptionKind.Separator && x.IsDefault)) messages.Add($"A {label} separator cannot be the default.");
        if (values.Select(x => x.DisplayOrder).Distinct().Count() != values.Count) messages.Add($"{label} display orders must be unique.");
        if (!values.Select(x => x.DisplayOrder).Order().SequenceEqual(Enumerable.Range(1, values.Count))) messages.Add($"{label} display orders must be contiguous and start at one.");
    }
}
