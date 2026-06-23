namespace FolioTrace.Aggregates;

public sealed record ProfitLossMethodValue
{
    public required ProfitLossMethod Method { get; init; }

    public required decimal RealizedPnL { get; init; }

    public required decimal BookValue { get; init; }

    public decimal? UnrealizedPnL { get; init; }

    public decimal? TotalPnL { get; init; }

    public required bool Complete { get; init; }

    public string? IncompleteReason { get; init; }
}
