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

    public static Result<FXCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, Alpha3 baseCurrency, Alpha3 quoteCurrency, bool active)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var pair = new CurrencyPair(baseCurrency, quoteCurrency);
        var validationErrors = FXCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair);

        return validationErrors.Count == 0
            ? Result<FXCreatedEvent>.Success(new FXCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, pair, active))
            : Result<FXCreatedEvent>.Invalid(validationErrors);
    }

    public static Result<FXCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha3 baseCurrency, Alpha3 quoteCurrency, bool active)
    {
        var pair = new CurrencyPair(baseCurrency, quoteCurrency);
        var validationErrors = FXCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair);

        return validationErrors.Count == 0
            ? Result<FXCreatedEvent>.Success(new FXCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, pair, active))
            : Result<FXCreatedEvent>.Invalid(validationErrors);
    }
}

public static class FXActiveModifiedEventBuilder
{
    public static Result<FXActiveModifiedEvent> Create(FXActiveModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Pair, request.Active);
    }

    public static Result<FXActiveModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, CurrencyPair pair, bool active)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = FXActiveModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair);

        return validationErrors.Count == 0
            ? Result<FXActiveModifiedEvent>.Success(new FXActiveModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, pair, active))
            : Result<FXActiveModifiedEvent>.Invalid(validationErrors);
    }
}

public static class FXRateSetEventBuilder
{
    public static Result<FXRateSetEvent> Create(FXRateSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Pair, request.Price);
    }

    public static Result<FXRateSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, CurrencyPair pair, FXPrice price)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = FXRateSetEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair, price);

        return validationErrors.Count == 0
            ? Result<FXRateSetEvent>.Success(new FXRateSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, pair, price))
            : Result<FXRateSetEvent>.Invalid(validationErrors);
    }

    public static Result<FXRateSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, CurrencyPair pair, FXPrice price)
    {
        var validationErrors = FXRateSetEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair, price);

        return validationErrors.Count == 0
            ? Result<FXRateSetEvent>.Success(new FXRateSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, pair, price))
            : Result<FXRateSetEvent>.Invalid(validationErrors);
    }
}
