using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Asset Allocation Modified Event")]
public sealed record AssetAllocationModifiedEvent : ConfigEventBase, IValuationSettingEvent
{
    [EventProperty(Description = "Asset Allocation ID")]
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;

    [EventProperty(Description = "Root Node ID")]
    public NodeID RootNodeID { get; init; } = null!;

    [EventProperty(Description = "Nodes")]
    public List<AssetAllocationNode> Nodes { get; init; } = [];

    [JsonConstructor]
    private AssetAllocationModifiedEvent()
        : base(null!, null!, null!)
    {
    }

    internal AssetAllocationModifiedEvent(EventID eventId, UserID userId, AuditDateTime auditDateTime, AssetAllocationID assetAllocationID, string name, NodeID rootNodeID, List<AssetAllocationNode> nodes)
        : base(eventId, userId, auditDateTime)
    {
        AssetAllocationID = assetAllocationID;
        Name = name?.Trim() ?? string.Empty;
        RootNodeID = rootNodeID;
        Nodes = ValuationSettingBuilder.CloneNodes(nodes);
    }

    public override string Type => nameof(AssetAllocationModifiedEvent);
}
