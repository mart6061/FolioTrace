using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class CurrencyModifiedEventBuilder
{
    public static Result<CurrencyModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = CurrencyModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, alphabeticCode, numericCode, decimalPlace, name);

        return validationErrors.Count == 0
            ? Result<CurrencyModifiedEvent>.Success(new CurrencyModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, alphabeticCode, numericCode, decimalPlace, name))
            : Result<CurrencyModifiedEvent>.Invalid(validationErrors);
    }

    public static Result<CurrencyModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
    {
        var validationErrors = CurrencyModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, alphabeticCode, numericCode, decimalPlace, name);

        return validationErrors.Count == 0
            ? Result<CurrencyModifiedEvent>.Success(new CurrencyModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, alphabeticCode, numericCode, decimalPlace, name))
            : Result<CurrencyModifiedEvent>.Invalid(validationErrors);
    }
}
