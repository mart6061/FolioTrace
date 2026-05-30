using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public enum TicketSide
{
    Buy,
    Sell
}

public enum TicketStatus
{
    Draft,
    Proposal,
    ProposalApproved,
    ProposalNotApproved,
    Trade,
    TradeApproved,
    TradeNotApproved,
    Completed,
    Cancelled
}

public sealed record TicketProposalAllocation(AccountID AccountID, decimal Quantity);

public sealed record TicketTradeAllocation(AccountID AccountID, decimal Quantity, decimal BookCost);

public sealed record TicketFill(Guid FillID, decimal Price, decimal Quantity, string Note);

public sealed record Ticket : IModel
{
    public required TicketNumber TicketNumber { get; init; }
    public required TicketSide Side { get; init; }
    public required InstrumentID InstrumentID { get; init; }
    public required TicketStatus Status { get; init; }
    public required List<AccountID> AccountIDs { get; init; }
    public decimal? ProposalTargetPrice { get; init; }
    public decimal? ProposalTotalAmount { get; init; }
    public required List<TicketProposalAllocation> ProposalAllocations { get; init; }
    public decimal? TradePrice { get; init; }
    public required List<TicketTradeAllocation> TradeAllocations { get; init; }
    public required List<TicketFill> Fills { get; init; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }
    public bool IsActive => Status is not TicketStatus.Completed and not TicketStatus.Cancelled;

    [JsonConstructor]
    [SetsRequiredMembers]
    public Ticket() { }

    public string ToData() => $"{TicketNumber.ToData()}|{Side}|{InstrumentID.ToData()}|{Status}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Ticket)}: ({TicketNumber.ToDetail()}, Side: {Side}, InstrumentID: {InstrumentID.ToDetail()}, Status: {Status})";
}

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

        var orderedItems = items
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
                    AccountIDs = ticket.AccountIDs.Contains(@event.AccountID) ? ticket.AccountIDs : [.. ticket.AccountIDs, @event.AccountID],
                    Status = ticket.Status is TicketStatus.Draft ? TicketStatus.Draft : ticket.Status
                });
                break;
            case TicketAccountRemovedEvent @event:
                Update(@event, ticket => ticket with { AccountIDs = ticket.AccountIDs.Where(accountID => accountID != @event.AccountID).ToList() });
                break;
            case TicketProposalCreatedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Status = TicketStatus.Proposal,
                    ProposalTargetPrice = @event.TargetPrice,
                    ProposalTotalAmount = @event.TotalAmount,
                    ProposalAllocations = @event.Allocations.ToList()
                });
                break;
            case TicketProposalModifiedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Status = TicketStatus.Proposal,
                    ProposalTargetPrice = @event.TargetPrice,
                    ProposalTotalAmount = @event.TotalAmount,
                    ProposalAllocations = @event.Allocations.ToList()
                });
                break;
            case TicketProposalApprovedEvent @event:
                Update(@event, ticket => ticket with { Status = TicketStatus.ProposalApproved });
                break;
            case TicketProposalNotApprovedEvent @event:
                Update(@event, ticket => ticket with { Status = TicketStatus.ProposalNotApproved });
                break;
            case TicketTradeCreatedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Status = TicketStatus.Trade,
                    TradePrice = @event.TradedPrice,
                    TradeAllocations = @event.Allocations.ToList()
                });
                break;
            case TicketTradeModifiedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Status = TicketStatus.Trade,
                    TradePrice = @event.TradedPrice,
                    TradeAllocations = @event.Allocations.ToList()
                });
                break;
            case TicketTradeFillAddedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Status = TicketStatus.Trade,
                    Fills = [.. ticket.Fills.Where(fill => fill.FillID != @event.FillID), new TicketFill(@event.FillID, @event.Price, @event.Quantity, @event.Note)]
                });
                break;
            case TicketTradeFillModifiedEvent @event:
                Update(@event, ticket => ticket with
                {
                    Status = TicketStatus.Trade,
                    Fills = [.. ticket.Fills.Where(fill => fill.FillID != @event.FillID), new TicketFill(@event.FillID, @event.Price, @event.Quantity, @event.Note)]
                });
                break;
            case TicketTradeFillRemovedEvent @event:
                Update(@event, ticket => ticket with { Fills = ticket.Fills.Where(fill => fill.FillID != @event.FillID).ToList() });
                break;
            case TicketTradeApprovedEvent @event:
                Update(@event, ticket => ticket with { Status = TicketStatus.Completed });
                break;
            case TicketTradeNotApprovedEvent @event:
                Update(@event, ticket => ticket with { Status = TicketStatus.TradeNotApproved });
                break;
            case TicketCancelledEvent @event:
                Update(@event, ticket => ticket with { Status = TicketStatus.Cancelled });
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
            Status = TicketStatus.Draft,
            AccountIDs = [],
            ProposalAllocations = [],
            TradeAllocations = [],
            Fills = [],
            ValuationDateTime = createdEvent.EventDateTime,
            AsOfDateTime = createdEvent.AuditDateTime,
            LastEventID = createdEvent.EventID,
            LastAuditDateTime = LastAuditDateTimeBuilder.Create(createdEvent.AuditDateTime.Value)
        });
        UpdateProvenance(createdEvent);
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

    public string ToData() => $"{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}|{Items.Count}";

    public string ToDetail() => $"{nameof(Tickets)}: (Items: {Items.Count})";

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<ITicket> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Any()
            ? AuditDateTimeBuilder.Create(includedItems.Max(@event => @event.AuditDateTime.Value))
            : AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value);
    }
}
