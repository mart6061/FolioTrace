using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Asset Allocation Created Event")]
public sealed record AssetAllocationCreatedEvent : EventBase, IValuationSettingEvent
{
    [EventProperty(Description = "Asset Allocation ID")]
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;

    [EventProperty(Description = "Account I Ds")]
    public List<AccountID> AccountIDs { get; init; } = [];

    [EventProperty(Description = "Active")]
    public bool Active { get; init; }

    [EventProperty(Description = "Root Node ID")]
    public NodeID RootNodeID { get; init; } = null!;

    [EventProperty(Description = "Nodes")]
    public List<AssetAllocationNode> Nodes { get; init; } = [];

    [JsonConstructor]
    private AssetAllocationCreatedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal AssetAllocationCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, bool active, NodeID rootNodeID, List<AssetAllocationNode> nodes)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AssetAllocationID = assetAllocationID;
        Name = name?.Trim() ?? string.Empty;
        AccountIDs = ValuationSettingBuilder.CloneAccountIDs(accountIDs);
        Active = active;
        RootNodeID = rootNodeID;
        Nodes = ValuationSettingBuilder.CloneNodes(nodes);
    }

    public override string Type => nameof(AssetAllocationCreatedEvent);
}
