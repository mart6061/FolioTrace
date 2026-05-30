using System.Text.Json;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class TransactionBuilderTests
{
    [Fact]
    public void Create_BalancedTwoLegSet_EmitsCreditAndDebitWithSharedGroup()
    {
        var request = CreateRequest(
            credits: [CreateLeg(10m)],
            debits: [CreateLeg(10m)]);

        var result = TransactionBuilder.Create(request, CreateHoldings());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        var events = result.Value!;
        Assert.Equal(2, events.Count);
        Assert.IsType<TransactionCreditEvent>(events[0]);
        Assert.IsType<TransactionDebitEvent>(events[1]);

        var expectedEventIds = EventIds(events);
        Assert.Single(events.Select(@event => @event.EventSetID.Value).Distinct());
        Assert.All(events, @event => Assert.Equal(expectedEventIds, EventIds(@event.EventIDGroup)));
        Assert.All(events, @event => Assert.Equal(request.EventDateTime, @event.EventDateTime));
        Assert.All(events, @event => Assert.Equal(request.SettlementDateTime, @event.SettlementDateTime));
    }

    [Fact]
    public void Create_BalancedMultiLegSet_EmitsCompleteEventGroups()
    {
        var request = CreateRequest(
            credits: [CreateLeg(4m), CreateLeg(6m)],
            debits: [CreateLeg(10m)]);

        var result = TransactionBuilder.Create(request, CreateHoldings());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        var events = result.Value!;
        var expectedEventIds = EventIds(events);
        Assert.Equal(3, events.Count);
        Assert.Equal(2, events.OfType<TransactionCreditEvent>().Count());
        Assert.Single(events.OfType<TransactionDebitEvent>());
        Assert.All(events, @event => Assert.Equal(expectedEventIds, EventIds(@event.EventIDGroup)));
    }

    [Fact]
    public void Create_RejectsSetsWithoutBothSides()
    {
        var result = TransactionBuilder.Create(CreateRequest(
            credits: [CreateLeg(10m)],
            debits: []), CreateHoldings());

        Assert.False(result.IsValid);
        Assert.Contains("At least one debit is required.", result.ValidationErrors);
    }

    [Fact]
    public void Create_RejectsUnbalancedBookCost()
    {
        var result = TransactionBuilder.Create(CreateRequest(
            credits: [CreateLeg(10m)],
            debits: [CreateLeg(9m)]), CreateHoldings());

        Assert.False(result.IsValid);
        Assert.Contains("Transaction book cost must balance: credits less debits must equal zero.", result.ValidationErrors);
    }

    [Fact]
    public void Create_RejectsMissingLegData()
    {
        var result = TransactionBuilder.Create(CreateRequest(
            credits: [new TransactionRequest(HoldingID, InstrumentID, AccountID, null!, new TransactionBookCost(10m))],
            debits: [CreateLeg(10m)]), CreateHoldings());

        Assert.False(result.IsValid);
        Assert.Contains("Credit 1 Quantity is required.", result.ValidationErrors);
    }

    [Fact]
    public void Create_RejectsMissingSettlementDate()
    {
        var request = CreateRequest(
            credits: [CreateLeg(10m)],
            debits: [CreateLeg(10m)]) with
        {
            SettlementDateTime = null!
        };

        var result = TransactionBuilder.Create(request, CreateHoldings());

        Assert.False(result.IsValid);
        Assert.Contains("SettlementDateTime is required.", result.ValidationErrors);
    }

    [Fact]
    public void Create_RejectsSettlementDateBeforeEventDate()
    {
        var eventDateTime = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10));
        var request = CreateRequest(eventDateTime, credits: [CreateLeg(10m)], debits: [CreateLeg(10m)]) with
        {
            SettlementDateTime = SettlementDateTimeBuilder.Create(eventDateTime.Value.AddTicks(-1))
        };

        var result = TransactionBuilder.Create(request, CreateHoldings());

        Assert.False(result.IsValid);
        Assert.Contains("SettlementDateTime must be equal to or greater than EventDateTime.", result.ValidationErrors);
    }

    [Fact]
    public void Create_RejectsMixedAccountSet()
    {
        var result = TransactionBuilder.Create(CreateRequest(
            credits: [CreateLeg(10m)],
            debits: [CreateLeg(OtherHoldingID, OtherAccountID, 10m)]), CreateHoldings(includeOtherAccount: true));

        Assert.False(result.IsValid);
        Assert.Contains("All transaction movements in a set must have the same AccountID.", result.ValidationErrors);
    }

    [Fact]
    public void CreateCancellation_EmitsOneCancellationPerOriginalEvent()
    {
        var originalEvents = CreateBalancedSet();
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);

        var result = TransactionCancellationEventBuilder.Create(request, originalEvents.Cast<ITransactionEvent>().ToList());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        var cancellationEvents = result.Value!;
        Assert.Equal(originalEvents.Count, cancellationEvents.Count);
        Assert.Single(cancellationEvents.Select(@event => @event.EventSetID.Value).Distinct());
        Assert.All(cancellationEvents, @event => Assert.Equal(originalEvents[0].AccountID, @event.AccountID));
        Assert.All(cancellationEvents, @event => Assert.Equal(originalEvents[0].EventDateTime, @event.EventDateTime));
        Assert.All(cancellationEvents, @event => Assert.Equal(originalEvents[0].SettlementDateTime, @event.SettlementDateTime));

        var cancellationEventIds = EventIds(cancellationEvents);
        var originalEventIds = EventIds(originalEvents);
        Assert.All(cancellationEvents, @event =>
        {
            Assert.Equal(cancellationEventIds, EventIds(@event.EventIDGroup));
            Assert.Equal(originalEventIds, EventIds(@event.CancelledIDGroup));
        });
        Assert.Equal(originalEventIds.OrderBy(value => value), cancellationEvents.Select(@event => @event.CancelledEventID.Value).OrderBy(value => value));
    }

    [Fact]
    public void CreateCancellation_RejectsIncompleteOriginalSet()
    {
        var originalEvents = CreateBalancedSet();
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);

        var result = TransactionCancellationEventBuilder.Create(request, originalEvents.Take(1).Cast<ITransactionEvent>().ToList());

        Assert.False(result.IsValid);
        Assert.Contains("At least two original transaction events are required.", result.ValidationErrors);
    }

    [Fact]
    public void CreateCancellation_RejectsMixedOriginalEventDates()
    {
        var originalEvents = CreateBalancedSet();
        var changedDebit = Assert.IsType<TransactionDebitEvent>(originalEvents[1]) with
        {
            EventDateTime = EventDateTimeBuilder.Create(originalEvents[1].EventDateTime.Value.AddDays(1))
        };
        var mixedEvents = new List<ITransactionEvent> { originalEvents[0], changedDebit };
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);

        var result = TransactionCancellationEventBuilder.Create(request, mixedEvents);

        Assert.False(result.IsValid);
        Assert.Contains("All original transaction events must have the same EventDateTime.", result.ValidationErrors);
    }

    [Fact]
    public void CreateCancellation_RejectsMixedOriginalSettlementDates()
    {
        var originalEvents = CreateBalancedSet();
        var changedDebit = Assert.IsType<TransactionDebitEvent>(originalEvents[1]) with
        {
            SettlementDateTime = SettlementDateTimeBuilder.Create(originalEvents[1].SettlementDateTime.Value.AddDays(1))
        };
        var mixedEvents = new List<ITransactionEvent> { originalEvents[0], changedDebit };
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);

        var result = TransactionCancellationEventBuilder.Create(request, mixedEvents);

        Assert.False(result.IsValid);
        Assert.Contains("All original transaction events must have the same SettlementDateTime.", result.ValidationErrors);
    }

    [Fact]
    public void CreateCancellation_RejectsMixedOriginalAccounts()
    {
        var originalEvents = CreateBalancedSet();
        var changedDebit = Assert.IsType<TransactionDebitEvent>(originalEvents[1]) with
        {
            AccountID = OtherAccountID
        };
        var mixedEvents = new List<ITransactionEvent> { originalEvents[0], changedDebit };
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);

        var result = TransactionCancellationEventBuilder.Create(request, mixedEvents);

        Assert.False(result.IsValid);
        Assert.Contains("All original transaction events must have the same AccountID.", result.ValidationErrors);
    }

    [Fact]
    public void SettlementDateTime_SerializesAsRoundTripString()
    {
        var settlementDateTime = SettlementDateTimeBuilder.Create(new DateTime(2026, 5, 28, 13, 45, 0, DateTimeKind.Utc));

        var json = JsonSerializer.Serialize(settlementDateTime);
        var restored = JsonSerializer.Deserialize<SettlementDateTime>(json);

        Assert.Equal("\"2026-05-28T13:45:00.0000000Z\"", json);
        Assert.Equal(settlementDateTime, restored);
        Assert.Throws<ArgumentException>(() => SettlementDateTimeBuilder.Create(default));
    }

    [Fact]
    public void CreateCancellation_ThrowsWhenSetAlreadyCancelled()
    {
        var originalEvents = CreateBalancedSet();
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);
        var cancellationEvents = TransactionCancellationEventBuilder.Create(request, originalEvents.Cast<ITransactionEvent>().ToList()).Value!;
        var existingEvents = originalEvents.Cast<ITransactionEvent>().Concat(cancellationEvents).ToList();

        Assert.Throws<InvalidOperationException>(() => TransactionCancellationEventBuilder.Create(request, existingEvents));
    }

    [Fact]
    public void CreateCancellation_RejectsCancellationSetID()
    {
        var originalEvents = CreateBalancedSet();
        var cancellationEvents = TransactionCancellationEventBuilder.Create(
            new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID),
            originalEvents.Cast<ITransactionEvent>().ToList()).Value!;
        var allEvents = originalEvents.Cast<ITransactionEvent>().Concat(cancellationEvents).ToList();
        var request = new TransactionCancellationRequest(UserID, "Cancel cancellation", cancellationEvents[0].EventSetID);

        var result = TransactionCancellationEventBuilder.Create(request, allEvents);

        Assert.False(result.IsValid);
        Assert.Contains($"No transaction events found for EventSetID '{request.EventSetID}'.", result.ValidationErrors);
    }

    [Fact]
    public void GetActiveMovements_IsAuditAwareForCancellations()
    {
        var originalEvents = CreateBalancedSet();
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);
        var cancellationEvents = TransactionCancellationEventBuilder.Create(request, originalEvents.Cast<ITransactionEvent>().ToList()).Value!;
        var allEvents = originalEvents.Cast<ITransactionEvent>().Concat(cancellationEvents).ToList();
        var beforeCancellation = AuditDateTimeBuilder.Create(cancellationEvents[0].AuditDateTime.Value.AddTicks(-1));
        var atCancellation = AuditDateTimeBuilder.Create(cancellationEvents[0].AuditDateTime.Value);

        Assert.Equal(originalEvents.Count, TransactionEventSelector.GetActiveMovements(allEvents, beforeCancellation).Count);
        Assert.Empty(TransactionEventSelector.GetActiveMovements(allEvents, atCancellation));
    }

    private static readonly UserID UserID = new(Guid.Parse("72afdf42-8088-4820-9d2c-e6037115f32b"));
    private static readonly EventDateTime HoldingEventDate = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-20));
    private static readonly AuditDateTime HoldingAuditDate = AuditDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-19));
    private static readonly HoldingID HoldingID = HoldingIDBuilder.Create();
    private static readonly HoldingID OtherHoldingID = HoldingIDBuilder.Create();
    private static readonly InstrumentID InstrumentID = InstrumentIDBuilder.Create();
    private static readonly AccountID AccountID = AccountIDBuilder.Create();
    private static readonly AccountID OtherAccountID = AccountIDBuilder.Create();

    private static TransactionSetRequest CreateRequest(IReadOnlyList<TransactionRequest> credits, IReadOnlyList<TransactionRequest> debits) =>
        CreateRequest(EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10)), credits, debits);

    private static TransactionSetRequest CreateRequest(EventDateTime eventDateTime, IReadOnlyList<TransactionRequest> credits, IReadOnlyList<TransactionRequest> debits) =>
        new(
            UserID,
            eventDateTime,
            SettlementDateTimeBuilder.Create(eventDateTime.Value.AddDays(1)),
            "Book transaction",
            credits,
            debits);

    private static TransactionRequest CreateLeg(decimal bookCost) =>
        CreateLeg(HoldingID, AccountID, bookCost);

    private static TransactionRequest CreateLeg(HoldingID holdingID, AccountID accountID, decimal bookCost) =>
        new(
            holdingID,
            InstrumentID,
            accountID,
            new TransactionQuantity(1m),
            new TransactionBookCost(bookCost));

    private static Holdings CreateHoldings(bool includeOtherAccount = false)
    {
        var holdingCreated = HoldingPositionMemoCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            HoldingEventDate,
            HoldingAuditDate,
            "Create holding",
            HoldingID,
            AccountID,
            InstrumentID,
            string.Empty,
            true,
            false).Value!;

        if (!includeOtherAccount)
            return new Holdings(HoldingEventDate, HoldingAuditDate, [holdingCreated]);

        var otherHoldingCreated = HoldingPositionMemoCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            HoldingEventDate,
            HoldingAuditDate,
            "Create other holding",
            OtherHoldingID,
            OtherAccountID,
            InstrumentID,
            string.Empty,
            true,
            false).Value!;

        return new Holdings(HoldingEventDate, HoldingAuditDate, [holdingCreated, otherHoldingCreated]);
    }

    private static IReadOnlyList<ITransactionMovementEvent> CreateBalancedSet()
    {
        var result = TransactionBuilder.Create(CreateRequest(
            credits: [CreateLeg(10m)],
            debits: [CreateLeg(10m)]), CreateHoldings());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        return result.Value!;
    }

    private static IReadOnlyList<Guid> EventIds(IEnumerable<ITransactionMovementEvent> events) =>
        events.Select(@event => @event.EventID.Value).ToList();

    private static IReadOnlyList<Guid> EventIds(IEnumerable<TransactionCancellationEvent> events) =>
        events.Select(@event => @event.EventID.Value).ToList();

    private static IReadOnlyList<Guid> EventIds(IEnumerable<EventID> events) =>
        events.Select(@event => @event.Value).ToList();
}
