using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountValuation
{
    public required AccountID AccountID { get; init; }
    public required string AccountName { get; init; }
    public required Alpha3 BookCurrency { get; init; }
    public required Alpha3 ValuationCurrency { get; init; }
    public required List<ValuationItem> Items { get; init; }
    public required ValuationTotals Totals { get; init; }
}
