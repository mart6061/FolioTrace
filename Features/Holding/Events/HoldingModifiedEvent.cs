using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingModifiedEvent : EventBase, IHoldingEvent
{
    public HoldingID HoldingID { get; init; } = null!;
    public HoldingNominalType? NominalType { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Default { get; init; }

    [JsonConstructor]
    private HoldingModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal HoldingModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, HoldingNominalType? nominalType, string name, bool isDefault)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        HoldingID = holdingID;
        NominalType = nominalType;
        Name = name.Trim();
        Default = isDefault;
    }

    public override string Type => nameof(HoldingModifiedEvent);
}
