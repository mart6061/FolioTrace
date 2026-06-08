using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class CountryModifiedEventBuilder
{
    public static Result<CountryModifiedEvent> Create(CountryModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Alpha2, request.Alpha3, request.Numeric, request.Name);
    }

    public static Result<CountryModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, Alpha2 alpha2, Alpha3 alpha3, short numeric, string name)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = CountryModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, alpha2, alpha3, numeric, name);

        return validationErrors.Count == 0
            ? Result<CountryModifiedEvent>.Success(new CountryModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, alpha2, alpha3, numeric, name))
            : Result<CountryModifiedEvent>.Invalid(validationErrors);
    }

    public static Result<CountryModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha2 alpha2, Alpha3 alpha3, short numeric, string name)
    {
        var validationErrors = CountryModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, alpha2, alpha3, numeric, name);

        return validationErrors.Count == 0
            ? Result<CountryModifiedEvent>.Success(new CountryModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, alpha2, alpha3, numeric, name))
            : Result<CountryModifiedEvent>.Invalid(validationErrors);
    }
}
