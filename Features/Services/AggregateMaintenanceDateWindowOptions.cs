namespace Services;

public sealed class AggregateMaintenanceDateWindowOptions
{
    public int DaysFromToday { get; set; } = 0;

    public int EndOfWeeksFromToday { get; set; } = 0;

    public int EndOfMonthsFromToday { get; set; } = 0;
}
