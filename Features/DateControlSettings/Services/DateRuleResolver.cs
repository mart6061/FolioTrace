using System.Globalization;
using System.Text.RegularExpressions;

namespace FolioTrace.Aggregates;

public static partial class DateRuleResolver
{
    [GeneratedRegex("^(day|bd|week|month|quarter|year|fy)\\.([+-]\\d+)\\.(start|end|at\\(([01]\\d|2[0-3]):([0-5]\\d)\\))$")]
    private static partial Regex DateExpressionPattern();

    [GeneratedRegex("^range\\.(day|bd|week|month|quarter|year|fy)\\.([+-]\\d+)$")]
    private static partial Regex AlignedRangePattern();

    [GeneratedRegex("^range\\.(next|last)\\.([1-9]\\d*)\\.(day|week|month|quarter|year)$")]
    private static partial Regex RollingRangePattern();

    public static bool IsValidDateExpression(string? expression) =>
        expression == "now" || (!string.IsNullOrWhiteSpace(expression) && DateExpressionPattern().IsMatch(expression));

    public static bool IsValidRangeExpression(string? expression) =>
        expression is "range.mtd" or "range.ytd"
        || (!string.IsNullOrWhiteSpace(expression)
            && (AlignedRangePattern().IsMatch(expression) || RollingRangePattern().IsMatch(expression)));

