using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal delegate TEvent HoldingBankCreatedEventFactory<out TEvent>(
    EventID eventId,
    UserID userId,
    EventDateTime eventDateTime,
    AuditDateTime auditDateTime,
    string reason,
    HoldingID holdingID,
    AccountID accountID,
    InstrumentID instrumentID,
    string name,
    bool active,
    bool isDefault,
    string bankName,
    string accountName,
    SortCode sortCode,
    BankAccountNumber accountNumber,
    BIC bic,
    IBAN iban);

internal static class HoldingCreatedEventBuilderCore
{
    public static Result<TEvent> Create<TEvent, TExpectedHolding>(
        IHoldingCreatedRequest request,
        Func<EventID, UserID, EventDateTime, AuditDateTime, string, HoldingID, AccountID, InstrumentID, string, bool, bool, TEvent> factory,
        Accounts? accounts = null,
        Instruments? instruments = null,
        Holdings? holdings = null)
        where TEvent : HoldingCreatedEvent
        where TExpectedHolding : Holding
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return CreateSeed<TEvent, TExpectedHolding>(
            Guid.NewGuid(),
            request.UserID,
            request.EventDateTime,
            AuditDateTimeBuilder.Create(),
            request.Reason,
            request.HoldingID ?? HoldingIDBuilder.Create(),
            request.AccountID,
            request.InstrumentID,
            request.Name,
            request.Active,
            request.Default,
            factory,
            accounts,
            instruments,
            holdings);
    }

    public static Result<TEvent> CreateSeed<TEvent, TExpectedHolding>(
        EventID eventId,
        UserID userId,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        HoldingID holdingID,
        AccountID accountID,
        InstrumentID instrumentID,
        string name,
        bool active,
        bool isDefault,
        Func<EventID, UserID, EventDateTime, AuditDateTime, string, HoldingID, AccountID, InstrumentID, string, bool, bool, TEvent> factory,
        Accounts? accounts = null,
        Instruments? instruments = null,
        Holdings? holdings = null)
        where TEvent : HoldingCreatedEvent
        where TExpectedHolding : Holding
    {
        var validationErrors = HoldingEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, holdingID);
        HoldingEventValidation.ValidateReferences(validationErrors, accountID, instrumentID, accounts, instruments);
        HoldingEventValidation.ValidateDefinition<TExpectedHolding>(validationErrors, name, isDefault);
        HoldingEventValidation.ValidateCreatedHolding<TExpectedHolding>(validationErrors, holdingID, accountID, instrumentID, isDefault, holdings);

        return validationErrors.Count == 0
            ? Result<TEvent>.Success(factory(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault))
            : Result<TEvent>.Invalid(validationErrors);
    }

    public static Result<TEvent> CreateBank<TEvent, TExpectedHolding>(
        IHoldingBankCreatedRequest request,
        HoldingBankCreatedEventFactory<TEvent> factory,
        Accounts? accounts = null,
        Instruments? instruments = null,
        Holdings? holdings = null)
        where TEvent : HoldingBankCreatedEvent
        where TExpectedHolding : HoldingBank
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return CreateBankSeed<TEvent, TExpectedHolding>(
            Guid.NewGuid(),
            request.UserID,
            request.EventDateTime,
            AuditDateTimeBuilder.Create(),
            request.Reason,
            request.HoldingID ?? HoldingIDBuilder.Create(),
            request.AccountID,
            request.InstrumentID,
            request.Name,
            request.Active,
            request.Default,
            request.BankName,
            request.AccountName,
            request.SortCode,
            request.AccountNumber,
            request.BIC,
            request.IBAN,
            factory,
            accounts,
            instruments,
            holdings);
    }

    public static Result<TEvent> CreateBankSeed<TEvent, TExpectedHolding>(
        EventID eventId,
        UserID userId,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        HoldingID holdingID,
        AccountID accountID,
        InstrumentID instrumentID,
        string name,
        bool active,
        bool isDefault,
        string bankName,
        string accountName,
        SortCode sortCode,
        BankAccountNumber accountNumber,
        BIC bic,
        IBAN iban,
        HoldingBankCreatedEventFactory<TEvent> factory,
        Accounts? accounts = null,
        Instruments? instruments = null,
        Holdings? holdings = null)
        where TEvent : HoldingBankCreatedEvent
        where TExpectedHolding : HoldingBank
    {
        var validationErrors = HoldingEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, holdingID);
        HoldingEventValidation.ValidateReferences(validationErrors, accountID, instrumentID, accounts, instruments);
        HoldingEventValidation.ValidateDefinition<TExpectedHolding>(validationErrors, name, isDefault);
        HoldingEventValidation.ValidateBankDetails(validationErrors, bankName, accountName, sortCode, accountNumber, bic, iban);
        HoldingEventValidation.ValidateCreatedHolding<TExpectedHolding>(validationErrors, holdingID, accountID, instrumentID, isDefault, holdings);

        return validationErrors.Count == 0
            ? Result<TEvent>.Success(factory(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban))
            : Result<TEvent>.Invalid(validationErrors);
    }
}
