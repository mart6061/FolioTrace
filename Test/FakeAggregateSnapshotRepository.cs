using Services;

namespace Test;

/// <summary>
/// In-memory stand-in for MartenAggregateSnapshotRepository, mirroring its query/retire semantics exactly so
/// tests exercise the same contract the real Marten-backed implementation is expected to satisfy. Shared
/// across multiple Phase 3 test files (HoldingPositionService seeding, snapshot invalidation, cadence) rather
/// than duplicated per-file, since the "find nearest preceding non-superseded, newest AsOfDateTime on ties"
/// logic is non-trivial and would risk drifting if reimplemented independently in each.
/// </summary>
public sealed class FakeAggregateSnapshotRepository : IAggregateSnapshotRepository
{
    private readonly List<AggregateSnapshot> snapshots = [];

    public IReadOnlyList<AggregateSnapshot> Snapshots => snapshots;

    public Task SaveAsync(AggregateSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        snapshots.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task<AggregateSnapshot?> FindLatestAsync(string aggregateKind, Guid streamId, DateTime valuationDateTime, string variant = "", CancellationToken cancellationToken = default)
    {
        var match = snapshots
            .Where(snapshot =>
                snapshot.AggregateKind == aggregateKind
                && snapshot.StreamId == streamId
                && snapshot.Variant == variant
                && snapshot.ValuationDateTime <= valuationDateTime
                && !snapshot.Superseded)
            .OrderByDescending(snapshot => snapshot.ValuationDateTime)
            .ThenByDescending(snapshot => snapshot.AsOfDateTime)
            .FirstOrDefault();

        return Task.FromResult(match);
    }

    public Task<int> RetireFromAsync(string aggregateKind, Guid streamId, DateTime eventDateTime, string variant = "", CancellationToken cancellationToken = default)
    {
        var affected = snapshots
            .Where(snapshot =>
                snapshot.AggregateKind == aggregateKind
                && snapshot.StreamId == streamId
                && snapshot.Variant == variant
                && snapshot.ValuationDateTime >= eventDateTime
                && !snapshot.Superseded)
            .ToList();

        for (var index = 0; index < snapshots.Count; index++)
        {
            if (affected.Contains(snapshots[index]))
                snapshots[index] = snapshots[index] with { Superseded = true };
        }

        return Task.FromResult(affected.Count);
    }
}
