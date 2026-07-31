namespace FolioTrace.Aggregates;

public sealed record DateControlConfiguration(int FinancialYearStartMonth, int FinancialYearStartDay, IReadOnlyList<DateRuleOption> DateOptions, IReadOnlyList<DateRangeRuleOption> RangeOptions)
{
    public static DateControlConfiguration Empty { get; } = new(4, 6, [], []);
}
