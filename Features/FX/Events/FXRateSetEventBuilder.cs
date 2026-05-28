using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

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
