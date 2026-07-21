using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;

namespace Test;

public sealed class SeedRepositoryTests
{
    public static TheoryData<string, Func<IReadOnlyList<IAuditEventBase>>> SeedEventFactories => new()
    {
        { "country created", () => SeedRepository.CreateInitialCountryCreatedEvents().Cast<IAuditEventBase>().ToList() },
        { "country flag modified", () => SeedRepository.CreateInitialCountryFlagModifiedEvents().Cast<IAuditEventBase>().ToList() },
        { "country modified", () => SeedRepository.CreateInitialCountryModifiedEvents().Cast<IAuditEventBase>().ToList() },
        { "currency created", () => SeedRepository.CreateInitialCurrencyCreatedEvents().Cast<IAuditEventBase>().ToList() },
        { "currency modified", () => SeedRepository.CreateInitialCurrencyModifiedEvents().Cast<IAuditEventBase>().ToList() },
        { "broker created", () => SeedRepository.CreateInitialBrokerCreatedEvents().Cast<IAuditEventBase>().ToList() },
        { "broker trade methods", () => SeedRepository.CreateInitialBrokerTradeMethodSetEvents().Cast<IAuditEventBase>().ToList() },
        { "account created", () => SeedRepository.CreateInitialAccountCreatedEvents().Cast<IAuditEventBase>().ToList() },
        { "account modified", () => SeedRepository.CreateInitialAccountModifiedEvents().Cast<IAuditEventBase>().ToList() },
        { "account active set", () => SeedRepository.CreateInitialAccountActiveModifiedEvents().Cast<IAuditEventBase>().ToList() },
        { "account identifier set", () => SeedRepository.CreateInitialAccountIdentifierSetEvents().Cast<IAuditEventBase>().ToList() },
        { "asset allocation created", () => SeedRepository.CreateInitialAssetAllocationCreatedEvents().Cast<IAuditEventBase>().ToList() },
        { "report created", () => SeedRepository.CreateInitialReportCreatedEvents().Cast<IAuditEventBase>().ToList() },
        { "holding created", () => SeedRepository.CreateInitialHoldingCreatedEvents().Cast<IAuditEventBase>().ToList() },
        { "transaction", () => SeedRepository.CreateInitialTransactionEvents().Cast<IAuditEventBase>().ToList() },
        { "ticket", () => SeedRepository.CreateInitialTicketEvents().Cast<IAuditEventBase>().ToList() }
    };

    [Theory]
    [MemberData(nameof(SeedEventFactories))]
    public void SeedData_PublicFactoriesCreateValidEvents(string seedGroup, Func<IReadOnlyList<IAuditEventBase>> createEvents)
    {
        var events = createEvents();

        Assert.NotEmpty(events);
        Assert.All(events.Select((@event, index) => (Event: @event, Index: index)), item =>
            AssertValidSeedEvent(seedGroup, item.Index, item.Event));
    }

    [Fact]
    public async Task SeedData_BuildCreatesOnlyValidEvents()
    {
        var eventRepository = new ValidatingEventRepository();
        var seedRepository = new SeedRepository(eventRepository);

        await seedRepository.Build();

        Assert.True(eventRepository.EventCount > 0);
        Assert.Contains(FolioTrace.Constants.Initialisation.FXsStreamId, eventRepository.StreamIds);
        Assert.Contains(FolioTrace.Constants.Initialisation.InstrumentsStreamId, eventRepository.StreamIds);
        Assert.Contains(FolioTrace.Constants.Initialisation.TransactionsStreamId, eventRepository.StreamIds);
        Assert.Contains(FolioTrace.Constants.Initialisation.TicketsStreamId, eventRepository.StreamIds);
    }

