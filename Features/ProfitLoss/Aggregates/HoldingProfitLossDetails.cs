using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingProfitLossDetails
{
    public required AccountID AccountID { get; init; }

    public required Alpha3 Currency { get; init; }

    public required HoldingID HoldingID { get; init; }

    public required string HoldingName { get; init; }

    public required string InstrumentName { get; init; }

    public required ProfitLossMethod DefaultMethod { get; init; }

    public required IReadOnlyList<ProfitLossMethodValue> Methods { get; init; }

    public required IReadOnlyList<ProfitLossMovement> Rows { get; init; }
}
