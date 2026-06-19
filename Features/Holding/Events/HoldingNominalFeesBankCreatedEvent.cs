using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Nominal Fees Bank Created Event")]
public sealed record HoldingNominalFeesBankCreatedEvent : HoldingCreatedEvent
{
    [JsonConstructor]
    private HoldingNominalFeesBankCreatedEvent() { }

    internal HoldingNominalFeesBankCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault) { }

    public override string Type => nameof(HoldingNominalFeesBankCreatedEvent);
    internal override HoldingBase CreateHolding() => new HoldingNominalFeesBank(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime);
}
