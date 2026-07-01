using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class AssetAllocationAccountIDsSetEventBuilder
{
    public static Result<AssetAllocationAccountIDsSetEvent> Create(AssetAllocationAccountIDsSetRequest request, ValuationSettings? valuationSettings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.AssetAllocationID, request.AccountIDs, valuationSettings);
    }

    public static Result<AssetAllocationAccountIDsSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AssetAllocationID assetAllocationID, List<AccountID> accountIDs, ValuationSettings? valuationSettings = null)
    {
        return Create(userId, assetAllocationID, accountIDs, valuationSettings);
    }

    public static Result<AssetAllocationAccountIDsSetEvent> Create(UserID userId, AssetAllocationID assetAllocationID, List<AccountID> accountIDs, ValuationSettings? valuationSettings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = AssetAllocationEventValidation.ValidateBase(eventId, userId, auditDateTime, assetAllocationID);
        AssetAllocationEventValidation.ValidateExistingAllocation(validationErrors, assetAllocationID, valuationSettings);
        AssetAllocationEventValidation.ValidateAccountIDs(validationErrors, accountIDs);

        return validationErrors.Count == 0
            ? Result<AssetAllocationAccountIDsSetEvent>.Success(new AssetAllocationAccountIDsSetEvent(eventId, userId, auditDateTime, assetAllocationID, accountIDs))
            : Result<AssetAllocationAccountIDsSetEvent>.Invalid(validationErrors);
    }
}
