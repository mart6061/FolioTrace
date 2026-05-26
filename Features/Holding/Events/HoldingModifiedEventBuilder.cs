using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingModifiedEventBuilder
{
    public static Result<HoldingModifiedEvent> Create(HoldingModifiedRequest request, Holdings? holdings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.HoldingID, request.NominalType, request.Name, request.Default, holdings);
    }

    public static Result<HoldingModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, HoldingID holdingID, HoldingNominalType? nominalType, string name, bool isDefault, Holdings? holdings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, nominalType, name, isDefault, holdings);
    }

    public static Result<HoldingModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, HoldingNominalType? nominalType, string name, bool isDefault, Holdings? holdings = null)
    {
        var validationErrors = HoldingEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, holdingID);
        var existingHolding = holdings?.Items.SingleOrDefault(holding => holding.HoldingID == holdingID);
        var holdingType = existingHolding?.HoldingType ?? HoldingType.Position;
        HoldingEventValidation.ValidateDefinition(validationErrors, holdingType, nominalType, name, isDefault);
        HoldingEventValidation.ValidateModifiedHolding(validationErrors, holdingID, isDefault, holdings);

        return validationErrors.Count == 0
            ? Result<HoldingModifiedEvent>.Success(new HoldingModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, nominalType, name, isDefault))
            : Result<HoldingModifiedEvent>.Invalid(validationErrors);
    }
}
