using FolioTrace.Aggregates;
using Repository;

namespace Test;

public sealed class SeedRepositoryTests
{
    [Fact]
    public void SeedData_SetsBookCostFromLocalCostAndFxWhenRequired()
    {
        var events = SeedRepository.CreateInitialTransactionEvents()
            .OfType<ITransactionMovementEvent>()
            .ToList();

        Assert.Contains(events, @event =>
            @event.LocalCostCurrency.Value == "GBP" &&
            @event.BookCostSource == BookCostSource.SameCurrency &&
            !@event.BookCostEstimated &&
            @event.BookCost.Value == @event.LocalCost.Value);

        Assert.Contains(events, @event =>
            @event.LocalCostCurrency.Value != "GBP" &&
            @event.BookCostSource == BookCostSource.FxEstimate &&
            @event.BookCostEstimated &&
            @event.BookCost.Value != @event.LocalCost.Value);
    }
}
