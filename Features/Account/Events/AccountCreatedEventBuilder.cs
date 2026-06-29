using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class AccountCreatedEventBuilder
{
    public static Result<AccountCreatedEvent> Create(AccountCreatedRequest request, Currencies? currencies = null, Accounts? accounts = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.AccountID ?? AccountIDBuilder.Create(), request.Name, request.FormalName, request.BookCurrency, request.Active, currencies, accounts, request.BookCostBasis);
    }

    public static Result<AccountCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AccountID accountID, string name, string formalName, Alpha3 bookCurrency, Active active, Currencies? currencies = null, Accounts? accounts = null, ProfitLossMethod bookCostBasis = ProfitLossMethod.FIFO)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, accountID, name, formalName, bookCurrency, active, currencies, accounts, bookCostBasis);
    }

    public static Result<AccountCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, string name, string formalName, Alpha3 bookCurrency, Active active, Currencies? currencies = null, Accounts? accounts = null, ProfitLossMethod bookCostBasis = ProfitLossMethod.FIFO)
    {
        var validationErrors = AccountCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, accountID, name, formalName, bookCurrency, bookCostBasis);
        AccountEventValidation.ValidateBookCurrency(validationErrors, bookCurrency, currencies);
        AccountEventValidation.ValidateCreatedName(validationErrors, name, accounts);

        return validationErrors.Count == 0
            ? Result<AccountCreatedEvent>.Success(new AccountCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, accountID, name, formalName, bookCurrency, bookCostBasis, active))
            : Result<AccountCreatedEvent>.Invalid(validationErrors);
    }
}
