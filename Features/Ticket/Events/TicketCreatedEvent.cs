using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Ticket Created Event")]
public sealed record TicketCreatedEvent : TicketEventBase
{
    [EventProperty(Description = "Side")]
    public TicketSide Side { get; init; }
    [EventProperty(Description = "Instrument ID")]
    public InstrumentID InstrumentID { get; init; } = null!;
    [EventProperty(Description = "Trade Currency")]
    public Alpha3 TradeCurrency { get; init; } = null!;

    [JsonConstructor]
    private TicketCreatedEvent() : this(null!, null!, null!, null!, string.Empty, null!, default, null!, null!) { }

    internal TicketCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, TicketSide side, InstrumentID instrumentID, Alpha3 tradeCurrency)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        Side = side;
        InstrumentID = instrumentID;
        TradeCurrency = tradeCurrency;
    }

    public override string Type => nameof(TicketCreatedEvent);
}
