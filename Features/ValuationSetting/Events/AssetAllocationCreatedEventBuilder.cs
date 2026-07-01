using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class AssetAllocationCreatedEventBuilder
{
    public static Result<AssetAllocationCreatedEvent> Create(AssetAllocationCreatedRequest request, ValuationSettings? valuationSettings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var assetAllocationID = request.AssetAllocationID ?? AssetAllocationIDBuilder.Create();
        var rootNodeID = request.RootNodeID ?? NodeIDBuilder.Create();

        return Create(
            request.UserID,
            assetAllocationID,
            request.Name,
            request.AccountIDs,
            request.Active,
            rootNodeID,
            request.Nodes,
            valuationSettings);
    }

    public static Result<AssetAllocationCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, bool active, NodeID rootNodeID, List<AssetAllocationNode> nodes, ValuationSettings? valuationSettings = null)
    {
        return Create(userId, assetAllocationID, name, accountIDs, active, rootNodeID, nodes, valuationSettings);
    }

    public static Result<AssetAllocationCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, EventDateTime? effectiveDateTime, string reason, AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, bool active, NodeID rootNodeID, List<AssetAllocationNode> nodes, ValuationSettings? valuationSettings = null)
    {
        return Create(userId, assetAllocationID, name, accountIDs, active, rootNodeID, nodes, valuationSettings);
    }

    public static Result<AssetAllocationCreatedEvent> Create(UserID userId, AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, bool active, NodeID rootNodeID, List<AssetAllocationNode> nodes, ValuationSettings? valuationSettings = null)
    {
        nodes = AssetAllocationNodeNormalizer.Normalise(rootNodeID, name, nodes);
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = AssetAllocationEventValidation.ValidateBase(eventId, userId, auditDateTime, assetAllocationID);
        AssetAllocationEventValidation.ValidateDefinition(validationErrors, name, accountIDs, rootNodeID, nodes);
        AssetAllocationEventValidation.ValidateCreatedAllocation(validationErrors, assetAllocationID, valuationSettings);

        return validationErrors.Count == 0
            ? Result<AssetAllocationCreatedEvent>.Success(new AssetAllocationCreatedEvent(eventId, userId, auditDateTime, assetAllocationID, name, accountIDs, active, rootNodeID, nodes))
            : Result<AssetAllocationCreatedEvent>.Invalid(validationErrors);
    }

    public static Result<AssetAllocationCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, bool active, NodeID rootNodeID, List<AssetAllocationNode> nodes, ValuationSettings? valuationSettings = null)
    {
        return CreateSeed(eventId, userId, eventDateTime, null, auditDateTime, reason, assetAllocationID, name, accountIDs, active, rootNodeID, nodes, valuationSettings);
    }

    public static Result<AssetAllocationCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, EventDateTime? effectiveDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, string name, List<AccountID> accountIDs, bool active, NodeID rootNodeID, List<AssetAllocationNode> nodes, ValuationSettings? valuationSettings = null)
    {
        nodes = AssetAllocationNodeNormalizer.Normalise(rootNodeID, name, nodes);
        var validationErrors = AssetAllocationEventValidation.ValidateBase(eventId, userId, auditDateTime, assetAllocationID);
        AssetAllocationEventValidation.ValidateDefinition(validationErrors, name, accountIDs, rootNodeID, nodes);
        AssetAllocationEventValidation.ValidateCreatedAllocation(validationErrors, assetAllocationID, valuationSettings);

        return validationErrors.Count == 0
            ? Result<AssetAllocationCreatedEvent>.Success(new AssetAllocationCreatedEvent(eventId, userId, auditDateTime, assetAllocationID, name, accountIDs, active, rootNodeID, nodes))
            : Result<AssetAllocationCreatedEvent>.Invalid(validationErrors);
    }
}
