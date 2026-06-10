using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Asset Allocation Active Set Event")]
public sealed record AssetAllocationActiveSetEvent : EventBase, IValuationSettingEvent
{
    [EventProperty(Description = "Asset Allocation ID")]
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    [EventProperty(Description = "Active")]
    public bool Active { get; init; }

    [JsonConstructor]
    private AssetAllocationActiveSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal AssetAllocationActiveSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, bool active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AssetAllocationID = assetAllocationID;
        Active = active;
    }

    public override string Type => nameof(AssetAllocationActiveSetEvent);
}
