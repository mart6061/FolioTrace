using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Snapshots;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

/// <summary>
/// End-to-end coverage of HoldingPositionService's snapshot wiring (Aggregate-Snapshot-Scaling-Plan.md
/// 3.3/3.4/4.1): finding and using a persisted snapshot, falling back to a full rebuild when none exists, the
/// critical safety property - retiring a snapshot the moment a correction lands that could have invalidated
/// its baked-in totals, before any read can pick it up stale - and Phase 4's rollout-safety verification mode
/// catching a deliberately-wrong snapshot.
/// </summary>
public sealed class HoldingPositionServiceSnapshotTests
{
    [Fact]
    public async Task Get_UsesAPersistedSnapshot_WhenOneExistsForTheValuationDate()
    {
        var fixture = Fixture.Create();
        fixture.CreditTargetHolding(10m, 250m);
        var service = fixture.CreateService(out var snapshotRepository);

        var first = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);
        await service.PersistSnapshotAsync(first);
        service.InvalidateAll(); // force the next Get to go through BuildCurrent's snapshot lookup, not the in-memory cache

        Assert.NotEmpty(snapshotRepository.Snapshots);

        var second = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);

        Assert.Equal(first.Items.Single().Quantity, second.Items.Single().Quantity);
        Assert.Equal(first.Items.Single().BookCost, second.Items.Single().BookCost);
    }

    [Fact]
    public async Task Get_FallsBackToAFullRebuild_WhenNoSnapshotExists()
    {
        var fixture = Fixture.Create();
        fixture.CreditTargetHolding(10m, 250m);
        var service = fixture.CreateService(out _);

        var positions = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);

        Assert.Single(positions.Items);
        Assert.Equal(10m, positions.Items.Single().Quantity);
    }

    [Fact]
    public async Task Invalidate_RetiresSnapshots_ThatCouldHaveIncludedTheCorrectedMovement()
    {
        var fixture = Fixture.Create();
        fixture.CreditTargetHolding(10m, 250m);
        var service = fixture.CreateService(out var snapshotRepository);

        var positions = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);
        await service.PersistSnapshotAsync(positions);
        Assert.Contains(snapshotRepository.Snapshots, snapshot => !snapshot.Superseded);

        // A book-cost adjustment always carries the corrected movement's original EventDateTime (see
        // TransactionBookCostAdjustedEventBuilder), which is exactly what Invalidate uses here.
        service.Invalidate(new FakeTransactionEvent(Fixture.ValuationDate.Value));

        Assert.All(snapshotRepository.Snapshots, snapshot => Assert.True(snapshot.Superseded));

        var found = await snapshotRepository.FindLatestAsync("HoldingPositions", Constants.Initialisation.TransactionsStreamId, Fixture.ValuationDate.Value, HoldingDateBasis.EventDateTime.ToString());
        Assert.Null(found);
    }

    [Fact]
    public async Task Invalidate_DoesNotRetireSnapshotsBeforeTheEventDate()
    {
        var fixture = Fixture.Create();
        fixture.CreditTargetHolding(10m, 250m);
        var service = fixture.CreateService(out var snapshotRepository);

        var positions = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);
        await service.PersistSnapshotAsync(positions);

        // A correction dated strictly after this snapshot's valuation date cannot have affected it.
        service.Invalidate(new FakeTransactionEvent(Fixture.ValuationDate.Value.AddYears(1)));

        Assert.Contains(snapshotRepository.Snapshots, snapshot => !snapshot.Superseded);
    }

    [Fact]
    public async Task Get_VerificationMode_CatchesADeliberatelyWrongSnapshot()
    {
        // Simulates "a snapshot bug" per the plan's own verify step, by persisting a snapshot whose payload
        // doesn't match what a full replay would actually produce - i.e. exactly what a bug in the seeded-
        // reconstruction merge logic would look like from the outside.
        var fixture = Fixture.Create();
        fixture.CreditTargetHolding(10m, 250m);
        var service = fixture.CreateService(out var snapshotRepository, new HoldingPositionSnapshotVerificationOptions { Enabled = true, SampleRate = 1.0 });

        var correct = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);
        var wrongPayload = System.Text.Json.JsonSerializer.Serialize(new[]
        {
            new HoldingPositionSnapshotItem(fixture.TargetHoldingID.Value, 999m, 999m, correct.LastEventID.Value, correct.LastAuditDateTime.Value)
        });
        await snapshotRepository.SaveAsync(new AggregateSnapshot
        {
            Id = Guid.CreateGuid7(),
            AggregateKind = "HoldingPositions",
            StreamId = Constants.Initialisation.TransactionsStreamId,
            Variant = HoldingDateBasis.EventDateTime.ToString(),
            ValuationDateTime = Fixture.ValuationDate.Value,
            AsOfDateTime = correct.AsOfDateTime.Value,
            LastEventID = correct.LastEventID.Value,
            LastAuditDateTime = correct.LastAuditDateTime.Value,
            PayloadJson = wrongPayload,
            CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = 1
        });
        service.InvalidateAll();

        var seeded = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);

        // The wrong snapshot is still what gets served - verification observes without correcting.
        Assert.Equal(999m, seeded.Items.Single().Quantity);

        var diagnostics = service.GetDiagnostics();
        Assert.Equal(1, diagnostics.SnapshotVerifiedCount);
        Assert.Equal(1, diagnostics.SnapshotMismatchCount);
        Assert.NotNull(diagnostics.LastSnapshotMismatchAtUtc);
        Assert.Contains(fixture.TargetHoldingID.Value.ToString(), diagnostics.LastSnapshotMismatchDetails);
    }

    [Fact]
    public async Task Get_VerificationMode_FindsNoMismatch_ForACorrectSnapshot()
    {
        var fixture = Fixture.Create();
        fixture.CreditTargetHolding(10m, 250m);
        var service = fixture.CreateService(out _, new HoldingPositionSnapshotVerificationOptions { Enabled = true, SampleRate = 1.0 });

        var first = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);
        await service.PersistSnapshotAsync(first);
        service.InvalidateAll();

        await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);

        var diagnostics = service.GetDiagnostics();
        Assert.Equal(1, diagnostics.SnapshotVerifiedCount);
        Assert.Equal(0, diagnostics.SnapshotMismatchCount);
        Assert.Null(diagnostics.LastSnapshotMismatchAtUtc);
    }

    [Fact]
    public async Task Get_VerificationMode_NeverRuns_WhenDisabled()
    {
        var fixture = Fixture.Create();
        fixture.CreditTargetHolding(10m, 250m);
        var service = fixture.CreateService(out _, new HoldingPositionSnapshotVerificationOptions { Enabled = false, SampleRate = 1.0 });

        var first = await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);
        await service.PersistSnapshotAsync(first);
        service.InvalidateAll();

        await service.Get(Fixture.ValuationDate, HoldingDateBasis.EventDateTime);

        Assert.Equal(0, service.GetDiagnostics().SnapshotVerifiedCount);
    }

    private sealed class FakeTransactionEvent(DateTime eventDateTime) : IEventBase
    {
        public string Type => "FakeTransactionEvent";
        public EventID EventID { get; } = new(Guid.CreateGuid7());
        public UserID UserID { get; } = new(Guid.CreateGuid7());
        public AuditDateTime AuditDateTime { get; } = AuditDateTimeBuilder.Create();
        public EventDateTime EventDateTime { get; } = new(eventDateTime);
        public string Reason => "Test correction";
    }

    private sealed class Fixture
    {
        public static readonly UserID UserID = new(Guid.CreateGuid7());
        public static readonly EventDateTime ValuationDate = EventDateTimeBuilder.Create(new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc));
        private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(ValuationDate.Value.AddMinutes(1));

        private readonly AccountID accountID;
        private readonly HoldingID targetHoldingID;
        private readonly InstrumentID targetInstrumentID;
        private readonly HoldingID fundingHoldingID;
        private readonly InstrumentID fundingInstrumentID;
        private readonly List<IAuditEventBase> accountEvents;
        private readonly List<IAuditEventBase> instrumentEvents;
        private readonly List<IAuditEventBase> holdingEvents;
        private readonly List<IAuditEventBase> transactionEvents = [];
        private readonly Holdings holdingsForValidation;

        private Fixture(AccountID accountID, HoldingID targetHoldingID, InstrumentID targetInstrumentID, HoldingID fundingHoldingID, InstrumentID fundingInstrumentID, List<IAuditEventBase> accountEvents, List<IAuditEventBase> instrumentEvents, List<IAuditEventBase> holdingEvents, Holdings holdingsForValidation)
        {
            this.accountID = accountID;
            this.targetHoldingID = targetHoldingID;
            this.targetInstrumentID = targetInstrumentID;
            this.fundingHoldingID = fundingHoldingID;
            this.fundingInstrumentID = fundingInstrumentID;
            this.accountEvents = accountEvents;
            this.instrumentEvents = instrumentEvents;
            this.holdingEvents = holdingEvents;
            this.holdingsForValidation = holdingsForValidation;
        }

        public static Fixture Create()
        {
            var accountID = AccountIDBuilder.Create();
            var accountCreated = AccountCreatedEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                ValuationDate,
                AuditDate,
                "Create account",
                accountID,
                "General",
                "General Account",
                Alpha3Builder.Create("GBP"),
                true).Value!;
            var accountEvents = new List<IAuditEventBase> { accountCreated };

            var targetInstrumentID = InstrumentIDBuilder.Create();
            var fundingInstrumentID = InstrumentIDBuilder.Create();
            var instrumentEvents = new List<IAuditEventBase>
            {
                CreateInstrument(targetInstrumentID, "Target"),
                CreateInstrument(fundingInstrumentID, "Funding")
            };

            var targetHoldingID = HoldingIDBuilder.Create();
            var fundingHoldingID = HoldingIDBuilder.Create();
            var targetHoldingEvent = CreateAssetHolding(targetHoldingID, accountID, targetInstrumentID);
            var fundingHoldingEvent = CreateOutflowHolding(fundingHoldingID, accountID, fundingInstrumentID);
            var holdingEvents = new List<IAuditEventBase> { targetHoldingEvent, fundingHoldingEvent };

            var holdingsForValidation = new Holdings(ValuationDate, AuditDate, new List<IHoldingEvent> { targetHoldingEvent, fundingHoldingEvent });

            return new Fixture(accountID, targetHoldingID, targetInstrumentID, fundingHoldingID, fundingInstrumentID, accountEvents, instrumentEvents, holdingEvents, holdingsForValidation);
        }

        public HoldingID TargetHoldingID => targetHoldingID;

        public HoldingPositionService CreateService(out FakeAggregateSnapshotRepository snapshotRepository) =>
            CreateService(out snapshotRepository, verificationOptions: null);

        public HoldingPositionService CreateService(out FakeAggregateSnapshotRepository snapshotRepository, HoldingPositionSnapshotVerificationOptions? verificationOptions)
        {
            snapshotRepository = new FakeAggregateSnapshotRepository();
            var eventRepository = new FakeEventRepository(accountEvents, instrumentEvents, holdingEvents, transactionEvents);
            var holdingService = new HoldingService(eventRepository);
            var accountService = new AccountService(eventRepository);
            var instrumentService = new InstrumentService(eventRepository);
            return new HoldingPositionService(eventRepository, holdingService, accountService, instrumentService, snapshotRepository, verificationOptions: verificationOptions);
        }

        public void CreditTargetHolding(decimal quantity, decimal bookCost)
        {
            var events = TransactionBuilder.Create(
                new TransactionSetRequest(
                    UserID,
                    ValuationDate,
                    SettlementDateTimeBuilder.Create(ValuationDate.Value.AddDays(1)),
                    "Book holding",
                    [CreateLeg(targetHoldingID, targetInstrumentID, quantity, bookCost)],
                    [CreateLeg(fundingHoldingID, fundingInstrumentID, quantity, bookCost)]),
                holdingsForValidation).Value!;

            transactionEvents.AddRange(events);
        }

        private TransactionRequest CreateLeg(HoldingID holdingID, InstrumentID instrumentID, decimal quantity, decimal bookCost) =>
            new(
                holdingID,
                instrumentID,
                accountID,
                new TransactionQuantity(quantity),
                new TransactionLocalCost(bookCost),
                Alpha3Builder.Create("GBP"),
                new TransactionBookCost(bookCost),
                BookCostSource.SameCurrency,
                false);

        private static InstrumentCreatedEvent CreateInstrument(InstrumentID instrumentID, string name) =>
            InstrumentCreatedEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                ValuationDate,
                AuditDate,
                "Create instrument",
                instrumentID,
                name,
                $"{name} plc",
                ExchangeBuilder.Create("XLON"),
                CFIBuilder.Create("ESVUFR"),
                null,
                true,
                Alpha2Builder.Create("GB"),
                Alpha2Builder.Create("GB"),
                Alpha3Builder.Create("GBP")).Value!;

        private static HoldingPositionAssetCreatedEvent CreateAssetHolding(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID) =>
            HoldingPositionAssetCreatedEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                ValuationDate,
                AuditDate,
                "Create holding",
                holdingID,
                accountID,
                instrumentID,
                "Position",
                true,
                false).Value!;

        private static HoldingNominalOutflowCreatedEvent CreateOutflowHolding(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID) =>
            HoldingNominalOutflowCreatedEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                ValuationDate,
                AuditDate,
                "Create outflow",
                holdingID,
                accountID,
                instrumentID,
                "Outflow",
                true,
                false).Value!;

        private static EventID CreateEventID() => new(Guid.CreateGuid7());
    }

    private sealed class FakeEventRepository(
        List<IAuditEventBase> accountEvents,
        List<IAuditEventBase> instrumentEvents,
        List<IAuditEventBase> holdingEvents,
        List<IAuditEventBase> transactionEvents) : IEventRepository
    {
        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, 4, 0, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IAuditEventBase =>
            Task.FromResult(AllEvents().OfType<T>().SingleOrDefault(@event => @event.EventID == eventId));

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(StreamFor(streamId).LastOrDefault()?.EventID);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(StreamFor(streamId).LastOrDefault()?.EventID);

        public Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IAuditEventBase>>(StreamFor(streamId));

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IAuditEventBase =>
            Task.FromResult<IReadOnlyList<TEvent>>(StreamFor(streamId).OfType<TEvent>().ToList());

        public Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
            where TAggregate : class
            where TEvent : class, IAuditEventBase =>
            Task.CompletedTask;

        public Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IAuditEventBase
        {
            transactionEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task AppendAsync(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken = default)
        {
            transactionEvents.AddRange(events);
            return Task.CompletedTask;
        }

        private IEnumerable<IAuditEventBase> AllEvents() =>
            accountEvents.Concat(instrumentEvents).Concat(holdingEvents).Concat(transactionEvents);

        private List<IAuditEventBase> StreamFor(Guid streamId)
        {
            if (streamId == Constants.Initialisation.TransactionsStreamId)
                return transactionEvents;
            if (streamId == Constants.Initialisation.HoldingsStreamId)
                return holdingEvents;
            if (streamId == Constants.Initialisation.AccountsStreamId)
                return accountEvents;
            if (streamId == Constants.Initialisation.InstrumentsStreamId)
                return instrumentEvents;

            return [];
        }
    }
}
