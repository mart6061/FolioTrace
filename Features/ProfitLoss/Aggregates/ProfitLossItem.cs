using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ProfitLossItem
{
    public required AccountID AccountID { get; init; }

    public required string AccountName { get; init; }

    public required Alpha3 BookCurrency { get; init; }

    public required HoldingID HoldingID { get; init; }

    public required string HoldingName { get; init; }

    public required string HoldingKind { get; init; }

    public required InstrumentID InstrumentID { get; init; }

    public required string InstrumentName { get; init; }

    public required Alpha3 PriceCurrency { get; init; }

    public required decimal Quantity { get; init; }

    public decimal? LocalPrice { get; init; }

    public decimal? BookPrice { get; init; }

    public decimal? MarketValue { get; init; }

    public required decimal BookCost { get; init; }

    public required IReadOnlyList<ProfitLossMethodValue> Methods { get; init; }
}
