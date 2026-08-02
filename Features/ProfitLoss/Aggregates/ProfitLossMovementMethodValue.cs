namespace FolioTrace.Aggregates;

public sealed record ProfitLossMovementMethodValue
{
    public required ProfitLossMethod Method { get; init; }

    public decimal? RealizedPnL { get; init; }
}
