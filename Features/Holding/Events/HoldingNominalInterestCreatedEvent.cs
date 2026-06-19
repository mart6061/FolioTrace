using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Nominal Interest Created Event")]
public sealed record HoldingNominalInterestCreatedEvent : HoldingCreatedEvent
{
    [JsonConstructor]
    private HoldingNominalInterestCreatedEvent() { }

    internal HoldingNominalInterestCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault) { }

    public override string Type => nameof(HoldingNominalInterestCreatedEvent);
    internal override HoldingBase CreateHolding() => new HoldingNominalInterest(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime);
}
