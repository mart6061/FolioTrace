using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketCreatedEvent : TicketEventBase
{
    public TicketSide Side { get; init; }
    public InstrumentID InstrumentID { get; init; } = null!;
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
