namespace FolioTrace.Aggregates;

public static class DateControlConfigurationDefaults
{
    public static DateControlConfiguration Create() => new(4, 6,
        [
            Date("Today", "day.+0.end", 1, true), Date("Now", "now", 2), Date("Yesterday", "day.-1.end", 3),
            Date("Last business day", "bd.-1.end", 4), Date("End of last week", "week.-1.end", 5), Date("End of last month", "month.-1.end", 6),
            Date("T + 1", "bd.+1.end", 7), Date("T + 2", "bd.+2.end", 8), Date("T + 3", "bd.+3.end", 9),
            new(Guid.Parse("39f4e6b9-2b9d-4793-a377-1f2f4b500010"), DateControlOptionKind.Custom, "Custom", null, 10, false)
        ],
        [
            Range("Today", "range.day.+0", 1), Range("Yesterday", "range.day.-1", 2), Range("Last business day", "range.bd.-1", 3, true),
            Range("This week", "range.week.+0", 4), Range("Last week", "range.week.-1", 5), Range("This month", "range.month.+0", 6),
            Range("Last month", "range.month.-1", 7), Range("Month to date", "range.mtd", 8), Range("Year to date", "range.ytd", 9),
            new(Guid.Parse("4bdb6172-b590-427d-aa5f-734f94f30010"), DateControlOptionKind.Custom, "Custom", null, 10, false)
        ]);

    private static DateRuleOption Date(string label, string expression, int order, bool isDefault = false) =>
        new(Guid.Parse($"39f4e6b9-2b9d-4793-a377-1f2f4b5000{order:00}"), DateControlOptionKind.Rule, label, expression, order, isDefault);
    private static DateRangeRuleOption Range(string label, string expression, int order, bool isDefault = false) =>
        new(Guid.Parse($"4bdb6172-b590-427d-aa5f-734f94f300{order:00}"), DateControlOptionKind.Rule, label, expression, order, isDefault);
}
