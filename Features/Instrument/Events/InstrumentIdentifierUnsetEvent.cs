using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIdentifierUnsetEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
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
