using API.FoleoTrader;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class FoleoTraderFillAllocatorTests
{
    [Fact]
    public void ProrateWholeQuantity_PreservesWholeTotalWithLargestRemainders()
    {
        var allocations = new[]
        {
            new TicketProposalAllocation(AccountIDBuilder.Create(), 50m),
            new TicketProposalAllocation(AccountIDBuilder.Create(), 30m),
            new TicketProposalAllocation(AccountIDBuilder.Create(), 20m)
        };

        var quantities = FoleoTraderFillAllocator.ProrateWholeQuantity(7m, allocations);

        Assert.Equal([4m, 2m, 1m], quantities);
        Assert.Equal(7m, quantities.Sum());
        Assert.All(quantities, quantity => Assert.Equal(decimal.Truncate(quantity), quantity));
    }

    [Fact]
    public void ProrateWholeQuantity_AllowsSmallerFillThanAccountCount()
    {
        var allocations = new[]
        {
            new TicketProposalAllocation(AccountIDBuilder.Create(), 1m),
            new TicketProposalAllocation(AccountIDBuilder.Create(), 1m),
            new TicketProposalAllocation(AccountIDBuilder.Create(), 1m)
        };

        var quantities = FoleoTraderFillAllocator.ProrateWholeQuantity(2m, allocations);

        Assert.Equal([1m, 1m, 0m], quantities);
        Assert.Equal(2m, quantities.Sum());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.5)]
    public void IsWholeQuantity_RejectsNonPositiveOrFractionalValues(decimal quantity)
    {
        Assert.False(FoleoTraderFillAllocator.IsWholeQuantity(quantity));
    }

    [Fact]
    public void ProrateAmountByQuantities_PutsResidualOnLastPositiveQuantity()
    {
        var values = FoleoTraderFillAllocator.ProrateAmountByQuantities(100m, [3m, 0m, 2m], 2);

        Assert.Equal([60m, 0m, 40m], values);
        Assert.Equal(100m, values.Sum());
    }
}
