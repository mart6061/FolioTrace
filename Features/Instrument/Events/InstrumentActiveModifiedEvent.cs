using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentActiveModifiedEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public Active Active { get; init; } = false;

    [JsonConstructor]
    private InstrumentActiveModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentActiveModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, Active active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Active = active;
    }

    public override string Type => nameof(InstrumentActiveModifiedEvent);
}
