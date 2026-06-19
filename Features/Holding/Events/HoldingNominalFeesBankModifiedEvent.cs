using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Holding Nominal Fees Bank Modified Event")]
public sealed record HoldingNominalFeesBankModifiedEvent : HoldingModifiedEvent
{
    [JsonConstructor]
    private HoldingNominalFeesBankModifiedEvent() { }

    internal HoldingNominalFeesBankModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault) { }

    public override string Type => nameof(HoldingNominalFeesBankModifiedEvent);
    internal override HoldingBase Apply(HoldingBase holding) =>
        holding is HoldingNominalFeesBank existing
            ? existing with { Name = Name, Default = Default, ValuationDateTime = EventDateTime, AsOfDateTime = AuditDateTime, LastEventID = EventID, LastAuditDateTime = AuditDateTime }
            : throw new InvalidOperationException($"HoldingID '{HoldingID}' is not a {this.GetHoldingKindName()} holding.");
}
