using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ValuationSetting : IModel
{
    public required AssetAllocationID AssetAllocationID { get; init; }

    public required string Name { get; init; }

    public required List<AccountID> AccountIDs { get; init; }

    public required bool Active { get; init; }

    public required NodeID RootNodeID { get; init; }

    public required List<AssetAllocationNode> Nodes { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required EventID LastEventID { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public ValuationSetting(AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, bool active, NodeID rootNodeID, List<AssetAllocationNode> nodes, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        AssetAllocationID = assetAllocationID;
        Name = name;
        AccountIDs = accountIDs;
        Active = active;
        RootNodeID = rootNodeID;
        Nodes = nodes;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public ValuationSetting(AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, bool active, NodeID rootNodeID, List<AssetAllocationNode> nodes, EventDateTime valuationDateTime, AuditDateTime auditDateTime, EventID lastEventID)
        : this(assetAllocationID, name, accountIDs, active, rootNodeID, nodes, valuationDateTime, auditDateTime, lastEventID, new LastAuditDateTime(auditDateTime.Value))
    {
    }

    public override string ToString() => Name;
}
