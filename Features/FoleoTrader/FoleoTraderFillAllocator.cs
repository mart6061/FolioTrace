using FolioTrace.Aggregates;

namespace FolioTrace.Aggregates;

public static class FoleoTraderFillAllocator
{
    public static bool IsWholeQuantity(decimal quantity) =>
        quantity > 0m && decimal.Truncate(quantity) == quantity;

    public static List<decimal> ProrateWholeQuantity(decimal total, IReadOnlyList<TicketProposalAllocation> proposalAllocations)
    {
        if (!IsWholeQuantity(total))
            throw new ArgumentException("Total quantity must be a positive whole number.", nameof(total));
        if (proposalAllocations.Count == 0)
            throw new ArgumentException("At least one proposal allocation is required.", nameof(proposalAllocations));

        var proposalTotal = proposalAllocations.Sum(allocation => allocation.Quantity);
        if (proposalTotal <= 0m)
            throw new ArgumentException("Proposal allocation total must be greater than zero.", nameof(proposalAllocations));

        var prorated = proposalAllocations
            .Select((allocation, index) =>
            {
                var exact = total * allocation.Quantity / proposalTotal;
                var whole = decimal.Floor(exact);
                return new WholeProration(index, whole, exact - whole);
            })
            .ToList();
        var remaining = total - prorated.Sum(item => item.Quantity);
        var remainderOrder = prorated
            .OrderByDescending(item => item.Remainder)
            .ThenBy(item => item.Index)
            .Select(item => item.Index)
            .ToList();

        for (var index = 0; remaining > 0m; index++, remaining--)
            prorated[remainderOrder[index]].Quantity++;

        return prorated
            .OrderBy(item => item.Index)
            .Select(item => item.Quantity)
            .ToList();
    }

    public static List<decimal> ProrateAmountByQuantities(decimal total, IReadOnlyList<decimal> quantities, int decimalScale)
    {
        var quantityTotal = quantities.Sum();
        if (quantityTotal <= 0m)
            throw new ArgumentException("Quantity total must be greater than zero.", nameof(quantities));

        var lastPositiveIndex = quantities
            .Select((quantity, index) => new { quantity, index })
            .Where(item => item.quantity > 0m)
            .Select(item => item.index)
            .Last();
        var values = new List<decimal>(quantities.Count);
        var allocated = 0m;

        for (var index = 0; index < quantities.Count; index++)
        {
            var value = quantities[index] <= 0m
                ? 0m
                : index == lastPositiveIndex
                    ? Round(total - allocated, decimalScale)
                    : Round(total * quantities[index] / quantityTotal, decimalScale);
            values.Add(value);
            allocated += value;
        }

        return values;
    }

    private static decimal Round(decimal value, int decimalScale) =>
        decimal.Round(value, decimalScale, MidpointRounding.AwayFromZero);

    private sealed record WholeProration(int Index, decimal Remainder)
    {
        public decimal Quantity { get; set; }

        public WholeProration(int index, decimal quantity, decimal remainder) : this(index, remainder)
        {
            Quantity = quantity;
        }
    }
}
