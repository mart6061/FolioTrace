using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Nominal In Specie In Created Event")]
public sealed record HoldingNominalInSpecieInCreatedEvent : HoldingCreatedEvent
{
    [JsonConstructor]
    private HoldingNominalInSpecieInCreatedEvent() { }

    internal HoldingNominalInSpecieInCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault) { }

    public override string Type => nameof(HoldingNominalInSpecieInCreatedEvent);
    internal override HoldingBase CreateHolding() => new HoldingNominalInSpecieIn(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime);
}
