using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class AssetAllocationActiveSetEventBuilder
{
    public static Result<AssetAllocationActiveSetEvent> Create(AssetAllocationActiveSetRequest request, ValuationSettings? valuationSettings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.AssetAllocationID, request.Active, valuationSettings);
    }

    public static Result<AssetAllocationActiveSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AssetAllocationID assetAllocationID, bool active, ValuationSettings? valuationSettings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = AssetAllocationEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, assetAllocationID);
        AssetAllocationEventValidation.ValidateExistingAllocation(validationErrors, assetAllocationID, valuationSettings);

        return validationErrors.Count == 0
            ? Result<AssetAllocationActiveSetEvent>.Success(new AssetAllocationActiveSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, assetAllocationID, active))
            : Result<AssetAllocationActiveSetEvent>.Invalid(validationErrors);
    }
}
