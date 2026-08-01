using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class InstrumentQuoteTests
{
    private static InstrumentQuote Quote(decimal? bid, decimal? mid, decimal? ask) =>
        new(new InstrumentPrice(bid), new InstrumentPrice(mid), new InstrumentPrice(ask));

    [Fact]
    public void Quote_RejectsOutOfOrderPrices()
    {
        Assert.Throws<ArgumentException>(() => Quote(101m, 100m, 102m));
        Assert.Throws<ArgumentException>(() => Quote(99m, 103m, 102m));
    }

    [Fact]
    public void Quote_AllowsPartialCombinations()
    {
        // An instrument quoted on one side only must still validate.
        Assert.Null(Quote(null, 100m, null).Bid.Amount);
        Assert.Null(Quote(99m, null, 101m).Mid.Amount);
        Assert.Null(Quote(null, null, null).Ask.Amount);
    }

    [Fact]
    public void Equity_AllowsLastOutsideTheSpread()
    {
        // A last trade can sit outside the current spread when stale or executed through the touch.
        var equity = new InstrumentPriceEquity(Quote(99m, 100m, 101m), new InstrumentPrice(150m), new InstrumentPrice(100m));

        Assert.Equal(150m, equity.Select(InstrumentPriceBasis.Last).Amount);
    }

    [Fact]
    public void Equity_SelectsByBasis()
    {
        var equity = new InstrumentPriceEquity(Quote(99m, 100m, 101m), new InstrumentPrice(98m), new InstrumentPrice(102m));

        Assert.Equal(99m, equity.Select(InstrumentPriceBasis.Bid).Amount);
        Assert.Equal(100m, equity.Select(InstrumentPriceBasis.Mid).Amount);
        Assert.Equal(101m, equity.Select(InstrumentPriceBasis.Ask).Amount);
        Assert.Equal(98m, equity.Select(InstrumentPriceBasis.Last).Amount);
        Assert.Equal(102m, equity.Select(InstrumentPriceBasis.NAV).Amount);
    }

    [Fact]
    public void Option_SelectsQuoteLastAndFallsBackFromNavToMid()
    {
        var option = new InstrumentPriceOption(Quote(4.9m, 5m, 5.1m), new InstrumentPrice(4.75m));

        Assert.Equal(4.9m, option.Select(InstrumentPriceBasis.Bid).Amount);
        Assert.Equal(5m, option.Select(InstrumentPriceBasis.Mid).Amount);
        Assert.Equal(5.1m, option.Select(InstrumentPriceBasis.Ask).Amount);
        Assert.Equal(4.75m, option.Select(InstrumentPriceBasis.Last).Amount);
        Assert.Equal(5m, option.Select(InstrumentPriceBasis.NAV).Amount);
    }

    [Fact]
    public void Add_DerivesDirtyQuoteAndPreservesOrdering()
    {
        var dirty = Quote(99m, 100m, 101m).Add(new InstrumentPrice(1.25m));

        Assert.Equal(100.25m, dirty.Bid.Amount);
        Assert.Equal(101.25m, dirty.Mid.Amount);
        Assert.Equal(102.25m, dirty.Ask.Amount);
        Assert.True(dirty.Bid.Amount <= dirty.Mid.Amount && dirty.Mid.Amount <= dirty.Ask.Amount);
    }

    [Fact]
    public void Add_LeavesAbsentQuotesAbsent()
    {
        // Accrued interest must not conjure a dirty price where no clean price was quoted.
        var dirty = Quote(null, 100m, null).Add(new InstrumentPrice(1.25m));

        Assert.Null(dirty.Bid.Amount);
        Assert.Equal(101.25m, dirty.Mid.Amount);
        Assert.Null(dirty.Ask.Amount);
    }
}
