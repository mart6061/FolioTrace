using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingActiveModifiedEventBuilder
{
    public static Result<HoldingActiveModifiedEvent> Create(HoldingActiveModifiedRequest request, Holdings? holdings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.HoldingID, request.Active, holdings);
    }

    public static Result<HoldingActiveModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, HoldingID holdingID, Active active, Holdings? holdings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, active, holdings);
    }

    public static Result<HoldingActiveModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, Active active, Holdings? holdings = null)
    {
        var validationErrors = HoldingEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, holdingID);
        HoldingEventValidation.ValidateActiveHolding(validationErrors, holdingID, holdings);

        return validationErrors.Count == 0
            ? Result<HoldingActiveModifiedEvent>.Success(new HoldingActiveModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, active))
            : Result<HoldingActiveModifiedEvent>.Invalid(validationErrors);
    }
}
