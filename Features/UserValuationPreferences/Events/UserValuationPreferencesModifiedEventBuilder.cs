using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserValuationPreferencesModifiedEventBuilder
{
    public static Result<UserValuationPreferencesModifiedEvent> Create(UserValuationPreferencesRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.ValuationDateOption, request.ValuationDateBasis, request.ShowZeroBalances);
    }

    public static Result<UserValuationPreferencesModifiedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, UserValuationDateOption valuationDateOption, ValuationDateBasis valuationDateBasis, bool showZeroBalances)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.NewGuid();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, valuationDateOption, valuationDateBasis, showZeroBalances);
    }

    public static Result<UserValuationPreferencesModifiedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, UserValuationDateOption valuationDateOption, ValuationDateBasis valuationDateBasis, bool showZeroBalances)
    {
        var validationErrors = UserValuationPreferencesEventValidation.Validate(eventID, userID, eventDateTime, auditDateTime, reason, valuationDateOption, valuationDateBasis);

        return validationErrors.Count == 0
            ? Result<UserValuationPreferencesModifiedEvent>.Success(new UserValuationPreferencesModifiedEvent(eventID, userID, eventDateTime, auditDateTime, reason, valuationDateOption, valuationDateBasis, showZeroBalances))
            : Result<UserValuationPreferencesModifiedEvent>.Invalid(validationErrors);
    }
}
