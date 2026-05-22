namespace FolioTrace.Aggregates;

public sealed record FXRatePointReadModel
{
    public required string Id { get; init; }

    public required string Pair { get; init; }

    public required decimal Bid { get; init; }

    public required decimal Mid { get; init; }

    public required decimal Ask { get; init; }

    public required DateTime ValidFrom { get; init; }

    public required DateTime ValidTo { get; init; }

    public required Guid LastEventID { get; init; }

    public required DateTime LastAuditDateTime { get; init; }
}
