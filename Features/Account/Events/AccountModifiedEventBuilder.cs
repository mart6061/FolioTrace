using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class AccountModifiedEventBuilder
{
    public static Result<AccountModifiedEvent> Create(AccountModifiedRequest request, Accounts? accounts = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.AccountID, request.Name, request.FormalName, accounts);
    }

    public static Result<AccountModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AccountID accountID, string name, string formalName, Accounts? accounts = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, accountID, name, formalName, accounts);
    }

    public static Result<AccountModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, string name, string formalName, Accounts? accounts = null)
    {
        var validationErrors = AccountEventValidation.ValidateAccountChange(eventId, userId, eventDateTime, auditDateTime, reason, accountID, name, formalName);
        AccountEventValidation.ValidateModifiedAccount(validationErrors, accountID, name, accounts);

        return validationErrors.Count == 0
            ? Result<AccountModifiedEvent>.Success(new AccountModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, accountID, name, formalName))
            : Result<AccountModifiedEvent>.Invalid(validationErrors);
    }
}
