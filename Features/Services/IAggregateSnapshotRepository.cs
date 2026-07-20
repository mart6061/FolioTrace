namespace Services;

public interface IAggregateSnapshotRepository
{
    Task SaveAsync(AggregateSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Finds the newest non-superseded snapshot for the given key whose ValuationDateTime is at or before the target, i.e. "nearest preceding valid snapshot."</summary>
    Task<AggregateSnapshot?> FindLatestAsync(string aggregateKind, Guid streamId, DateTime valuationDateTime, string variant = "", CancellationToken cancellationToken = default);

    /// <summary>Marks superseded every non-retired snapshot for the given stream/variant whose ValuationDateTime is at or after eventDateTime - mirrors the in-memory cache's InvalidateFrom condition exactly.</summary>
    Task<int> RetireFromAsync(string aggregateKind, Guid streamId, DateTime eventDateTime, string variant = "", CancellationToken cancellationToken = default);
}
