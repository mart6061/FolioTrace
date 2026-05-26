using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingCreatedEventBuilder
{
    public static Result<HoldingCreatedEvent> Create(HoldingCreatedRequest request, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.HoldingID ?? HoldingIDBuilder.Create(), request.AccountID, request.InstrumentID, request.HoldingType, request.NominalType, request.Name, request.Active, request.Default, accounts, instruments, holdings);
    }

    public static Result<HoldingCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, HoldingType holdingType, HoldingNominalType? nominalType, string name, bool active, bool isDefault, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, holdingType, nominalType, name, active, isDefault, accounts, instruments, holdings);
    }

    public static Result<HoldingCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, HoldingType holdingType, HoldingNominalType? nominalType, string name, bool active, bool isDefault, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null)
    {
        var validationErrors = HoldingCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, holdingType, nominalType, name, isDefault);
        HoldingEventValidation.ValidateReferences(validationErrors, accountID, instrumentID, accounts, instruments);
        HoldingEventValidation.ValidateCreatedHolding(validationErrors, holdingID, accountID, instrumentID, holdingType, isDefault, holdings);

        return validationErrors.Count == 0
            ? Result<HoldingCreatedEvent>.Success(new HoldingCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, holdingType, nominalType, name, active, isDefault))
            : Result<HoldingCreatedEvent>.Invalid(validationErrors);
    }
}
