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

    /// <summary>
    /// Accrued interest per unit in the price currency, sitting alongside <see cref="LocalPrice"/>. Null for
    /// anything that does not accrue, so no row prints a meaningless zero under an equity.
    /// </summary>
    public decimal? LocalAccruedInterest { get; init; }

    /// <summary>
    /// The position's accrued interest in the valuation currency: per-unit accrued scaled by quantity and
    /// converted at the rate <see cref="QuotePrice"/> used. Carried whichever convention is in force, because
    /// the totals reconcile through it either way.
    /// </summary>
    public decimal? AccruedValue { get; init; }

    public decimal? BookValue { get; init; }
    public decimal? WeightPercent { get; init; }
    public required decimal BookCost { get; init; }
    public required bool Complete { get; init; }
    public string? IncompleteReason { get; init; }
}
