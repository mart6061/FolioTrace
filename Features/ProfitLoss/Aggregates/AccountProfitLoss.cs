using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountProfitLoss
{
    public required AccountID AccountID { get; init; }

    public required string AccountName { get; init; }

    public required Alpha3 BookCurrency { get; init; }

    public required IReadOnlyList<ProfitLossItem> Items { get; init; }

    public required IReadOnlyList<ProfitLossMethodValue> Totals { get; init; }
}
