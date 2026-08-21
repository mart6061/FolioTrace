using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Position Option Created Event")]
public sealed record HoldingPositionOptionCreatedEvent : HoldingCreatedEvent
{
    [JsonConstructor]
    private HoldingPositionOptionCreatedEvent() { }

    internal HoldingPositionOptionCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault) { }

    public override string Type => nameof(HoldingPositionOptionCreatedEvent);
    internal override HoldingBase CreateHolding() => new HoldingPositionOption(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime);
}
