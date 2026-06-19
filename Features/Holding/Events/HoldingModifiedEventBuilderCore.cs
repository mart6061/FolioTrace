using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class HoldingModifiedEventBuilderCore
{
    public static Result<TEvent> Create<TEvent, TExpectedHolding>(
        IHoldingModifiedRequest request,
        Func<EventID, UserID, EventDateTime, AuditDateTime, string, HoldingID, string, bool, TEvent> factory,
        Holdings? holdings = null)
        where TEvent : HoldingModifiedEvent
        where TExpectedHolding : HoldingBase
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return CreateSeed<TEvent, TExpectedHolding>(
            Guid.CreateGuid7(),
            request.UserID,
            request.EventDateTime,
            AuditDateTimeBuilder.Create(),
            request.Reason,
            request.HoldingID,
            request.Name,
            request.Default,
            factory,
            holdings);
    }

    public static Result<TEvent> CreateSeed<TEvent, TExpectedHolding>(
        EventID eventId,
        UserID userId,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        HoldingID holdingID,
        string name,
        bool isDefault,
        Func<EventID, UserID, EventDateTime, AuditDateTime, string, HoldingID, string, bool, TEvent> factory,
        Holdings? holdings = null)
        where TEvent : HoldingModifiedEvent
        where TExpectedHolding : HoldingBase
    {
        var validationErrors = HoldingEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, holdingID);
        HoldingEventValidation.ValidateDefinition<TExpectedHolding>(validationErrors, name, isDefault);
        HoldingEventValidation.ValidateModifiedHolding<TExpectedHolding>(validationErrors, holdingID, isDefault, holdings);

        return validationErrors.Count == 0
            ? Result<TEvent>.Success(factory(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault))
            : Result<TEvent>.Invalid(validationErrors);
    }

    public static Result<TEvent> CreateBank<TEvent, TExpectedHolding>(
        IHoldingCashBaseModifiedRequest request,
        Func<EventID, UserID, EventDateTime, AuditDateTime, string, HoldingID, string, bool, string, string, SortCode, BankAccountNumber, BIC, IBAN, TEvent> factory,
        Holdings? holdings = null)
        where TEvent : HoldingCashBaseModifiedEvent
        where TExpectedHolding : HoldingCashBase
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return CreateBankSeed<TEvent, TExpectedHolding>(
            Guid.CreateGuid7(),
            request.UserID,
            request.EventDateTime,
            AuditDateTimeBuilder.Create(),
            request.Reason,
            request.HoldingID,
            request.Name,
            request.Default,
            request.BankName,
            request.AccountName,
            request.SortCode,
            request.AccountNumber,
            request.BIC,
            request.IBAN,
            factory,
            holdings);
    }

    public static Result<TEvent> CreateBankSeed<TEvent, TExpectedHolding>(
        EventID eventId,
        UserID userId,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        HoldingID holdingID,
        string name,
        bool isDefault,
        string bankName,
        string accountName,
        SortCode sortCode,
        BankAccountNumber accountNumber,
        BIC bic,
        IBAN iban,
        Func<EventID, UserID, EventDateTime, AuditDateTime, string, HoldingID, string, bool, string, string, SortCode, BankAccountNumber, BIC, IBAN, TEvent> factory,
        Holdings? holdings = null)
        where TEvent : HoldingCashBaseModifiedEvent
        where TExpectedHolding : HoldingCashBase
    {
        var validationErrors = HoldingEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, holdingID);
        HoldingEventValidation.ValidateDefinition<TExpectedHolding>(validationErrors, name, isDefault);
        HoldingEventValidation.ValidateBankDetails(validationErrors, bankName, accountName, sortCode, accountNumber, bic, iban);
        HoldingEventValidation.ValidateModifiedHolding<TExpectedHolding>(validationErrors, holdingID, isDefault, holdings);

        return validationErrors.Count == 0
            ? Result<TEvent>.Success(factory(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban))
            : Result<TEvent>.Invalid(validationErrors);
    }
}
