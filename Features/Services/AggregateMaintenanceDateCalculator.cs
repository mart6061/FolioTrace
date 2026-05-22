using FolioTrace.Types;

namespace Services;

public static class AggregateMaintenanceDateCalculator
{
    public static IReadOnlyList<EventDateTime> CreateValuationDates(AggregateMaintenanceDateWindowOptions options, DateTime now)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var dates = new SortedSet<DateTime>();

        for (var offset = 0; offset <= Math.Max(0, options.DaysFromToday); offset++)
            dates.Add(EndOfDay(now.Date.AddDays(-offset)));

        var currentWeekEnd = EndOfWeek(now);
        for (var offset = 0; offset <= Math.Max(0, options.EndOfWeeksFromToday); offset++)
            dates.Add(EndOfDay(currentWeekEnd.AddDays(-7 * offset)));

        var currentMonthEnd = EndOfMonth(now);
        for (var offset = 0; offset <= Math.Max(0, options.EndOfMonthsFromToday); offset++)
            dates.Add(EndOfDay(currentMonthEnd.AddMonths(-offset)));

        return dates
            .Select(EventDateTimeBuilder.Create)
            .ToList();
    }

    public static DateTime EndOfDay(DateTime value) => value.Date.AddDays(1).AddTicks(-1);

    private static DateTime EndOfWeek(DateTime value)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)value.DayOfWeek + 7) % 7;
        return value.Date.AddDays(daysUntilSunday);
    }

    private static DateTime EndOfMonth(DateTime value) =>
        new DateTime(value.Year, value.Month, 1).AddMonths(1).AddTicks(-1).Date;
}

