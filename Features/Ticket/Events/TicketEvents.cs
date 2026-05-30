using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record TicketEventBase(EventID EventID, UserID UserID, EventDateTime EventDateTime, AuditDateTime AuditDateTime, string Reason, TicketNumber TicketNumber)
    : EventBase(EventID, UserID, EventDateTime, AuditDateTime, Reason), ITicket;

public sealed record TicketCreatedEvent : TicketEventBase
{
    public TicketSide Side { get; init; }
    public InstrumentID InstrumentID { get; init; } = null!;

    [JsonConstructor]
    private TicketCreatedEvent() : this(null!, null!, null!, null!, string.Empty, null!, default, null!) { }

    internal TicketCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, TicketSide side, InstrumentID instrumentID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        Side = side;
        InstrumentID = instrumentID;
    }

    public override string Type => nameof(TicketCreatedEvent);
}

public sealed record TicketAccountAddedEvent : TicketEventBase
{
    public AccountID AccountID { get; init; } = null!;

    [JsonConstructor]
    private TicketAccountAddedEvent() : this(null!, null!, null!, null!, string.Empty, null!, null!) { }

    internal TicketAccountAddedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, AccountID accountID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        AccountID = accountID;

    public override string Type => nameof(TicketAccountAddedEvent);
}

public sealed record TicketAccountRemovedEvent : TicketEventBase
{
    public AccountID AccountID { get; init; } = null!;

    [JsonConstructor]
    private TicketAccountRemovedEvent() : this(null!, null!, null!, null!, string.Empty, null!, null!) { }

    internal TicketAccountRemovedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, AccountID accountID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        AccountID = accountID;

    public override string Type => nameof(TicketAccountRemovedEvent);
}

public abstract record TicketProposalEventBase : TicketEventBase
{
    public decimal TargetPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<TicketProposalAllocation> Allocations { get; init; } = [];

    protected TicketProposalEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, decimal targetPrice, decimal totalAmount, IReadOnlyList<TicketProposalAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        TargetPrice = targetPrice;
        TotalAmount = totalAmount;
        Allocations = allocations.ToList();
    }
}

public sealed record TicketProposalCreatedEvent : TicketProposalEventBase
{
    [JsonConstructor]
    private TicketProposalCreatedEvent() : this(null!, null!, null!, null!, string.Empty, null!, 0, 0, []) { }

    internal TicketProposalCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, decimal targetPrice, decimal totalAmount, IReadOnlyList<TicketProposalAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, targetPrice, totalAmount, allocations) { }

    public override string Type => nameof(TicketProposalCreatedEvent);
}

public sealed record TicketProposalModifiedEvent : TicketProposalEventBase
{
    [JsonConstructor]
    private TicketProposalModifiedEvent() : this(null!, null!, null!, null!, string.Empty, null!, 0, 0, []) { }

    internal TicketProposalModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, decimal targetPrice, decimal totalAmount, IReadOnlyList<TicketProposalAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, targetPrice, totalAmount, allocations) { }

    public override string Type => nameof(TicketProposalModifiedEvent);
}

public sealed record TicketProposalApprovedEvent : TicketEventBase
{
    [JsonConstructor]
    private TicketProposalApprovedEvent() : this(null!, null!, null!, null!, string.Empty, null!) { }

    internal TicketProposalApprovedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) { }

    public override string Type => nameof(TicketProposalApprovedEvent);
}

public sealed record TicketProposalNotApprovedEvent : TicketEventBase
{
    [JsonConstructor]
    private TicketProposalNotApprovedEvent() : this(null!, null!, null!, null!, string.Empty, null!) { }

    internal TicketProposalNotApprovedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) { }

    public override string Type => nameof(TicketProposalNotApprovedEvent);
}

public abstract record TicketTradeEventBase : TicketEventBase
{
    public decimal TradedPrice { get; init; }
    public IReadOnlyList<TicketTradeAllocation> Allocations { get; init; } = [];

    protected TicketTradeEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, decimal tradedPrice, IReadOnlyList<TicketTradeAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        TradedPrice = tradedPrice;
        Allocations = allocations.ToList();
    }
}

public sealed record TicketTradeCreatedEvent : TicketTradeEventBase
{
    [JsonConstructor]
    private TicketTradeCreatedEvent() : this(null!, null!, null!, null!, string.Empty, null!, 0, []) { }

    internal TicketTradeCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, decimal tradedPrice, IReadOnlyList<TicketTradeAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, tradedPrice, allocations) { }

    public override string Type => nameof(TicketTradeCreatedEvent);
}

public sealed record TicketTradeModifiedEvent : TicketTradeEventBase
{
    [JsonConstructor]
    private TicketTradeModifiedEvent() : this(null!, null!, null!, null!, string.Empty, null!, 0, []) { }

    internal TicketTradeModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, decimal tradedPrice, IReadOnlyList<TicketTradeAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, tradedPrice, allocations) { }

    public override string Type => nameof(TicketTradeModifiedEvent);
}

public abstract record TicketTradeFillEventBase : TicketEventBase
{
    public Guid FillID { get; init; }
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public string Note { get; init; } = string.Empty;

    protected TicketTradeFillEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID, decimal price, decimal quantity, string note)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        FillID = fillID;
        Price = price;
        Quantity = quantity;
        Note = note ?? string.Empty;
    }
}

public sealed record TicketTradeFillAddedEvent : TicketTradeFillEventBase
{
    [JsonConstructor]
    private TicketTradeFillAddedEvent() : this(null!, null!, null!, null!, string.Empty, null!, Guid.Empty, 0, 0, string.Empty) { }

    internal TicketTradeFillAddedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID, decimal price, decimal quantity, string note)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, fillID, price, quantity, note) { }

    public override string Type => nameof(TicketTradeFillAddedEvent);
}

public sealed record TicketTradeFillModifiedEvent : TicketTradeFillEventBase
{
    [JsonConstructor]
    private TicketTradeFillModifiedEvent() : this(null!, null!, null!, null!, string.Empty, null!, Guid.Empty, 0, 0, string.Empty) { }

    internal TicketTradeFillModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID, decimal price, decimal quantity, string note)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, fillID, price, quantity, note) { }

    public override string Type => nameof(TicketTradeFillModifiedEvent);
}

public sealed record TicketTradeFillRemovedEvent : TicketEventBase
{
    public Guid FillID { get; init; }

    [JsonConstructor]
    private TicketTradeFillRemovedEvent() : this(null!, null!, null!, null!, string.Empty, null!, Guid.Empty) { }

    internal TicketTradeFillRemovedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        FillID = fillID;

    public override string Type => nameof(TicketTradeFillRemovedEvent);
}

public sealed record TicketTradeApprovedEvent : TicketEventBase
{
    [JsonConstructor]
    private TicketTradeApprovedEvent() : this(null!, null!, null!, null!, string.Empty, null!) { }

    internal TicketTradeApprovedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) { }

    public override string Type => nameof(TicketTradeApprovedEvent);
}

public sealed record TicketTradeNotApprovedEvent : TicketEventBase
{
    [JsonConstructor]
    private TicketTradeNotApprovedEvent() : this(null!, null!, null!, null!, string.Empty, null!) { }

    internal TicketTradeNotApprovedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) { }

    public override string Type => nameof(TicketTradeNotApprovedEvent);
}

public sealed record TicketCancelledEvent : TicketEventBase
{
    [JsonConstructor]
    private TicketCancelledEvent() : this(null!, null!, null!, null!, string.Empty, null!) { }

    internal TicketCancelledEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) { }

    public override string Type => nameof(TicketCancelledEvent);
}
