using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;


namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Trade tickets")]
public sealed record Tickets : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<Ticket> Items { get; init; }

    [SetsRequiredMembers]
    public Tickets(EventDateTime valuationDateTime, List<ITicket> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Tickets(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<ITicket> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        Items = [];
        LastEventID = Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = LastAuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value);

        var tradeFileRequestEventDates = items
            .OfType<TicketTradeFileRequestedEvent>()
            .Where(@event => @event.TradeFileID is not null)
            .GroupBy(@event => (@event.TicketNumber, TradeFileID: @event.TradeFileID!))
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(@event => @event.AuditDateTime.Value).ThenBy(@event => @event.EventID.Value).First().EventDateTime);
        var orderedItems = items
            .Select(item => NormalizeTradeFileEventDate(item, tradeFileRequestEventDates))
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        foreach (var item in orderedItems)
            Apply(item);
    }

    public void Apply(ITicket ticketEvent)
    {
        switch (ticketEvent)
        {
            case TicketCreatedEvent @event:
                Apply(@event);
                break;
            case TicketAccountAddedEvent @event:
                Update(@event, ticket => ticket with
                {
                    AccountIDs = ticket.AccountIDs.Contains(@event.AccountID) ? ticket.AccountIDs : [.. ticket.AccountIDs, @event.AccountID]
                });
                break;
            case TicketAccountRemovedEvent @event:
                Update(@event, ticket => ticket with { AccountIDs = ticket.AccountIDs.Where(accountID => accountID != @event.AccountID).ToList() });
                break;
            case TicketProposalCreatedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Stage = TicketStage.Proposal,
                    ProposalDecision = TicketDecision.InProgress,
                    ProposalTargetPrice = @event.TargetPrice,
                    TradeCurrency = @event.TradeCurrency,
                    ProposalAllocations = @event.Allocations.ToList()
                });
                break;
            case TicketProposalModifiedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Stage = TicketStage.Proposal,
                    ProposalDecision = TicketDecision.InProgress,
                    ProposalTargetPrice = @event.TargetPrice,
                    TradeCurrency = @event.TradeCurrency,
                    ProposalAllocations = @event.Allocations.ToList()
                });
                break;
            case TicketProposalDecisionRequestedEvent @event:
                Update(@event, ticket => ticket with { Stage = TicketStage.Proposal, ProposalDecision = TicketDecision.PendingApproval });
                break;
            case TicketProposalApprovedEvent @event:
                Update(@event, ticket => ticket with { Stage = TicketStage.Trade, ProposalDecision = TicketDecision.Approved, TradeDecision = TicketDecision.InProgress, TradeExecutionStatus = TicketTradeExecutionStatus.Ready, TradeExecutionMethod = null, ExecutionBrokerLEI = null, TradeFileID = null, TradeExecutionError = string.Empty });
                break;
            case TicketProposalNotApprovedEvent @event:
                Update(@event, ticket => ticket with { Stage = TicketStage.Proposal, ProposalDecision = TicketDecision.InProgress });
                break;
            case TicketProposalReasonSetEvent @event:
                Update(@event, ticket => ticket with { ProposalReason = @event.ProposalReason });
                break;
            case TicketProposalAllocationSetEvent @event:
                Update(@event, ticket => ticket with { ProposalAllocation = @event.ProposalAllocation });
                break;
            case TicketTradeCreatedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Stage = TicketStage.Trade,
                    TradeDecision = TicketDecision.InProgress,
                    TradePrice = @event.TradedPrice,
                    TradeDateTime = @event.TradeDateTime,
                    SettlementDateTime = @event.SettlementDateTime,
                    TradeAllocations = @event.Allocations.ToList()
                });
                break;
            case TicketTradeModifiedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Stage = TicketStage.Trade,
                    TradeDecision = TicketDecision.InProgress,
                    TradePrice = @event.TradedPrice,
                    TradeDateTime = @event.TradeDateTime,
                    SettlementDateTime = @event.SettlementDateTime,
                    TradeAllocations = @event.Allocations.ToList()
                });
                break;
            case TicketTradeFillAddedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Stage = TicketStage.Trade,
                    TradeDecision = TicketDecision.InProgress,
                    Fills = [.. ticket.Fills.Where(fill => fill.FillID != @event.FillID), new TicketFill(@event.FillID, @event.BrokerLEI, @event.Price, @event.Quantity, @event.SettlementAmount, @event.Note, @event.BookCostOverride)]
                });
                break;
            case TicketTradeFillModifiedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Stage = TicketStage.Trade,
                    TradeDecision = TicketDecision.InProgress,
                    Fills = [.. ticket.Fills.Where(fill => fill.FillID != @event.FillID), new TicketFill(@event.FillID, @event.BrokerLEI, @event.Price, @event.Quantity, @event.SettlementAmount, @event.Note, @event.BookCostOverride)]
                });
                break;
            case TicketTradeFillRemovedEvent @event:
                Update(@event, ticket => ticket with { Stage = TicketStage.Trade, TradeDecision = TicketDecision.InProgress, Fills = ticket.Fills.Where(fill => fill.FillID != @event.FillID).ToList() });
                break;
            case TicketTradeDecisionRequestedEvent @event:
                Update(@event, ticket => ticket with { Stage = TicketStage.Trade, TradeDecision = TicketDecision.PendingApproval });
                break;
            case TicketTradeApprovedEvent @event:
                Update(@event, ticket => ticket with { Stage = TicketStage.Completed, TradeDecision = TicketDecision.Approved, TradeExecutionStatus = TicketTradeExecutionStatus.Completed });
                break;
            case TicketTradeNotApprovedEvent @event:
                Update(@event, ticket => ticket with { Stage = TicketStage.Trade, TradeDecision = TicketDecision.InProgress });
                break;
            case TicketTradeInstructionNotesSetEvent @event:
                Update(@event, ticket => ticket with { TradeInstructionNotes = @event.TradeInstructionNotes });
                break;
            case TicketTradeProgressNotesSetEvent @event:
                Update(@event, ticket => ticket with { TradeProgressNotes = @event.TradeProgressNotes });
                break;
            case TicketCancelledEvent @event:
                Update(@event, ticket => ticket with { Stage = TicketStage.Cancelled });
                break;
            case TicketTradeExecutionEventBase @event:
                Apply(@event);
                break;
            default:
                throw new InvalidOperationException($"Unsupported ticket event type '{ticketEvent.GetType().Name}'.");
        }
    }

    public Ticket? Find(TicketNumber ticketNumber) =>
        Items.SingleOrDefault(ticket => ticket.TicketNumber == ticketNumber);

    private void Apply(TicketCreatedEvent createdEvent)
    {
        if (Items.Any(ticket => ticket.TicketNumber == createdEvent.TicketNumber))
            throw new InvalidOperationException($"Ticket already exists for TicketNumber '{createdEvent.TicketNumber}'.");

        Items.Add(new Ticket
        {
            TicketNumber = createdEvent.TicketNumber,
            Side = createdEvent.Side,
            InstrumentID = createdEvent.InstrumentID,
            TradeCurrency = createdEvent.TradeCurrency,
            Stage = TicketStage.Proposal,
            ProposalDecision = TicketDecision.InProgress,
            TradeDecision = TicketDecision.InProgress,
            TradeExecutionStatus = TicketTradeExecutionStatus.Ready,
            AccountIDs = [],
            ProposalAllocations = [],
            ProposalReason = string.Empty,
            ProposalAllocation = string.Empty,
            TradeAllocations = [],
            Fills = [],
            TradeInstructionNotes = string.Empty,
            TradeProgressNotes = string.Empty,
            ValuationDateTime = createdEvent.EventDateTime,
            AsOfDateTime = createdEvent.AuditDateTime,
            LastEventID = createdEvent.EventID,
            LastAuditDateTime = LastAuditDateTimeBuilder.Create(createdEvent.AuditDateTime.Value)
        });
        UpdateProvenance(createdEvent);
    }

    private void Apply(TicketTradeExecutionEventBase @event)
    {
        var status = @event switch
        {
            TicketFIXRequestedEvent => TicketTradeExecutionStatus.FIXRequested,
            TicketTradeFilePendingEvent => TicketTradeExecutionStatus.PendingTradeFile,
            TicketTradeFileRequestedEvent => TicketTradeExecutionStatus.TradeFileRequested,
            TicketTradeFileCreatedEvent => TicketTradeExecutionStatus.TradeFileCreated,
            TicketTradeFileSentEvent => TicketTradeExecutionStatus.TradeFileSent,
            TicketTradeFileAcknowledgedEvent => TicketTradeExecutionStatus.TradeFileAcknowledged,
            TicketTradeExecutionInProgressEvent => TicketTradeExecutionStatus.InProgress,
            TicketTradeExecutionFailedEvent => TicketTradeExecutionStatus.Failed,
            _ => throw new InvalidOperationException($"Unsupported execution event '{@event.Type}'.")
        };
        Update(@event, ticket => ticket with
        {
            TradeExecutionStatus = status,
            TradeExecutionMethod = @event.TradeMethodType,
            ExecutionBrokerLEI = @event.BrokerLEI,
            TradeFileID = @event.TradeFileID,
            TradeExecutionError = @event is TicketTradeExecutionFailedEvent failed ? failed.Error : string.Empty
        });
    }

    private void Update(ITicket @event, Func<Ticket, Ticket> update)
    {
        var index = Items.FindIndex(ticket => ticket.TicketNumber == @event.TicketNumber);
        if (index < 0)
            throw new InvalidOperationException($"No matching ticket found for TicketNumber '{@event.TicketNumber}'.");

        var updated = update(Items[index]) with
        {
            ValuationDateTime = @event.EventDateTime,
            AsOfDateTime = @event.AuditDateTime,
            LastEventID = @event.EventID,
            LastAuditDateTime = LastAuditDateTimeBuilder.Create(@event.AuditDateTime.Value)
        };
        Items[index] = updated;
        UpdateProvenance(@event);
    }

    private void UpdateProvenance(ITicket @event)
    {
        LastEventID = @event.EventID;
        LastAuditDateTime = LastAuditDateTimeBuilder.Create(Items.Max(ticket => ticket.LastAuditDateTime.Value));
    }

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<ITicket> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Any()
            ? AuditDateTimeBuilder.Create(includedItems.Max(@event => @event.AuditDateTime.Value))
            : AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value);
    }

    private static ITicket NormalizeTradeFileEventDate(
        ITicket @event,
        IReadOnlyDictionary<(TicketNumber TicketNumber, TradeFileID TradeFileID), EventDateTime> requestEventDates)
    {
        if (@event is not TicketTradeExecutionEventBase
            {
                TradeMethodType: TradeMethodType.TradeFile,
                TradeFileID: not null
            } tradeFileEvent ||
            !requestEventDates.TryGetValue((@event.TicketNumber, tradeFileEvent.TradeFileID), out var requestEventDate) ||
            @event.EventDateTime.Value >= requestEventDate.Value)
            return @event;

        return tradeFileEvent with { EventDateTime = requestEventDate };
    }
}
