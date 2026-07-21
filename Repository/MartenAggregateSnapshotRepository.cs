using Marten;
using FolioTrace.Snapshots;
using Services;

namespace Repository;

public sealed class MartenAggregateSnapshotRepository(IDocumentStore store) : IAggregateSnapshotRepository
{
    public async Task SaveAsync(AggregateSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));

        await using var session = store.LightweightSession();
        session.Store(snapshot);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<AggregateSnapshot?> FindLatestAsync(string aggregateKind, Guid streamId, DateTime valuationDateTime, string variant = "", CancellationToken cancellationToken = default)
    {
        var databaseValuationDateTime = DateTime.SpecifyKind(valuationDateTime, DateTimeKind.Unspecified);
        await using var session = store.QuerySession();

        return await session.Query<AggregateSnapshot>()
            .Where(snapshot =>
                snapshot.AggregateKind == aggregateKind
                && snapshot.StreamId == streamId
                && snapshot.Variant == variant
                && snapshot.ValuationDateTime <= databaseValuationDateTime
                && !snapshot.Superseded)
            .OrderByDescending(snapshot => snapshot.ValuationDateTime)
            .ThenByDescending(snapshot => snapshot.AsOfDateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> RetireFromAsync(string aggregateKind, Guid streamId, DateTime eventDateTime, string variant = "", CancellationToken cancellationToken = default)
    {
        var databaseEventDateTime = DateTime.SpecifyKind(eventDateTime, DateTimeKind.Unspecified);
        await using var session = store.LightweightSession();

        var affected = await session.Query<AggregateSnapshot>()
            .Where(snapshot =>
                snapshot.AggregateKind == aggregateKind
                && snapshot.StreamId == streamId
                && snapshot.Variant == variant
                && snapshot.ValuationDateTime >= databaseEventDateTime
                && !snapshot.Superseded)
            .ToListAsync(cancellationToken);

        if (affected.Count == 0)
            return 0;

        foreach (var snapshot in affected)
            session.Store(snapshot with { Superseded = true });

        await session.SaveChangesAsync(cancellationToken);
        return affected.Count;
    }
}
