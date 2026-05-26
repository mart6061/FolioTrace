using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentTermsSetEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public IInstrumentTerms Terms { get; init; } = null!;

    [JsonConstructor]
    private InstrumentTermsSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentTermsSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentTerms terms)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Terms = terms;
    }

    public override string Type => nameof(InstrumentTermsSetEvent);
}
