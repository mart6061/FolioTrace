using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class CountryFlagModifiedEventBuilder
{
    public static Result<CountryFlagModifiedEvent> Create(CountryFlagModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Alpha2, request.Flag);
    }

    public static Result<CountryFlagModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, Alpha2 alpha2, CountryFlag flag)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = CountryFlagModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, alpha2, flag);

        return validationErrors.Count == 0
            ? Result<CountryFlagModifiedEvent>.Success(new CountryFlagModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, alpha2, flag))
            : Result<CountryFlagModifiedEvent>.Invalid(validationErrors);
    }

    public static Result<CountryFlagModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha2 alpha2, CountryFlag flag)
    {
        var validationErrors = CountryFlagModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, alpha2, flag);

        return validationErrors.Count == 0
            ? Result<CountryFlagModifiedEvent>.Success(new CountryFlagModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, alpha2, flag))
            : Result<CountryFlagModifiedEvent>.Invalid(validationErrors);
    }
}
