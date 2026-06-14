using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Asset Allocation Mapping Set Event")]
public sealed record AssetAllocationMappingSetEvent : EventBase, IAssetAllocationMappingEvent
{
    [EventProperty(Description = "Asset Allocation ID")]
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    [EventProperty(Description = "Holding ID")]
    public HoldingID HoldingID { get; init; } = null!;

    [EventProperty(Description = "Node ID")]
    public NodeID NodeID { get; init; } = null!;

    [JsonConstructor]
    private AssetAllocationMappingSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal AssetAllocationMappingSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, HoldingID holdingID, NodeID nodeID)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AssetAllocationID = assetAllocationID;
        HoldingID = holdingID;
        NodeID = nodeID;
    }

    public override string Type => nameof(AssetAllocationMappingSetEvent);
}
