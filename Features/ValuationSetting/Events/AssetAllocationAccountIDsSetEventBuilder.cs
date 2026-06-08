using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class AssetAllocationAccountIDsSetEventBuilder
{
    public static Result<AssetAllocationAccountIDsSetEvent> Create(AssetAllocationAccountIDsSetRequest request, ValuationSettings? valuationSettings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.AssetAllocationID, request.AccountIDs, valuationSettings);
    }

    public static Result<AssetAllocationAccountIDsSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AssetAllocationID assetAllocationID, List<AccountID> accountIDs, ValuationSettings? valuationSettings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = AssetAllocationEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, assetAllocationID);
        AssetAllocationEventValidation.ValidateExistingAllocation(validationErrors, assetAllocationID, valuationSettings);
        AssetAllocationEventValidation.ValidateAccountIDs(validationErrors, accountIDs);

        return validationErrors.Count == 0
            ? Result<AssetAllocationAccountIDsSetEvent>.Success(new AssetAllocationAccountIDsSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, assetAllocationID, accountIDs))
            : Result<AssetAllocationAccountIDsSetEvent>.Invalid(validationErrors);
    }
}
