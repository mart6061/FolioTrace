using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingInSpecieOutCreatedEvent : HoldingCreatedEvent
{
    [JsonConstructor]
    private HoldingInSpecieOutCreatedEvent() { }

    internal HoldingInSpecieOutCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault) { }

    public override string Type => nameof(HoldingInSpecieOutCreatedEvent);
    internal override Holding CreateHolding() => new HoldingInSpecieOut(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime);
}
