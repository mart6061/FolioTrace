using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record HoldingCreatedEvent : EventBase, IHoldingEvent
{
    public HoldingID HoldingID { get; init; } = null!;
    public AccountID AccountID { get; init; } = null!;
    public InstrumentID InstrumentID { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public Active Active { get; init; } = false;
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

    internal abstract Holding CreateHolding();
}
