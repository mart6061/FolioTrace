using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class AssetAllocationMappingEventBuilder
{
    public static Result<AssetAllocationMappingSetEvent> Create(AssetAllocationMappingRequest request, ValuationSettings? valuationSettings, Holdings? holdings)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(
            request.UserID,
            request.EventDateTime,
            request.Reason,
            request.AssetAllocationID,
            request.HoldingID,
            request.NodeID,
            valuationSettings,
            holdings);
    }

    public static Result<AssetAllocationMappingSetEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, AssetAllocationID assetAllocationID, HoldingID holdingID, NodeID nodeID, ValuationSettings? valuationSettings, Holdings? holdings)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        var validationErrors = Validate(eventID, userID, eventDateTime, auditDateTime, reason, assetAllocationID, holdingID, nodeID, valuationSettings, holdings);

        return validationErrors.Count == 0
            ? Result<AssetAllocationMappingSetEvent>.Success(new AssetAllocationMappingSetEvent(eventID, userID, eventDateTime, auditDateTime, reason, assetAllocationID, holdingID, nodeID))
            : Result<AssetAllocationMappingSetEvent>.Invalid(validationErrors);
    }

    private static List<string> Validate(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, AssetAllocationID? assetAllocationID, HoldingID? holdingID, NodeID? nodeID, ValuationSettings? valuationSettings, Holdings? holdings)
    {
        var messages = AssetAllocationEventValidation.ValidateBase(eventID, userID, eventDateTime, auditDateTime, reason, assetAllocationID);

        if (holdingID is null)
            messages.Add("HoldingID is required.");

        if (nodeID is null)
            messages.Add("NodeID is required.");

        var allocation = assetAllocationID is null
            ? null
            : valuationSettings?.Items.SingleOrDefault(item => item.AssetAllocationID == assetAllocationID);

        if (allocation is null)
        {
            if (assetAllocationID is not null)
                messages.Add($"No matching asset allocation found for AssetAllocationID '{assetAllocationID}'.");
        }
        else
        {
            if (!allocation.Active)
                messages.Add($"Asset allocation '{assetAllocationID}' is inactive.");

            var node = nodeID is null
                ? null
                : allocation.Nodes.SingleOrDefault(item => item.NodeID == nodeID);

            if (node is null)
            {
                if (nodeID is not null)
                    messages.Add($"No matching node found for NodeID '{nodeID}' in AssetAllocationID '{assetAllocationID}'.");
            }
            else if (node.Nodes.Count > 0)
            {
                messages.Add($"NodeID '{nodeID}' has child nodes and cannot be used for holding mappings.");
            }
        }

        var holding = holdingID is null
            ? null
            : holdings?.Items.SingleOrDefault(item => item.HoldingID == holdingID);

        if (holding is null)
        {
            if (holdingID is not null)
                messages.Add($"No matching holding found for HoldingID '{holdingID}'.");
        }
        else
        {
            if (!holding.Active)
                messages.Add($"HoldingID '{holdingID}' is inactive.");

            if (!holding.IncludeInValuation)
                messages.Add($"HoldingID '{holdingID}' is not included in valuation.");

            if (allocation is not null && !allocation.AccountIDs.Contains(holding.AccountID))
                messages.Add($"HoldingID '{holdingID}' belongs to AccountID '{holding.AccountID}' which is not assigned to AssetAllocationID '{assetAllocationID}'.");
        }

        return messages;
    }
}
