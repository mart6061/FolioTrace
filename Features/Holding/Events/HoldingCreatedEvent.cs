using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Created Event")]
public abstract record HoldingCreatedEvent : EventBase, IHoldingEvent
{
    [EventProperty(Description = "Holding ID")]
    public HoldingID HoldingID { get; init; } = null!;
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Instrument ID")]
    public InstrumentID InstrumentID { get; init; } = null!;
    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;
    [EventProperty(Description = "Active")]
    public Active Active { get; init; } = false;
    [EventProperty(Description = "Default")]
    public bool Default { get; init; }

    protected HoldingCreatedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal HoldingCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        HoldingID = holdingID;
        AccountID = accountID;
        InstrumentID = instrumentID;
        Name = name?.Trim() ?? string.Empty;
        Active = active;
        Default = isDefault;
    }

    internal abstract HoldingBase CreateHolding();
}
