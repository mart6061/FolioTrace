using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class AssetAllocationActiveSetEventBuilder
{
    public static Result<AssetAllocationActiveSetEvent> Create(AssetAllocationActiveSetRequest request, ValuationSettings? valuationSettings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.AssetAllocationID, request.Active, valuationSettings);
    }

    public static Result<AssetAllocationActiveSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AssetAllocationID assetAllocationID, bool active, ValuationSettings? valuationSettings = null)
    {
        return Create(userId, assetAllocationID, active, valuationSettings);
    }

    public static Result<AssetAllocationActiveSetEvent> Create(UserID userId, AssetAllocationID assetAllocationID, bool active, ValuationSettings? valuationSettings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = AssetAllocationEventValidation.ValidateBase(eventId, userId, auditDateTime, assetAllocationID);
        AssetAllocationEventValidation.ValidateExistingAllocation(validationErrors, assetAllocationID, valuationSettings);

        return validationErrors.Count == 0
            ? Result<AssetAllocationActiveSetEvent>.Success(new AssetAllocationActiveSetEvent(eventId, userId, auditDateTime, assetAllocationID, active))
            : Result<AssetAllocationActiveSetEvent>.Invalid(validationErrors);
    }
}
