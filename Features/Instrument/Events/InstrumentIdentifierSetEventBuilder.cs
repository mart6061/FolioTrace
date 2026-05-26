using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InstrumentIdentifierSetEventBuilder
{
    public static Result<InstrumentIdentifierSetEvent> Create(InstrumentIdentifierSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        return CreateSeed(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Identifier);
    }

    public static Result<InstrumentIdentifierSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, InstrumentIdentifier identifier)
    {
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID);
        if (identifier is null) validationErrors.Add("Identifier is required.");

        return validationErrors.Count == 0
            ? Result<InstrumentIdentifierSetEvent>.Success(new InstrumentIdentifierSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, identifier!))
            : Result<InstrumentIdentifierSetEvent>.Invalid(validationErrors);
    }
}
