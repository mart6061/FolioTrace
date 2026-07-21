using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

/// <summary>
/// Verifies HoldingPositions' seeded-reconstruction constructor (Aggregate-Snapshot-Scaling-Plan.md 3.3)
/// produces byte-identical results to a full from-scratch replay for the same inputs, per the plan's own
/// verify step. Backdated corrections that target a movement already folded into a snapshot's baseline are
/// deliberately NOT covered here - that's handled by 3.4's snapshot retirement (tested at the
/// HoldingPositionService level), not by this constructor, which has no way to know a baseline is stale.
/// </summary>
public sealed class HoldingPositionsSnapshotSeedingTests
{
    [Fact]
    public void SeededReconstruction_MatchesFullReplay_ForANewMovementOnAnExistingHolding()
    {
        var fixture = Fixture.Create(instrumentCount: 1);

        var baselineEvents = fixture.CreditTargetHolding(0, 10m, 250m);
        var deltaEvents = fixture.CreditTargetHolding(0, 5m, 100m);

        fixture.AssertSeededMatchesFullReplay(baselineEvents, deltaEvents);
    }

    [Fact]
    public void SeededReconstruction_MatchesFullReplay_WhenANewHoldingAppearsOnlyInTheDelta()
    {
        var fixture = Fixture.Create(instrumentCount: 2);

        var baselineEvents = fixture.CreditTargetHolding(0, 10m, 250m);
        var deltaEvents = fixture.CreditTargetHolding(1, 20m, 500m);

        fixture.AssertSeededMatchesFullReplay(baselineEvents, deltaEvents);
    }

    [Fact]
    public void SeededReconstruction_MatchesFullReplay_WhenADeltaMovementCancelsAnotherDeltaMovement()
    {
        var fixture = Fixture.Create(instrumentCount: 1);

        var baselineEvents = fixture.CreditTargetHolding(0, 10m, 250m);
        var toCancel = fixture.CreditTargetHolding(0, 5m, 100m);
        var cancellation = TransactionCancellationEventBuilder.Create(
            new TransactionCancellationRequest(Fixture.UserID, "Cancel in error", toCancel[0].EventSetID),
            baselineEvents.Cast<ITransactionEvent>().Concat(toCancel).ToList()).Value!;
        IReadOnlyList<ITransactionEvent> deltaEvents = [.. toCancel.Cast<ITransactionEvent>(), .. cancellation];

        fixture.AssertSeededMatchesFullReplay(baselineEvents, deltaEvents);
    }

    [Fact]
    public void SeededReconstruction_MatchesFullReplay_WhenADeltaMovementAdjustsAnotherDeltaMovement()
    {
        var fixture = Fixture.Create(instrumentCount: 1);

        var baselineEvents = fixture.CreditTargetHolding(0, 10m, 250m);
        var toAdjust = fixture.CreditTargetHolding(0, 5m, 100m);
        var adjustment = TransactionBookCostAdjustedEventBuilder.Create(
            new TransactionBookCostAdjustmentRequest(Fixture.UserID, "Correct book cost", toAdjust[0].EventSetID, new TransactionBookCost(999m), BookCostSource.SameCurrency, false),
            baselineEvents.Cast<ITransactionEvent>().Concat(toAdjust).ToList()).Value!;
        IReadOnlyList<ITransactionEvent> deltaEvents = [.. toAdjust.Cast<ITransactionEvent>(), adjustment];

        fixture.AssertSeededMatchesFullReplay(baselineEvents, deltaEvents);
    }

    private sealed class Fixture
    {
        public static readonly UserID UserID = new(Guid.CreateGuid7());
        private static readonly EventDateTime ValuationDate = EventDateTimeBuilder.Create(new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc));
        private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(ValuationDate.Value.AddMinutes(1));

        private readonly AccountID accountID;
        private readonly HoldingID fundingHoldingID;
        private readonly InstrumentID fundingInstrumentID;
        private readonly IReadOnlyList<HoldingID> targetHoldingIDs;
        private readonly IReadOnlyList<InstrumentID> targetInstrumentIDs;

        private Fixture(Holdings holdings, Accounts accounts, Instruments instruments, AccountID accountID, HoldingID fundingHoldingID, InstrumentID fundingInstrumentID, IReadOnlyList<HoldingID> targetHoldingIDs, IReadOnlyList<InstrumentID> targetInstrumentIDs)
        {
            Holdings = holdings;
            Accounts = accounts;
            Instruments = instruments;
            this.accountID = accountID;
            this.fundingHoldingID = fundingHoldingID;
            this.fundingInstrumentID = fundingInstrumentID;
            this.targetHoldingIDs = targetHoldingIDs;
            this.targetInstrumentIDs = targetInstrumentIDs;
        }

        public Holdings Holdings { get; }
        public Accounts Accounts { get; }
        public Instruments Instruments { get; }

