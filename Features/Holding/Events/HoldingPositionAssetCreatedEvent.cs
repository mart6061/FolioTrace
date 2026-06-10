using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Position Asset Created Event")]
public sealed record HoldingPositionAssetCreatedEvent : HoldingCreatedEvent
{
    [JsonConstructor]
    private HoldingPositionAssetCreatedEvent() { }

    internal HoldingPositionAssetCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault) { }

    public override string Type => nameof(HoldingPositionAssetCreatedEvent);
    internal override Holding CreateHolding() => new HoldingPositionAsset(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime);
}
