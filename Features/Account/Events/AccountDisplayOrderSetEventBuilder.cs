using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class AccountDisplayOrderSetEventBuilder
{
    public static Result<AccountDisplayOrderSetEvent> Create(AccountDisplayOrderSetRequest request, Accounts? accounts = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.AccountID, request.DisplayOrder, accounts);
    }

    public static Result<AccountDisplayOrderSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AccountID accountID, DisplayOrder displayOrder, Accounts? accounts = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, accountID, displayOrder, accounts);
    }

    public static Result<AccountDisplayOrderSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, DisplayOrder displayOrder, Accounts? accounts = null)
    {
        var validationErrors = AccountEventValidation.ValidateAccountDisplayOrder(eventId, userId, eventDateTime, auditDateTime, reason, accountID, displayOrder);
        AccountEventValidation.ValidateModifiedAccount(validationErrors, accountID, null, accounts);

        return validationErrors.Count == 0
            ? Result<AccountDisplayOrderSetEvent>.Success(new AccountDisplayOrderSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, accountID, displayOrder))
            : Result<AccountDisplayOrderSetEvent>.Invalid(validationErrors);
    }
}
