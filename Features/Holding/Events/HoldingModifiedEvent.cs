using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record HoldingModifiedEvent : EventBase, IHoldingEvent
{
    public HoldingID HoldingID { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public bool Default { get; init; }

    protected HoldingModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal HoldingModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        HoldingID = holdingID;
        Name = name?.Trim() ?? string.Empty;
        Default = isDefault;
    }

    internal abstract Holding Apply(Holding holding);
}