    [Fact]
    public void SeedData_SetsBookCostFromLocalCostAndFxWhenRequired()
    {
        var events = SeedRepository.CreateInitialTransactionEvents()
            .OfType<ITransactionMovementEvent>()
            .ToList();

        Assert.Contains(events, @event =>
            @event.LocalCostCurrency.Value == "GBP" &&
            @event.BookCostSource == BookCostSource.SameCurrency &&
            !@event.BookCostEstimated &&
            @event.BookCost.Value == @event.LocalCost.Value);

        Assert.Contains(events, @event =>
            @event.LocalCostCurrency.Value != "GBP" &&
            @event.BookCostSource == BookCostSource.FxEstimate &&
            @event.BookCostEstimated &&
            @event.BookCost.Value != @event.LocalCost.Value);
    }

    [Fact]
    public void SeedData_CreatesBuyAndSellTicketsAcrossEveryStage()
    {
        var ticketEvents = SeedRepository.CreateInitialTicketEvents();
        var tickets = new Tickets(EventDateTimeBuilder.Create(DateTime.UtcNow), ticketEvents.ToList());

        // 6 lifecycle stages x (5 buy + 5 sell).
        Assert.Equal(60, tickets.Items.Count);
        Assert.Equal(30, tickets.Items.Count(ticket => ticket.Side == TicketSide.Buy));
        Assert.Equal(30, tickets.Items.Count(ticket => ticket.Side == TicketSide.Sell));

        // Proposal - in progress.
        Assert.Equal(10, tickets.Items.Count(ticket =>
            ticket.Stage == TicketStage.Proposal && ticket.ProposalDecision == TicketDecision.InProgress));
        // Proposal - pending approval.
        Assert.Equal(10, tickets.Items.Count(ticket =>
            ticket.Stage == TicketStage.Proposal && ticket.ProposalDecision == TicketDecision.PendingApproval));
        // Trade - ready (proposal approved, no fills yet).
        Assert.Equal(10, tickets.Items.Count(ticket =>
            ticket.Stage == TicketStage.Trade && ticket.TradeDecision == TicketDecision.InProgress && ticket.Fills.Count == 0));
        // Trade - in progress (fills added, not yet submitted for approval).
        Assert.Equal(10, tickets.Items.Count(ticket =>
            ticket.Stage == TicketStage.Trade && ticket.TradeDecision == TicketDecision.InProgress && ticket.Fills.Count > 0));
        // Trade - pending approval.
        Assert.Equal(10, tickets.Items.Count(ticket =>
            ticket.Stage == TicketStage.Trade && ticket.TradeDecision == TicketDecision.PendingApproval));
        // Completed.
        Assert.Equal(10, tickets.Items.Count(ticket => ticket.Stage == TicketStage.Completed));
    }

    [Fact]
    public void SeedData_SellTicketsOnlyTargetHeldInstruments()
    {
        var ticketEvents = SeedRepository.CreateInitialTicketEvents();
        var tickets = new Tickets(EventDateTimeBuilder.Create(DateTime.UtcNow), ticketEvents.ToList());
        var holdings = new Holdings(
            EventDateTimeBuilder.Create(DateTime.UtcNow),
            AuditDateTimeBuilder.Create(),
            SeedRepository.CreateInitialHoldingCreatedEvents().Cast<IHoldingEvent>().ToList());

        var assetHoldingKinds = holdings.Items
            .Where(holding => holding.GetHoldingKindName() == HoldingKindRuntime.GetKindName<HoldingPositionAsset>())
            .Select(holding => (holding.AccountID, holding.InstrumentID))
            .ToHashSet();

        var sellTickets = tickets.Items.Where(ticket => ticket.Side == TicketSide.Sell).ToList();
        Assert.NotEmpty(sellTickets);

        // "You can't sell what you don't have": every sell ticket's account must already hold the instrument.
        Assert.All(sellTickets, ticket =>
            Assert.All(ticket.AccountIDs, accountID =>
                Assert.Contains((accountID, ticket.InstrumentID), assetHoldingKinds)));
    }

    private static void AssertValidSeedEvent(string seedGroup, int index, IAuditEventBase? @event)
    {
        Assert.True(@event is not null, $"{seedGroup}[{index}] produced a null event.");
        Assert.NotNull(@event.EventID);
        Assert.NotNull(@event.UserID);
        Assert.NotNull(@event.AuditDateTime);
        Assert.False(string.IsNullOrWhiteSpace(@event.Type), $"{seedGroup}[{index}] has no event type.");

        if (@event is IEventBase eventBase)
        {
            Assert.NotNull(eventBase.EventDateTime);
            Assert.False(string.IsNullOrWhiteSpace(eventBase.Reason), $"{seedGroup}[{index}] has no reason.");
        }
    }

