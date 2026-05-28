using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingFeesAdministratorCreatedEvent : HoldingCreatedEvent
{
    [JsonConstructor]
    private HoldingFeesAdministratorCreatedEvent() { }

    internal HoldingFeesAdministratorCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault) { }

    public override string Type => nameof(HoldingFeesAdministratorCreatedEvent);
    internal override Holding CreateHolding() => new HoldingFeesAdministrator(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime);
}
