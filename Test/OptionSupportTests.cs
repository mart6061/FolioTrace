using System.Text.Json;
using FolioTrace.Aggregates;
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
}
