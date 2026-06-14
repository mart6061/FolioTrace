using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class AssetAllocationModifiedEventBuilder
{
    public static Result<AssetAllocationModifiedEvent> Create(AssetAllocationModifiedRequest request, ValuationSettings? valuationSettings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var accountIDs = valuationSettings?.Items.SingleOrDefault(item => item.AssetAllocationID == request.AssetAllocationID)?.AccountIDs ?? [];

        return Create(
            request.UserID,
            request.EventDateTime,
            request.EffectiveDateTime,
            request.Reason,
            request.AssetAllocationID,
            request.Name,
            accountIDs,
            request.RootNodeID,
            request.Nodes,
            valuationSettings);
    }

    public static Result<AssetAllocationModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, NodeID rootNodeID, List<AssetAllocationNode> nodes, ValuationSettings? valuationSettings = null)
    {
        return Create(userId, eventDateTime, ValuationSettingBuilder.DateOnly(eventDateTime), reason, assetAllocationID, name, accountIDs, rootNodeID, nodes, valuationSettings);
    }

    public static Result<AssetAllocationModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, EventDateTime? effectiveDateTime, string reason, AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, NodeID rootNodeID, List<AssetAllocationNode> nodes, ValuationSettings? valuationSettings = null)
    {
        nodes = AssetAllocationNodeNormalizer.Normalise(rootNodeID, name, nodes);
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = AssetAllocationEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, assetAllocationID);
        AssetAllocationEventValidation.ValidateEffectiveDateTime(validationErrors, effectiveDateTime);
        effectiveDateTime = effectiveDateTime is null ? null : ValuationSettingBuilder.DateOnly(effectiveDateTime);
        AssetAllocationEventValidation.ValidateExistingAllocation(validationErrors, assetAllocationID, valuationSettings);
        AssetAllocationEventValidation.ValidateDefinition(validationErrors, name, accountIDs, rootNodeID, nodes);

        return validationErrors.Count == 0
            ? Result<AssetAllocationModifiedEvent>.Success(new AssetAllocationModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, assetAllocationID, name, rootNodeID, nodes, effectiveDateTime!))
            : Result<AssetAllocationModifiedEvent>.Invalid(validationErrors);
    }
}
