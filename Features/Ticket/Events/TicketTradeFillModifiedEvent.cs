using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Trade Fill Modified Event")]
public sealed record TicketTradeFillModifiedEvent : TicketTradeFillEventBase
{
    [JsonConstructor]
    private TicketTradeFillModifiedEvent() : this(null!, null!, null!, null!, string.Empty, null!, Guid.Empty, null!, null!, 0, null!, string.Empty) { }

    internal TicketTradeFillModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID, LegalEntityIdentifier brokerLEI, Price price, decimal quantity, TransactionLocalCost settlementAmount, string note, TransactionBookCost? bookCostOverride = null)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, fillID, brokerLEI, price, quantity, settlementAmount, note, bookCostOverride) { }

    public override string Type => nameof(TicketTradeFillModifiedEvent);
}
