using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class CurrencyCreatedEventBuilder
{
    public static Result<CurrencyCreatedEvent> Create(CurrencyCreatedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.AlphabeticCode, request.NumericCode, request.DecimalPlace, request.Name);
    }

    public static Result<CurrencyCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = CurrencyCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, alphabeticCode, numericCode, decimalPlace, name);

        return validationErrors.Count == 0
            ? Result<CurrencyCreatedEvent>.Success(new CurrencyCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, alphabeticCode, numericCode, decimalPlace, name))
            : Result<CurrencyCreatedEvent>.Invalid(validationErrors);
    }

    public static Result<CurrencyCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
    {
        var validationErrors = CurrencyCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, alphabeticCode, numericCode, decimalPlace, name);

        return validationErrors.Count == 0
            ? Result<CurrencyCreatedEvent>.Success(new CurrencyCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, alphabeticCode, numericCode, decimalPlace, name))
            : Result<CurrencyCreatedEvent>.Invalid(validationErrors);
    }
}
