using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Instrument Identifier Unset Event")]
public sealed record InstrumentIdentifierUnsetEvent : EventBase, IInstrumentEvent
{
    [EventProperty(Description = "Instrument ID")]
    public InstrumentID InstrumentID { get; init; } = null!;
    [EventProperty(Description = "Identifier Type")]
    public InstrumentIdentifierType IdentifierType { get; init; }

    [JsonConstructor]
    private InstrumentIdentifierUnsetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentIdentifierUnsetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, InstrumentIdentifierType identifierType)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        IdentifierType = identifierType;
    }

    public override string Type => nameof(InstrumentIdentifierUnsetEvent);
}
