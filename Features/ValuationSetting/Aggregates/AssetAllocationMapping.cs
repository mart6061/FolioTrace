using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationMapping : IModel
{
    public required AssetAllocationID AssetAllocationID { get; init; }
    public required HoldingID HoldingID { get; init; }
    public required NodeID NodeID { get; init; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public AssetAllocationMapping(AssetAllocationID assetAllocationID, HoldingID holdingID, NodeID nodeID, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        AssetAllocationID = assetAllocationID ?? throw new ArgumentNullException(nameof(assetAllocationID));
        HoldingID = holdingID ?? throw new ArgumentNullException(nameof(holdingID));
        NodeID = nodeID ?? throw new ArgumentNullException(nameof(nodeID));
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        LastEventID = lastEventID ?? throw new ArgumentNullException(nameof(lastEventID));
        LastAuditDateTime = lastAuditDateTime ?? throw new ArgumentNullException(nameof(lastAuditDateTime));
    }
}
