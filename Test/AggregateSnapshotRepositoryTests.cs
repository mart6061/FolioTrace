using Services;

namespace Test;

public sealed class AggregateSnapshotRepositoryTests
{
    private static readonly Guid StreamId = Guid.NewGuid();

    [Fact]
    public async Task SaveAsync_ThenFindLatestAsync_RoundTripsEveryField()
    {
        var repository = new FakeAggregateSnapshotRepository();
        var snapshot = CreateSnapshot(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        await repository.SaveAsync(snapshot);
        var found = await repository.FindLatestAsync(snapshot.AggregateKind, StreamId, snapshot.ValuationDateTime, snapshot.Variant);

        Assert.NotNull(found);
        Assert.Equal(snapshot, found);
    }

    [Fact]
    public async Task FindLatestAsync_ReturnsNull_WhenNoSnapshotExists()
    {
        var repository = new FakeAggregateSnapshotRepository();

        var found = await repository.FindLatestAsync("HoldingPositions", StreamId, DateTime.UtcNow);

        Assert.Null(found);
    }

    [Fact]
    public async Task FindLatestAsync_FindsTheNearestPrecedingSnapshot_WhenNoExactMatchExists()
    {
        var repository = new FakeAggregateSnapshotRepository();
        var earlier = CreateSnapshot(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var later = CreateSnapshot(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        await repository.SaveAsync(earlier);
        await repository.SaveAsync(later);

        var found = await repository.FindLatestAsync("HoldingPositions", StreamId, new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(earlier.Id, found?.Id);
    }

    [Fact]
    public async Task FindLatestAsync_NeverReturnsASnapshotAfterTheTargetDate()
    {
        var repository = new FakeAggregateSnapshotRepository();
        var future = CreateSnapshot(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));
        await repository.SaveAsync(future);

        var found = await repository.FindLatestAsync("HoldingPositions", StreamId, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Null(found);
    }

    [Fact]
    public async Task FindLatestAsync_PrefersTheNewestAsOfDateTime_ForTheSameValuationDate()
    {
        var repository = new FakeAggregateSnapshotRepository();
        var valuationDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var stale = CreateSnapshot(valuationDate, new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc));
        var fresh = CreateSnapshot(valuationDate, new DateTime(2025, 1, 1, 20, 0, 0, DateTimeKind.Utc));
        await repository.SaveAsync(stale);
        await repository.SaveAsync(fresh);

        var found = await repository.FindLatestAsync("HoldingPositions", StreamId, valuationDate);

        Assert.Equal(fresh.Id, found?.Id);
    }

    [Fact]
    public async Task FindLatestAsync_IgnoresSupersededSnapshots()
    {
        var repository = new FakeAggregateSnapshotRepository();
        var valuationDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await repository.SaveAsync(CreateSnapshot(valuationDate, valuationDate) with { Superseded = true });

        var found = await repository.FindLatestAsync("HoldingPositions", StreamId, valuationDate);

        Assert.Null(found);
    }

    [Fact]
    public async Task FindLatestAsync_KeepsAggregateKindStreamIdAndVariantIndependent()
    {
        var repository = new FakeAggregateSnapshotRepository();
        var valuationDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var eventBasis = CreateSnapshot(valuationDate, valuationDate) with { Variant = "EventDateTime" };
        var settlementBasis = CreateSnapshot(valuationDate, valuationDate) with { Variant = "SettlementDateTime" };
        await repository.SaveAsync(eventBasis);
        await repository.SaveAsync(settlementBasis);

        var found = await repository.FindLatestAsync("HoldingPositions", StreamId, valuationDate, "EventDateTime");

        Assert.Equal(eventBasis.Id, found?.Id);
    }

    [Fact]
    public async Task RetireFromAsync_SupersedesOnlySnapshotsAtOrAfterTheEventDate()
    {
        var repository = new FakeAggregateSnapshotRepository();
        var before = CreateSnapshot(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var atBoundary = CreateSnapshot(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var after = CreateSnapshot(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        await repository.SaveAsync(before);
        await repository.SaveAsync(atBoundary);
        await repository.SaveAsync(after);

        var retiredCount = await repository.RetireFromAsync("HoldingPositions", StreamId, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, retiredCount);
        Assert.False(repository.Snapshots.Single(snapshot => snapshot.Id == before.Id).Superseded);
        Assert.True(repository.Snapshots.Single(snapshot => snapshot.Id == atBoundary.Id).Superseded);
        Assert.True(repository.Snapshots.Single(snapshot => snapshot.Id == after.Id).Superseded);
    }

    private static AggregateSnapshot CreateSnapshot(DateTime valuationDateTime, DateTime asOfDateTime) =>
        new()
        {
            Id = Guid.NewGuid(),
            AggregateKind = "HoldingPositions",
            StreamId = StreamId,
            Variant = "",
            ValuationDateTime = valuationDateTime,
            AsOfDateTime = asOfDateTime,
            LastEventID = Guid.NewGuid(),
            LastAuditDateTime = asOfDateTime,
            PayloadJson = "[]",
            CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = 0
        };
}
