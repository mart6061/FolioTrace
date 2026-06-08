using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class FXCreatedEventBuilder
{
    public static Result<FXCreatedEvent> Create(FXCreatedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.BaseCurrency, request.QuoteCurrency, request.Active);
    }

    public static Result<FXCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, Alpha3 baseCurrency, Alpha3 quoteCurrency, Active active)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var pair = new CurrencyPair(baseCurrency, quoteCurrency);
        var validationErrors = FXCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair);

        return validationErrors.Count == 0
            ? Result<FXCreatedEvent>.Success(new FXCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, pair, active))
            : Result<FXCreatedEvent>.Invalid(validationErrors);
    }

    public static Result<FXCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha3 baseCurrency, Alpha3 quoteCurrency, Active active)
    {
        var pair = new CurrencyPair(baseCurrency, quoteCurrency);
        var validationErrors = FXCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair);

        return validationErrors.Count == 0
            ? Result<FXCreatedEvent>.Success(new FXCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, pair, active))
            : Result<FXCreatedEvent>.Invalid(validationErrors);
    }
}
