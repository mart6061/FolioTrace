using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Trade Modified Event")]
public sealed record TicketTradeModifiedEvent : TicketTradeEventBase
{
    [JsonConstructor]
    private TicketTradeModifiedEvent() : this(null!, null!, null!, null!, string.Empty, null!, null!, null!, null!, []) { }

    internal TicketTradeModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Price tradedPrice, EventDateTime tradeDateTime, SettlementDateTime settlementDateTime, IReadOnlyList<TicketTradeAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, tradedPrice, tradeDateTime, settlementDateTime, allocations) { }

    public override string Type => nameof(TicketTradeModifiedEvent);
}