    private sealed class ValidatingEventRepository : IEventRepository
    {
        private readonly Dictionary<Guid, List<IAuditEventBase>> streams = [];
        private readonly Dictionary<Guid, IAuditEventBase> eventsById = [];

        public int EventCount => eventsById.Count;

        public IReadOnlyList<Guid> StreamIds => streams.Keys.ToList();

        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, streams.Count, eventsById.Count, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            streams.Clear();
            eventsById.Clear();
            return Task.CompletedTask;
        }

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default)
            where T : class, IAuditEventBase =>
            Task.FromResult(eventsById.TryGetValue(eventId.Value, out var @event) ? @event as T : null);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                streams.TryGetValue(streamId, out var events) && events.Count > 0
                    ? events[^1].EventID
                    : null);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default)
        {
            if (!streams.TryGetValue(streamId, out var events))
                return Task.FromResult<EventID?>(null);

            IAuditEventBase? latest = null;
            foreach (var @event in events)
            {
                if (@event is IEventBase eventBase && eventBase.EventDateTime.Value > valuationDateTime)
                    continue;

                if (asOfDateTime.HasValue && @event.AuditDateTime.Value > asOfDateTime.Value)
                    continue;

                if (latest is null || CompareEventOrder(@event, latest) > 0)
                    latest = @event;
            }

            return Task.FromResult(latest?.EventID);
        }

        public Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IAuditEventBase>>(streams.TryGetValue(streamId, out var events) ? events.ToList() : []);

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IAuditEventBase =>
            Task.FromResult<IReadOnlyList<TEvent>>(streams.TryGetValue(streamId, out var events) ? events.OfType<TEvent>().ToList() : []);

        public Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
            where TAggregate : class
            where TEvent : class, IAuditEventBase
        {
            if (events is null)
                throw new ArgumentNullException(nameof(events));

            streams[streamId] = [];
            foreach (var (@event, index) in events.Select((@event, index) => (@event, index)))
                AddEvent(streamId, @event, $"{typeof(TEvent).Name} stream start", index);

            return Task.CompletedTask;
        }

        public Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default)
            where T : class, IAuditEventBase
        {
            AddEvent(streamId, @event, typeof(T).Name, 0);
            return Task.CompletedTask;
        }

        public Task AppendAsync(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken = default)
        {
            if (events is null)
                throw new ArgumentNullException(nameof(events));

            foreach (var (@event, index) in events.Select((@event, index) => (@event, index)))
                AddEvent(streamId, @event, "batched event", index);

            return Task.CompletedTask;
        }

        private void AddEvent(Guid streamId, IAuditEventBase? @event, string seedGroup, int index)
        {
            AssertValidSeedEvent(seedGroup, index, @event);

            if (!eventsById.TryAdd(@event!.EventID.Value, @event))
                throw new InvalidOperationException($"Duplicate seed event ID '{@event.EventID.Value}' in {seedGroup}[{index}].");

            if (!streams.TryGetValue(streamId, out var events))
            {
                events = [];
                streams[streamId] = events;
            }

            events.Add(@event);
        }

        private static int CompareEventOrder(IAuditEventBase left, IAuditEventBase right)
        {
            if (left is IEventBase leftTimed && right is IEventBase rightTimed)
            {
                var eventDateComparison = leftTimed.EventDateTime.Value.CompareTo(rightTimed.EventDateTime.Value);
                if (eventDateComparison != 0)
                    return eventDateComparison;
            }

            var auditDateComparison = left.AuditDateTime.Value.CompareTo(right.AuditDateTime.Value);
            if (auditDateComparison != 0)
                return auditDateComparison;

            return left.EventID.Value.CompareTo(right.EventID.Value);
        }
    }

}
