using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ProfitLossMovement
{
    public required EventID EventID { get; init; }

    public required string TransactionType { get; init; }

    public required DateTime DisplayDateTime { get; init; }

    public required decimal Quantity { get; init; }

    public required decimal BookCost { get; init; }

    public required IReadOnlyList<ProfitLossMovementMethodValue> Methods { get; init; }
}
