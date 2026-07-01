using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class UserValuationPreferencesCreatedEventBuilder
{
    public static Result<UserValuationPreferencesCreatedEvent> Create(UserValuationPreferencesRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(
            request.UserID,
            request.EventDateTime,
            request.Reason,
            request.StartValuationDateOption ?? UserValuationPreferenceDefaults.StartValuationDateOption,
            request.EndValuationDateOption ?? request.ValuationDateOption,
            request.HoldingDateBasis,
            request.ShowZeroBalances);
    }

    public static Result<UserValuationPreferencesCreatedEvent> CreateDefault(UserCreatedEvent userCreatedEvent)
    {
        if (userCreatedEvent is null)
            throw new ArgumentNullException(nameof(userCreatedEvent));

        return Create(
            userCreatedEvent.UserID,
            userCreatedEvent.EventDateTime,
            $"Create default valuation preferences for {userCreatedEvent.DisplayName}",
            UserValuationPreferenceDefaults.StartValuationDateOption,
            UserValuationPreferenceDefaults.EndValuationDateOption,
            UserValuationPreferenceDefaults.HoldingDateBasis,
            UserValuationPreferenceDefaults.ShowZeroBalances);
    }

    public static Result<UserValuationPreferencesCreatedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, UserValuationDateOption valuationDateOption, HoldingDateBasis holdingDateBasis, bool showZeroBalances)
    {
        return Create(
            userID,
            eventDateTime,
            reason,
            UserValuationPreferenceDefaults.StartValuationDateOption,
            valuationDateOption,
            holdingDateBasis,
            showZeroBalances);
    }

    public static Result<UserValuationPreferencesCreatedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, UserValuationDateOption startValuationDateOption, UserValuationDateOption endValuationDateOption, HoldingDateBasis holdingDateBasis, bool showZeroBalances)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, startValuationDateOption, endValuationDateOption, holdingDateBasis, showZeroBalances);
    }

    public static Result<UserValuationPreferencesCreatedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, UserValuationDateOption valuationDateOption, HoldingDateBasis holdingDateBasis, bool showZeroBalances)
    {
        return CreateSeed(
            eventID,
            userID,
            eventDateTime,
            auditDateTime,
            reason,
            UserValuationPreferenceDefaults.StartValuationDateOption,
            valuationDateOption,
            holdingDateBasis,
            showZeroBalances);
    }

    public static Result<UserValuationPreferencesCreatedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, UserValuationDateOption startValuationDateOption, UserValuationDateOption endValuationDateOption, HoldingDateBasis holdingDateBasis, bool showZeroBalances)
    {
        var validationErrors = UserValuationPreferencesEventValidation.Validate(eventID, userID, eventDateTime, auditDateTime, reason, startValuationDateOption, endValuationDateOption, holdingDateBasis);

        return validationErrors.Count == 0
            ? Result<UserValuationPreferencesCreatedEvent>.Success(new UserValuationPreferencesCreatedEvent(eventID, userID, eventDateTime, auditDateTime, reason, startValuationDateOption, endValuationDateOption, holdingDateBasis, showZeroBalances))
            : Result<UserValuationPreferencesCreatedEvent>.Invalid(validationErrors);
    }
}
