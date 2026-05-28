using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingInspecieOutModifiedEvent : HoldingModifiedEvent
{
    [JsonConstructor]
    private HoldingInspecieOutModifiedEvent() { }

    internal HoldingInspecieOutModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault) { }

    public override string Type => nameof(HoldingInspecieOutModifiedEvent);
    internal override Holding Apply(Holding holding) =>
        holding is HoldingInspecieOut existing
            ? existing with { Name = Name, Default = Default, ValuationDateTime = EventDateTime, AsOfDateTime = AuditDateTime, LastEventID = EventID, LastAuditDateTime = AuditDateTime }
            : throw new InvalidOperationException($"HoldingID '{HoldingID}' is not a {this.GetHoldingKindName()} holding.");
}
