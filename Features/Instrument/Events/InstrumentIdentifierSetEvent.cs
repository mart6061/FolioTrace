using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIdentifierSetEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public InstrumentIdentifier Identifier { get; init; } = null!;

    [JsonConstructor]
    private InstrumentIdentifierSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentIdentifierSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, InstrumentIdentifier identifier)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Identifier = identifier;
    }

    public override string Type => nameof(InstrumentIdentifierSetEvent);
}
