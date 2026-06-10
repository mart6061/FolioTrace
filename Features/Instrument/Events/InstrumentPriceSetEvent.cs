using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Instrument Price Set Event")]
public sealed record InstrumentPriceSetEvent : EventBase, IInstrumentPriceEvent
{
    [EventProperty(Description = "Instrument ID")]
    public InstrumentID InstrumentID { get; init; } = null!;
    [EventProperty(Description = "Price")]
    public IInstrumentPrice Price { get; init; } = null!;

    [JsonConstructor]
    private InstrumentPriceSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentPriceSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentPrice price)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Price = price;
    }

    public override string Type => nameof(InstrumentPriceSetEvent);
}
