using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class AccountActiveSetEventBuilder
{
    public static Result<AccountActiveSetEvent> Create(AccountActiveSetRequest request, Accounts? accounts = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.AccountID, request.Active, accounts);
    }

    public static Result<AccountActiveSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, AccountID accountID, Active active, Accounts? accounts = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, accountID, active, accounts);
    }

    public static Result<AccountActiveSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, Active active, Accounts? accounts = null)
    {
        var validationErrors = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);
        if (accountID is null) validationErrors.Add("AccountID is required.");
        AccountEventValidation.ValidateModifiedAccount(validationErrors, accountID, null, accounts);

        return validationErrors.Count == 0
            ? Result<AccountActiveSetEvent>.Success(new AccountActiveSetEvent(eventId!, userId!, eventDateTime!, auditDateTime!, reason, accountID!, active))
            : Result<AccountActiveSetEvent>.Invalid(validationErrors);
    }
}
