using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[Builder]
public static class AccountIdentifierSetEventBuilder
{
    public static Result<AccountIdentifierSetEvent> Create(AccountIdentifierSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CreateSeed(Guid.CreateGuid7(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.AccountID, request.Identifier);
    }
    public static Result<AccountIdentifierSetEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, InstrumentIdentifier identifier)
    {
        var errors = AccountEventValidation.ValidateCommon(eventID, userID, eventDateTime, auditDateTime, reason, accountID);
        if (identifier is null) errors.Add("Identifier is required.");
        return errors.Count == 0
            ? Result<AccountIdentifierSetEvent>.Success(new AccountIdentifierSetEvent(eventID, userID, eventDateTime, auditDateTime, reason, accountID, identifier!))
            : Result<AccountIdentifierSetEvent>.Invalid(errors);
    }
}
