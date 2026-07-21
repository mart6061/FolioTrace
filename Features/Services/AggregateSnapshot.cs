namespace Services;

/// <summary>
/// A persisted, point-in-time materialization of an aggregate, used to avoid replaying an aggregate's full
/// event history on every rebuild. "Current" snapshots are looked up as "latest available for this valuation
/// date," never an exact match on AsOfDateTime - a given (AggregateKind, StreamId, Variant, ValuationDateTime)
/// combination can have multiple rows over time as it gets rebuilt and re-persisted; only the newest
/// non-superseded one is valid. Snapshots are immutable once written - a bitemporal correction retires
/// (Superseded = true) affected rows rather than mutating them.
/// </summary>
public sealed record AggregateSnapshot
{
    public required Guid Id { get; init; }

    /// <summary>Discriminator matching the aggregate's FeatureAggregateAttribute/diagnostics naming, e.g. "HoldingPositions".</summary>
    public required string AggregateKind { get; init; }

    public required Guid StreamId { get; init; }

    /// <summary>Extra key dimension beyond StreamId, e.g. HoldingPositions' HoldingDateBasis. Empty for aggregates with no such dimension.</summary>
    public string Variant { get; init; } = "";

    public required DateTime ValuationDateTime { get; init; }

    /// <summary>The audit cutoff this snapshot was materialized as of - used to pick the newest of several rows for the same ValuationDateTime, not as a lookup filter.</summary>
    public required DateTime AsOfDateTime { get; init; }

    public required Guid LastEventID { get; init; }

    public required DateTime LastAuditDateTime { get; init; }

    public required string PayloadJson { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required int SourceEventCount { get; init; }

    public bool Superseded { get; init; }
}
