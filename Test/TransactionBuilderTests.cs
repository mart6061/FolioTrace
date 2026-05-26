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
    public void CreateCancellation_EmitsOneCancellationPerOriginalEvent()
    {
        var originalEvents = CreateBalancedSet();
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);

        var result = TransactionCancellationEventBuilder.Create(request, originalEvents.Cast<ITransactionEvent>().ToList());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        var cancellationEvents = result.Value!;
        Assert.Equal(originalEvents.Count, cancellationEvents.Count);
        Assert.Single(cancellationEvents.Select(@event => @event.EventSetID.Value).Distinct());
        Assert.All(cancellationEvents, @event => Assert.Equal(originalEvents[0].EventDateTime, @event.EventDateTime));

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
    public void CreateCancellation_ThrowsWhenSetAlreadyCancelled()
    {
        var originalEvents = CreateBalancedSet();
        var request = new TransactionCancellationRequest(UserID, "Cancel trade", originalEvents[0].EventSetID);
        var cancellationEvents = TransactionCancellationEventBuilder.Create(request, originalEvents.Cast<ITransactionEvent>().ToList()).Value!;
        var existingEvents = originalEvents.Cast<ITransactionEvent>().Concat(cancellationEvents).ToList();

        Assert.Throws<InvalidOperationException>(() => TransactionCancellationEventBuilder.Create(request, existingEvents));
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
    private static readonly InstrumentID InstrumentID = InstrumentIDBuilder.Create();
    private static readonly AccountID AccountID = AccountIDBuilder.Create();

    private static TransactionSetRequest CreateRequest(IReadOnlyList<TransactionRequest> credits, IReadOnlyList<TransactionRequest> debits) =>
        new(
            UserID,
            EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10)),
            "Book transaction",
            credits,
            debits);

    private static TransactionRequest CreateLeg(decimal bookCost) =>
        new(
            HoldingID,
            InstrumentID,
            AccountID,
            new TransactionQuantity(1m),
            new TransactionBookCost(bookCost));

    private static Holdings CreateHoldings()
    {
        var holdingCreated = HoldingCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            HoldingEventDate,
            HoldingAuditDate,
            "Create holding",
            HoldingID,
            AccountID,
            InstrumentID,
            HoldingType.Position,
            null,
            string.Empty,
            true,
            false).Value!;

        return new Holdings(HoldingEventDate, HoldingAuditDate, [holdingCreated]);
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
