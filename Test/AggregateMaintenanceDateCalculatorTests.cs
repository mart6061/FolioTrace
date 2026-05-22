using Services;

namespace Test;

public sealed class AggregateMaintenanceDateCalculatorTests
{
    [Fact]
    public void CreateValuationDates_UsesEndOfDayForEveryGeneratedDate()
    {
        var options = new AggregateMaintenanceDateWindowOptions
        {
            DaysFromToday = 2,
            EndOfWeeksFromToday = 1,
            EndOfMonthsFromToday = 1
        };
        var now = new DateTime(2026, 5, 22, 10, 30, 0);

        var dates = AggregateMaintenanceDateCalculator.CreateValuationDates(options, now);

        Assert.All(dates, date =>
        {
            var value = date.Value;
            Assert.Equal(value.Date.AddDays(1).AddTicks(-1), value);
        });
    }

    [Fact]
    public void CreateValuationDates_IncludesTodayCurrentWeekEndAndCurrentMonthEnd()
    {
        var options = new AggregateMaintenanceDateWindowOptions
        {
            DaysFromToday = 0,
            EndOfWeeksFromToday = 0,
            EndOfMonthsFromToday = 0
        };
        var now = new DateTime(2026, 5, 22, 10, 30, 0);

        var dates = AggregateMaintenanceDateCalculator.CreateValuationDates(options, now)
            .Select(date => date.Value)
            .ToList();

        Assert.Contains(new DateTime(2026, 5, 22).AddDays(1).AddTicks(-1), dates);
        Assert.Contains(new DateTime(2026, 5, 24).AddDays(1).AddTicks(-1), dates);
        Assert.Contains(new DateTime(2026, 5, 31).AddDays(1).AddTicks(-1), dates);
    }

    [Fact]
    public void CreateValuationDates_DeduplicatesOverlappingDates()
    {
        var options = new AggregateMaintenanceDateWindowOptions
        {
            DaysFromToday = 0,
            EndOfWeeksFromToday = 0,
            EndOfMonthsFromToday = 0
        };
        var sundayMonthEnd = new DateTime(2026, 5, 31, 10, 30, 0);

        var dates = AggregateMaintenanceDateCalculator.CreateValuationDates(options, sundayMonthEnd);

        Assert.Single(dates);
        Assert.Equal(new DateTime(2026, 5, 31).AddDays(1).AddTicks(-1), dates[0].Value);
    }
}

