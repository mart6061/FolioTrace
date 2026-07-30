using System.Text.Json;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class InstrumentPriceJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserialize_LegacyEquityPrice_PreservesQuoteAndNav()
    {
        const string json =
            """
            {
              "$type": "InstrumentPriceEquity",
              "bid": { "amount": 99 },
              "mid": { "amount": 100 },
              "ask": { "amount": 101 },
              "nav": { "amount": 102 }
            }
            """;

        var price = Assert.IsType<InstrumentPriceEquity>(
            JsonSerializer.Deserialize<IInstrumentPrice>(json, JsonOptions));

        Assert.Equal(99m, price.Quote.Bid.Amount);
        Assert.Equal(100m, price.Quote.Mid.Amount);
        Assert.Equal(101m, price.Quote.Ask.Amount);
        Assert.Null(price.Last.Amount);
        Assert.Equal(102m, price.Nav.Amount);
    }

    [Fact]
    public void Deserialize_LegacyFixedIncomePrice_ExpandsCleanPriceToAFlatQuote()
    {
        const string json =
            """
            {
              "$type": "InstrumentPriceFixedIncome",
              "cleanPrice": { "amount": 98.5 }
            }
            """;

        var price = Assert.IsType<InstrumentPriceFixedIncome>(
            JsonSerializer.Deserialize<IInstrumentPrice>(json, JsonOptions));

        Assert.Equal(98.5m, price.CleanQuote.Bid.Amount);
        Assert.Equal(98.5m, price.CleanQuote.Mid.Amount);
        Assert.Equal(98.5m, price.CleanQuote.Ask.Amount);
    }

    [Fact]
    public void SerializeAndDeserialize_CurrentEquityPrice_PreservesCurrentShape()
    {
        IInstrumentPrice original = new InstrumentPriceEquity(
            new InstrumentQuote(new InstrumentPrice(99m), new InstrumentPrice(100m), new InstrumentPrice(101m)),
            new InstrumentPrice(100.5m),
            new InstrumentPrice(102m));

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var price = Assert.IsType<InstrumentPriceEquity>(
            JsonSerializer.Deserialize<IInstrumentPrice>(json, JsonOptions));
        using var document = JsonDocument.Parse(json);

        Assert.Equal(original, price);
        Assert.True(document.RootElement.TryGetProperty("quote", out _));
        Assert.False(document.RootElement.TryGetProperty("bid", out _));
    }
}
