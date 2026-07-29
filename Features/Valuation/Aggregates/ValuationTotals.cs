namespace FolioTrace.Aggregates;

/// <summary>
/// Portfolio totals. <paramref name="BookValue"/> is the final total and always includes accrued interest,
/// whichever convention is displayed; <paramref name="CleanValue"/> is derived from it rather than summed
/// separately, so the clean subtotal plus <paramref name="AccruedValue"/> can never disagree with it.
/// </summary>
public sealed record ValuationTotals(decimal BookValue, decimal CleanValue, decimal AccruedValue, decimal BookCost, int IncompleteCount);
