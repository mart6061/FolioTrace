using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Asset Allocation Modified Event")]
public sealed record AssetAllocationModifiedEvent : EventBase, IValuationSettingEvent
{
    [EventProperty(Description = "Asset Allocation ID")]
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;

    [EventProperty(Description = "Root Node ID")]
    public NodeID RootNodeID { get; init; } = null!;

    [EventProperty(Description = "Nodes")]
    public List<AssetAllocationNode> Nodes { get; init; } = [];

    [EventProperty(Description = "Effective Date Time")]
    public EventDateTime? EffectiveDateTime { get; init; }

    [JsonConstructor]
    private AssetAllocationModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal AssetAllocationModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, string name, NodeID rootNodeID, List<AssetAllocationNode> nodes, EventDateTime effectiveDateTime)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AssetAllocationID = assetAllocationID;
        Name = name?.Trim() ?? string.Empty;
        RootNodeID = rootNodeID;
        Nodes = ValuationSettingBuilder.CloneNodes(nodes);
        EffectiveDateTime = ValuationSettingBuilder.DateOnly(effectiveDateTime);
    }

    public override string Type => nameof(AssetAllocationModifiedEvent);
}
