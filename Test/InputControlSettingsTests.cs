using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class InputControlSettingsTests
{
    private static readonly UserID UserID = new(Guid.Parse("9461ecf2-9632-4088-af90-a08875a5223d"));
    private static readonly UserID OtherUserID = new(Guid.Parse("1379b19d-5f47-4891-83a2-88eb28b8694b"));
    private static readonly AccountID AccountID = new(Guid.Parse("ddc37fbb-851d-40c9-8e1f-f0408bd7b341"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2026, 7, 9, 12, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime FirstAuditDate = AuditDateTimeBuilder.Create(new DateTime(2026, 7, 9, 12, 0, 1, DateTimeKind.Utc));
    private static readonly AuditDateTime SecondAuditDate = AuditDateTimeBuilder.Create(new DateTime(2026, 7, 9, 12, 0, 2, DateTimeKind.Utc));

    [Fact]
    public void CreatedBuilder_RejectsDuplicateSettings()
    {
        var settings = new[]
        {
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.Global, null, null, 4, null, null, null, null),
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.Global, null, null, 2, null, null, null, null)
        };

        var result = InputControlSettingsCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            FirstAuditDate,
            "Create settings",
            settings);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, error => error.Contains("duplicated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ModifiedEvent_ReplacesAggregateSettings()
    {
        var created = InputControlSettingsCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            FirstAuditDate,
            "Create settings",
            [
                new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.Global, null, null, 4, null, null, "#,##0.####", false)
            ]).Value!;
        var modified = InputControlSettingsModifiedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            SecondAuditDate,
            "Modify settings",
            [
                new InputControlSettingDefinition(InputControlKind.Money, InputControlSettingScope.Global, null, null, null, 0m, null, "#,##0.00", false)
            ]).Value!;

        var aggregate = new InputControlSettings(EventDate, [created, modified]);

        Assert.Single(aggregate.Items);
        Assert.Equal(InputControlKind.Money, aggregate.Items[0].ControlKind);
        Assert.Null(aggregate.Items[0].DecimalPlaces);
        Assert.Equal(modified.EventID, aggregate.LastEventID);
    }

    [Fact]
    public void CreatedBuilder_RejectsMoneyDecimalPlaceSettings()
    {
        var settings = new[]
        {
            new InputControlSettingDefinition(InputControlKind.Money, InputControlSettingScope.Global, null, null, 2, null, null, null, null)
        };

        var result = InputControlSettingsCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            FirstAuditDate,
            "Create settings",
            settings);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, error => error.Contains("Money input control settings must not specify DecimalPlaces", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolver_UsesAccountAbsoluteSettingsAndMostRestrictiveLimits()
    {
        var settings = SettingsFromDefinitions(
        [
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.User, null, UserID, 6, null, 100m, "#,##0.######", null),
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.Global, null, null, 4, 0.01m, 50m, "#,##0.####", true),
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.Account, AccountID, null, 2, 1m, null, "#,##0.##", false)
        ]);

        var policy = InputPolicyResolver.Resolve(InputControlKind.Quantity, settings, [], AccountID, UserID, null, null);

        Assert.Equal((short)2, policy.DecimalPlaces);
        Assert.Equal(1m, policy.MinValue);
        Assert.Equal(50m, policy.MaxValue);
        Assert.Equal("#,##0.##", policy.FormatPattern);
        Assert.Equal("Account", policy.FormatSource);
        Assert.False(policy.AllowNegative);
        Assert.Empty(policy.ValidationMessages);
    }

    [Fact]
    public void Resolver_IgnoresUserSettingsForOtherUsers()
    {
        var settings = SettingsFromDefinitions(
        [
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.User, null, OtherUserID, 1, null, null, "#,##0.#", null),
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.Global, null, null, 4, null, null, "#,##0.####", false)
        ]);

        var policy = InputPolicyResolver.Resolve(InputControlKind.Quantity, settings, [], null, UserID, null, null);

        Assert.Equal((short)4, policy.DecimalPlaces);
        Assert.Equal("#,##0.####", policy.FormatPattern);
        Assert.Equal("Global", policy.FormatSource);
    }

    [Fact]
    public void Resolver_AppliesCurrencyDecimalPlaceToMoney()
    {
        var settings = SettingsFromDefinitions(
        [
            new InputControlSettingDefinition(InputControlKind.Money, InputControlSettingScope.Global, null, null, null, 0m, null, "#,##0.00##", false)
        ]);
        var currency = new Currency(
            Alpha3Builder.Create("JPY"),
            392,
            0,
            "Yen",
            EventDate,
            FirstAuditDate,
            Guid.CreateGuid7());

        var policy = InputPolicyResolver.Resolve(InputControlKind.Money, settings, [currency], null, UserID, Alpha3Builder.Create("JPY"), null);

        Assert.Equal((short)0, policy.DecimalPlaces);
        Assert.Equal("JPY", policy.Currency);
        Assert.Equal("#,##0.00##", policy.FormatPattern);
        Assert.Equal("Global", policy.FormatSource);
    }

    [Fact]
    public void Resolver_ValidatesResolvedPolicyConflicts()
    {
        var settings = SettingsFromDefinitions(
        [
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.Global, null, null, null, 10m, null, null, null),
            new InputControlSettingDefinition(InputControlKind.Quantity, InputControlSettingScope.Account, AccountID, null, null, null, 5m, null, null)
        ]);

        var policy = InputPolicyResolver.Resolve(InputControlKind.Quantity, settings, [], AccountID, UserID, null, null);

        Assert.Contains("Resolved MinValue is greater than resolved MaxValue.", policy.ValidationMessages);
    }

    [Fact]
    public void ValidateValue_RejectsTooManyDecimalsAndNegativeMoney()
    {
        var moneyPolicy = new InputControlPolicy(InputControlKind.Money, 2, 0m, null, "#,##0.00", "Global", false, "GBP", []);

        var messages = InputPolicyResolver.ValidateValue("-1.234", moneyPolicy);

        Assert.Contains("Value must not be negative.", messages);
        Assert.Contains("Value can have at most 2 decimal places.", messages);
    }

    private static List<InputControlSetting> SettingsFromDefinitions(IReadOnlyList<InputControlSettingDefinition> definitions)
    {
        var source = InputControlSettingsCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            FirstAuditDate,
            "Create settings",
            definitions).Value!;

        return definitions.Select(definition => new InputControlSetting(definition, source)).ToList();
    }
}
