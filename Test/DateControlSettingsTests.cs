using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class DateControlSettingsTests
{
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Defaults_AreValidAndUseRequestedSelections()
    {
        var configuration = DateControlConfigurationDefaults.Create();

        Assert.Empty(DateControlSettingsValidation.Validate(configuration, false));
        Assert.Equal("day.+0.end", configuration.DateOptions.Single(item => item.IsDefault).Expression);
        Assert.Equal("range.bd.-1", configuration.RangeOptions.Single(item => item.IsDefault).Expression);
    }

    [Fact]
    public void Validation_RejectsInvalidExpressionsAndMissingDefaults()
    {
        var configuration = DateControlConfigurationDefaults.Create() with
        {
            DateOptions = [new(Guid.NewGuid(), DateControlOptionKind.Rule, "Broken", "month.end", 1, false)]
        };

        var errors = DateControlSettingsValidation.Validate(configuration, false);

        Assert.Contains(errors, message => message.Contains("default", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, message => message.Contains("expressions", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("day.+0.end", 2026, 7, 31, 23, 59, 59)]
    [InlineData("month.-1.end", 2026, 6, 30, 23, 59, 59)]
    [InlineData("quarter.+0.start", 2026, 7, 1, 0, 0, 0)]
    [InlineData("fy.+0.end", 2027, 4, 5, 23, 59, 59)]
    public void DateResolver_ResolvesCalendarBoundaries(string expression, int year, int month, int day, int hour, int minute, int second)
    {
        var now = new DateTime(2026, 7, 31, 12, 30, 0);

        var result = DateRuleResolver.ResolveDate(expression, now, 4, 6);

        Assert.Equal(new DateTime(year, month, day, hour, minute, second), result.Value);
    }

    [Fact]
    public void BusinessDayResolver_SkipsWeekends()
    {
        var monday = new DateTime(2026, 8, 3, 9, 0, 0);

        Assert.Equal(new DateTime(2026, 7, 31, 23, 59, 59), DateRuleResolver.ResolveDate("bd.-1.end", monday, 4, 6).Value);
        Assert.Equal(new DateTime(2026, 8, 4, 23, 59, 59), DateRuleResolver.ResolveDate("bd.+1.end", monday, 4, 6).Value);
    }

    [Fact]
    public void RangeResolver_ReturnsInclusiveEndAndAllowsLeapMonth()
    {
        var result = DateRuleResolver.ResolveRange("range.month.+0", new DateTime(2028, 2, 14, 10, 0, 0), 4, 6);

        Assert.Equal(new DateTime(2028, 2, 1, 0, 0, 0), result.Start);
        Assert.Equal(new DateTime(2028, 2, 29, 23, 59, 59), result.End);
        Assert.Equal(new DateTime(2028, 3, 1, 0, 0, 0), result.ExpiresAt);
    }

    [Fact]
    public void UserClear_RemovesOverrideSoGlobalCanTakePriority()
    {
        var request = new UserDateControlSettingsRequest(new UserID(Guid.NewGuid()), EventDate, "Create user date controls", DateControlConfigurationDefaults.Create());
        var created = UserDateControlSettingsCreatedEventBuilder.Create(request).Value!;
        var clear = UserDateControlSettingsClearedEventBuilder.Create(new(request.UserID, EventDateTimeBuilder.Create(EventDate.Value.AddTicks(1)), "Clear user date controls")).Value!;
        var asOf = AuditDateTimeBuilder.Create(DateTime.UtcNow);

        var before = new UserDateControlSettings(request.UserID, EventDate, asOf, [created]);
        var after = new UserDateControlSettings(request.UserID, clear.EventDateTime, asOf, [created, clear]);

        Assert.True(before.HasStoredConfiguration);
        Assert.False(after.HasStoredConfiguration);
        Assert.Empty(after.Configuration.DateOptions);
        Assert.Empty(after.Configuration.RangeOptions);
    }
}
