using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ValuationItem
{
    public required AccountID AccountID { get; init; }
    public required string AccountName { get; init; }
    public required HoldingID HoldingID { get; init; }
    public required string HoldingName { get; init; }
    public required string HoldingKind { get; init; }
    public required InstrumentID InstrumentID { get; init; }
    public required string InstrumentName { get; init; }
    public required string Name { get; init; }
    public required Alpha3 PriceCurrency { get; init; }
    public required Alpha3 ValuationCurrency { get; init; }
    public string? FXPair { get; init; }
    public string? FXDisplayPair { get; init; }
    public decimal? FXRate { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? LocalPrice { get; init; }
    public decimal? QuotePrice { get; init; }
    public decimal? BookValue { get; init; }
    public decimal? WeightPercent { get; init; }
    public required decimal BookCost { get; init; }
    public required bool Complete { get; init; }
    public string? IncompleteReason { get; init; }
}
