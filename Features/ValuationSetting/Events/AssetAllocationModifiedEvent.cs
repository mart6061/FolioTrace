using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationModifiedEvent : EventBase, IValuationSettingEvent
{
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    public string Name { get; init; } = string.Empty;

    public NodeID RootNodeID { get; init; } = null!;

    public List<AssetAllocationNode> Nodes { get; init; } = [];

    [JsonConstructor]
    private AssetAllocationModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal AssetAllocationModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, string name, NodeID rootNodeID, List<AssetAllocationNode> nodes)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AssetAllocationID = assetAllocationID;
        Name = name?.Trim() ?? string.Empty;
        RootNodeID = rootNodeID;
        Nodes = ValuationSettingBuilder.CloneNodes(nodes);
    }

    public override string Type => nameof(AssetAllocationModifiedEvent);
}
