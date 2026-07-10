using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[Builder]
public static class AccountIdentifierUnsetEventBuilder
{
    public static Result<AccountIdentifierUnsetEvent> Create(AccountIdentifierUnsetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        var errors = AccountEventValidation.ValidateCommon(eventID, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.AccountID);
        return errors.Count == 0
            ? Result<AccountIdentifierUnsetEvent>.Success(new AccountIdentifierUnsetEvent(eventID, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.AccountID, request.IdentifierType))
            : Result<AccountIdentifierUnsetEvent>.Invalid(errors);
    }
}
