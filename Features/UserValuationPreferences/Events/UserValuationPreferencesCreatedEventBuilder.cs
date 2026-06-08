using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserValuationPreferencesCreatedEventBuilder
{
    public static Result<UserValuationPreferencesCreatedEvent> Create(UserValuationPreferencesRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.ValuationDateOption, request.HoldingDateBasis, request.ShowZeroBalances);
    }

    public static Result<UserValuationPreferencesCreatedEvent> CreateDefault(UserCreatedEvent userCreatedEvent)
    {
        if (userCreatedEvent is null)
            throw new ArgumentNullException(nameof(userCreatedEvent));

        return Create(
            userCreatedEvent.UserID,
            userCreatedEvent.EventDateTime,
            $"Create default valuation preferences for {userCreatedEvent.DisplayName}",
            UserValuationPreferenceDefaults.ValuationDateOption,
            UserValuationPreferenceDefaults.HoldingDateBasis,
            UserValuationPreferenceDefaults.ShowZeroBalances);
    }

    public static Result<UserValuationPreferencesCreatedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, UserValuationDateOption valuationDateOption, HoldingDateBasis holdingDateBasis, bool showZeroBalances)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, valuationDateOption, holdingDateBasis, showZeroBalances);
    }

    public static Result<UserValuationPreferencesCreatedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, UserValuationDateOption valuationDateOption, HoldingDateBasis holdingDateBasis, bool showZeroBalances)
    {
        var validationErrors = UserValuationPreferencesEventValidation.Validate(eventID, userID, eventDateTime, auditDateTime, reason, valuationDateOption, holdingDateBasis);

        return validationErrors.Count == 0
            ? Result<UserValuationPreferencesCreatedEvent>.Success(new UserValuationPreferencesCreatedEvent(eventID, userID, eventDateTime, auditDateTime, reason, valuationDateOption, holdingDateBasis, showZeroBalances))
            : Result<UserValuationPreferencesCreatedEvent>.Invalid(validationErrors);
    }
}