    public static ResolvedDateRule ResolveDate(string expression, DateTime now, int financialYearStartMonth, int financialYearStartDay)
    {
        if (expression == "now")
            return new(now, now.AddSeconds(1));

        var match = DateExpressionPattern().Match(expression);
        if (!match.Success)
            throw new ArgumentException($"Unsupported date expression '{expression}'.", nameof(expression));

        var period = match.Groups[1].Value;
        var offset = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var boundary = match.Groups[3].Value;
        var date = StartOfDay(now);

        date = period switch
        {
            "day" => date.AddDays(offset),
            "bd" => AddBusinessDays(date, offset),
            "week" => StartOfWeek(date).AddDays(offset * 7),
            "month" => new DateTime(date.Year, date.Month, 1).AddMonths(offset),
            "quarter" => new DateTime(date.Year, ((date.Month - 1) / 3 * 3) + 1, 1).AddMonths(offset * 3),
            "year" => new DateTime(date.Year + offset, 1, 1),
            "fy" => FinancialYearStart(date, financialYearStartMonth, financialYearStartDay).AddYears(offset),
            _ => throw new ArgumentException($"Unsupported date period '{period}'.", nameof(expression))
        };

        if (boundary == "end")
        {
            date = period switch
            {
                "week" => date.AddDays(7).AddSeconds(-1),
                "month" => date.AddMonths(1).AddSeconds(-1),
                "quarter" => date.AddMonths(3).AddSeconds(-1),
                "year" => date.AddYears(1).AddSeconds(-1),
                "fy" => RecurringDate(date.Year + 1, financialYearStartMonth, financialYearStartDay).AddSeconds(-1),
                _ => date.AddDays(1).AddSeconds(-1)
            };
        }
        else if (boundary.StartsWith("at(", StringComparison.Ordinal))
        {
            date = date.AddHours(int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture))
                .AddMinutes(int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture));
        }

        return new(date, Expiry(period, now, financialYearStartMonth, financialYearStartDay));
    }

    public static ResolvedDateRangeRule ResolveRange(string expression, DateTime now, int financialYearStartMonth, int financialYearStartDay)
    {
        var today = StartOfDay(now);
        DateTime start;
        DateTime endExclusive;
        string expiryPeriod;

        if (expression is "range.mtd" or "range.ytd")
        {
            start = expression == "range.mtd" ? new DateTime(today.Year, today.Month, 1) : new DateTime(today.Year, 1, 1);
            endExclusive = today.AddDays(1);
            expiryPeriod = "day";
        }
        else if (RollingRangePattern().Match(expression) is { Success: true } rolling)
        {
            var direction = rolling.Groups[1].Value;
            var count = int.Parse(rolling.Groups[2].Value, CultureInfo.InvariantCulture);
            var period = rolling.Groups[3].Value;
            if (direction == "next")
            {
                start = today;
                endExclusive = AddCalendarUnits(start, count, period);
            }
            else
            {
                endExclusive = today.AddDays(1);
                start = AddCalendarUnits(endExclusive, -count, period);
            }
            expiryPeriod = "day";
        }
        else
        {
            var aligned = AlignedRangePattern().Match(expression);
            if (!aligned.Success)
                throw new ArgumentException($"Unsupported range expression '{expression}'.", nameof(expression));

            var period = aligned.Groups[1].Value;
            var offset = int.Parse(aligned.Groups[2].Value, CultureInfo.InvariantCulture);
            (start, endExclusive) = period switch
            {
                "day" => (today.AddDays(offset), today.AddDays(offset + 1)),
                "bd" => BusinessDayBounds(today, offset),
                "week" => PeriodBounds(StartOfWeek(today).AddDays(offset * 7), 0, 7),
                "month" => MonthBounds(new DateTime(today.Year, today.Month, 1).AddMonths(offset), 1),
                "quarter" => MonthBounds(new DateTime(today.Year, ((today.Month - 1) / 3 * 3) + 1, 1).AddMonths(offset * 3), 3),
                "year" => YearBounds(new DateTime(today.Year + offset, 1, 1)),
                "fy" => FinancialYearBounds(FinancialYearStart(today, financialYearStartMonth, financialYearStartDay).AddYears(offset), financialYearStartMonth, financialYearStartDay),
                _ => throw new ArgumentException($"Unsupported range period '{period}'.", nameof(expression))
            };
            expiryPeriod = period;
        }

        return new(start, endExclusive.AddSeconds(-1), Expiry(expiryPeriod, now, financialYearStartMonth, financialYearStartDay));
    }

    private static (DateTime Start, DateTime End) BusinessDayBounds(DateTime today, int offset)
    {
        var start = AddBusinessDays(today, offset);
        return (start, start.AddDays(1));
    }

    private static (DateTime Start, DateTime End) PeriodBounds(DateTime start, int _, int days) => (start, start.AddDays(days));
    private static (DateTime Start, DateTime End) MonthBounds(DateTime start, int months) => (start, start.AddMonths(months));
    private static (DateTime Start, DateTime End) YearBounds(DateTime start) => (start, start.AddYears(1));
    private static (DateTime Start, DateTime End) FinancialYearBounds(DateTime start, int month, int day) => (start, RecurringDate(start.Year + 1, month, day));

    private static DateTime Expiry(string period, DateTime now, int financialYearStartMonth, int financialYearStartDay) => period switch
    {
        "week" => StartOfWeek(now).AddDays(7),
        "month" => new DateTime(now.Year, now.Month, 1).AddMonths(1),
        "quarter" => new DateTime(now.Year, ((now.Month - 1) / 3 * 3) + 1, 1).AddMonths(3),
        "year" => new DateTime(now.Year + 1, 1, 1),
        "fy" => FinancialYearStart(now, financialYearStartMonth, financialYearStartDay).AddYears(1),
        _ => StartOfDay(now).AddDays(1)
    };

    private static DateTime StartOfDay(DateTime value) => value.Date;

    private static DateTime StartOfWeek(DateTime value)
    {
        var offset = ((int)value.DayOfWeek + 6) % 7;
        return value.Date.AddDays(-offset);
    }

    private static DateTime AddBusinessDays(DateTime value, int offset)
    {
        var result = value.Date;
        if (offset == 0)
        {
            while (result.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                result = result.AddDays(1);
            return result;
        }

        var direction = Math.Sign(offset);
        var remaining = Math.Abs(offset);
        while (remaining > 0)
        {
            result = result.AddDays(direction);
            if (result.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                remaining--;
        }
        return result;
    }

    private static DateTime AddCalendarUnits(DateTime value, int count, string period) => period switch
    {
        "day" => value.AddDays(count),
        "week" => value.AddDays(count * 7),
        "month" => value.AddMonths(count),
        "quarter" => value.AddMonths(count * 3),
        "year" => value.AddYears(count),
        _ => value
    };

    private static DateTime FinancialYearStart(DateTime value, int month, int day)
    {
        var thisYear = RecurringDate(value.Year, month, day);
        return value >= thisYear ? thisYear : RecurringDate(value.Year - 1, month, day);
    }

    private static DateTime RecurringDate(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
}
