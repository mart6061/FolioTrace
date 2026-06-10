using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

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
        var validationErrors = new List<string>();
        if (eventId is null) validationErrors.Add("EventID is required.");
        if (userId is null) validationErrors.Add("UserID is required.");
        if (eventDateTime is null) validationErrors.Add("EventDateTime is required.");
        if (auditDateTime is null) validationErrors.Add("AuditDateTime is required.");
        if (string.IsNullOrWhiteSpace(reason)) validationErrors.Add("Reason is required.");
        if (accountID is null) validationErrors.Add("AccountID is required.");
        AccountEventValidation.ValidateModifiedAccount(validationErrors, accountID, null, accounts);

        return validationErrors.Count == 0
            ? Result<AccountActiveSetEvent>.Success(new AccountActiveSetEvent(eventId!, userId!, eventDateTime!, auditDateTime!, reason, accountID!, active))
            : Result<AccountActiveSetEvent>.Invalid(validationErrors);
    }
}
