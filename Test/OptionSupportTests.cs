using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;

namespace Test;

public sealed class OptionSupportTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void OptionTermsRoundTripThroughPolymorphicContract()
    {
        IInstrumentTerms original = new InstrumentTermsOption(
            OptionType.Call,
            new InstrumentID(Guid.NewGuid()),
            new Money(125.5m, new Alpha3("USD")),
            new InstrumentDate(new DateOnly(2027, 3, 19)),
            OptionExerciseStyle.American,
            OptionSettlementType.Physical,
            new ContractMultiplier(100m));

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var terms = Assert.IsType<InstrumentTermsOption>(JsonSerializer.Deserialize<IInstrumentTerms>(json, JsonOptions));

        Assert.Equal(original, terms);
        Assert.Contains("InstrumentTermsOption", json);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ContractMultiplierMustBePositive(decimal value)
    {
        Assert.Throws<ArgumentException>(() => new ContractMultiplier(value));
    }

    [Fact]
    public void OptionHoldingRoundTripsAndCreatedEventIsRegistered()
    {
        var eventDateTime = EventDateTimeBuilder.Create(new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc));
        var auditDateTime = AuditDateTimeBuilder.Create(eventDateTime.Value.AddMinutes(1));
        var eventID = new EventID(Guid.CreateGuid7());
        var userID = new UserID(Guid.CreateGuid7());
        var holdingID = HoldingIDBuilder.Create();
        var accountID = AccountIDBuilder.Create();
        var instrumentID = InstrumentIDBuilder.Create();
        var createdEvent = HoldingPositionOptionCreatedEventBuilder.CreateSeed(
            eventID,
            userID,
            eventDateTime,
            auditDateTime,
            "Create option holding",
            holdingID,
            accountID,
            instrumentID,
            "Call position",
            true,
            false).Value!;
        HoldingBase holding = new HoldingPositionOption(
            holdingID,
            accountID,
            instrumentID,
            "Call position",
            true,
            false,
            eventDateTime,
            auditDateTime,
            eventID,
            auditDateTime);

        var holdingJson = JsonSerializer.Serialize(holding, JsonOptions);
        var eventBaseRegistrations = typeof(EventBase).GetCustomAttributes(typeof(JsonDerivedTypeAttribute), inherit: false).Cast<JsonDerivedTypeAttribute>();
        var interfaceRegistrations = typeof(IEventBase).GetCustomAttributes(typeof(JsonDerivedTypeAttribute), inherit: false).Cast<JsonDerivedTypeAttribute>();

        Assert.IsType<HoldingPositionOption>(JsonSerializer.Deserialize<HoldingBase>(holdingJson, JsonOptions));
        Assert.Contains(eventBaseRegistrations, registration => registration.DerivedType == typeof(HoldingPositionOptionCreatedEvent));
        Assert.Contains(interfaceRegistrations, registration => registration.DerivedType == typeof(HoldingPositionOptionCreatedEvent));
        Assert.Equal(nameof(HoldingPositionOptionCreatedEvent), createdEvent.Type);
        Assert.Contains(nameof(HoldingPositionOption), holdingJson);
    }
}