        public static Fixture Create(int instrumentCount)
        {
            var accountID = AccountIDBuilder.Create();
            var accounts = CreateAccounts(accountID);

            var targetInstrumentIDs = Enumerable.Range(0, instrumentCount).Select(_ => InstrumentIDBuilder.Create()).ToList();
            var fundingInstrumentID = InstrumentIDBuilder.Create();
            var instruments = CreateInstruments([.. targetInstrumentIDs, fundingInstrumentID]);

            var targetHoldingIDs = targetInstrumentIDs.Select(_ => HoldingIDBuilder.Create()).ToList();
            var fundingHoldingID = HoldingIDBuilder.Create();

            var targetHoldingEvents = targetHoldingIDs.Zip(targetInstrumentIDs, (holdingID, instrumentID) => CreateAssetHolding(holdingID, accountID, instrumentID));
            var fundingHoldingEvent = CreateOutflowHolding(fundingHoldingID, accountID, fundingInstrumentID);
            var holdings = new Holdings(ValuationDate, AuditDate, targetHoldingEvents.Cast<IHoldingEvent>().Append(fundingHoldingEvent).ToList());

            return new Fixture(holdings, accounts, instruments, accountID, fundingHoldingID, fundingInstrumentID, targetHoldingIDs, targetInstrumentIDs);
        }

        public IReadOnlyList<ITransactionMovementEvent> CreditTargetHolding(int index, decimal quantity, decimal bookCost) =>
            TransactionBuilder.Create(
                new TransactionSetRequest(
                    UserID,
                    ValuationDate,
                    SettlementDateTimeBuilder.Create(ValuationDate.Value.AddDays(1)),
                    "Book holding",
                    [CreateLeg(targetHoldingIDs[index], targetInstrumentIDs[index], quantity, bookCost)],
                    [CreateLeg(fundingHoldingID, fundingInstrumentID, quantity, bookCost)]),
                Holdings).Value!.ToList();

        public void AssertSeededMatchesFullReplay(IReadOnlyList<ITransactionMovementEvent> baselineEvents, IReadOnlyList<ITransactionEvent> deltaEvents)
        {
            var snapshotAsOf = AuditDateTimeBuilder.Create();
            var snapshotSource = new HoldingPositions(ValuationDate, snapshotAsOf, Holdings, Accounts, Instruments, baselineEvents.Cast<ITransactionEvent>().ToList());

            var readAsOf = AuditDateTimeBuilder.Create();
            var fullReplay = new HoldingPositions(
                ValuationDate,
                readAsOf,
                Holdings,
                Accounts,
                Instruments,
                baselineEvents.Cast<ITransactionEvent>().Concat(deltaEvents).ToList());

            var baselineTotals = snapshotSource.Items.ToDictionary(
                item => item.HoldingID.Value,
                item => new HoldingPositions.HoldingPositionTotals(item.Quantity, item.BookCost, item.LastEventID, new AuditDateTime(item.LastAuditDateTime.Value)));

            var seeded = new HoldingPositions(
                ValuationDate,
                readAsOf,
                HoldingDateBasis.EventDateTime,
                Holdings,
                Accounts,
                Instruments,
                HoldingPositionFilter.Default,
                baselineTotals,
                snapshotSource.LastEventID,
                new AuditDateTime(snapshotSource.LastAuditDateTime.Value),
                deltaEvents);

            Assert.Equal(fullReplay.LastEventID, seeded.LastEventID);
            Assert.Equal(fullReplay.LastAuditDateTime, seeded.LastAuditDateTime);
            Assert.Equal(fullReplay.Items.Count, seeded.Items.Count);
            Assert.NotEmpty(fullReplay.Items);

            var fullByHolding = fullReplay.Items.ToDictionary(item => item.HoldingID.Value);
            foreach (var seededItem in seeded.Items)
            {
                var expected = fullByHolding[seededItem.HoldingID.Value];
                Assert.Equal(expected.Quantity, seededItem.Quantity);
                Assert.Equal(expected.BookCost, seededItem.BookCost);
                Assert.Equal(expected.LastEventID, seededItem.LastEventID);
                Assert.Equal(expected.LastAuditDateTime, seededItem.LastAuditDateTime);
            }
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

        private static Accounts CreateAccounts(AccountID accountID)
        {
            var created = AccountCreatedEventBuilder.CreateSeed(
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

            return new Accounts(ValuationDate, AuditDate, [created]);
        }

        private static Instruments CreateInstruments(IReadOnlyList<InstrumentID> instrumentIDs)
        {
            var events = instrumentIDs
                .Select((instrumentID, index) => InstrumentCreatedEventBuilder.CreateSeed(
                    CreateEventID(),
                    UserID,
                    ValuationDate,
                    AuditDate,
                    "Create instrument",
                    instrumentID,
                    $"Instrument {index}",
                    $"Instrument {index} plc",
                    ExchangeBuilder.Create("XLON"),
                    CFIBuilder.Create("ESVUFR"),
                    null,
                    true,
                    Alpha2Builder.Create("GB"),
                    Alpha2Builder.Create("GB"),
                    Alpha3Builder.Create("GBP")).Value!)
                .Cast<IInstrumentEvent>()
                .ToList();

            return new Instruments(ValuationDate, AuditDate, events);
        }

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
}
