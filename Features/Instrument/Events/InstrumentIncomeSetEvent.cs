using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Instrument Income Set Event")]
public sealed record InstrumentIncomeSetEvent : EventBase, IInstrumentIncomeEvent
{
    [EventProperty(Description = "Instrument ID")]
    public InstrumentID InstrumentID { get; init; } = null!;
    [EventProperty(Description = "Income")]
    public IInstrumentIncome Income { get; init; } = null!;

    [JsonConstructor]
    private InstrumentIncomeSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentIncomeSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentIncome income)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Income = income;
    }

    public override string Type => nameof(InstrumentIncomeSetEvent);
}
