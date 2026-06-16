using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Asset Allocation Active Set Event")]
public sealed record AssetAllocationActiveSetEvent : ConfigEventBase, IValuationSettingEvent
{
    [EventProperty(Description = "Asset Allocation ID")]
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    [EventProperty(Description = "Active")]
    public bool Active { get; init; }

    [JsonConstructor]
    private AssetAllocationActiveSetEvent()
        : base(null!, null!, null!)
    {
    }

    internal AssetAllocationActiveSetEvent(EventID eventId, UserID userId, AuditDateTime auditDateTime, AssetAllocationID assetAllocationID, bool active)
        : base(eventId, userId, auditDateTime)
    {
        AssetAllocationID = assetAllocationID;
        Active = active;
    }

    public override string Type => nameof(AssetAllocationActiveSetEvent);
}
