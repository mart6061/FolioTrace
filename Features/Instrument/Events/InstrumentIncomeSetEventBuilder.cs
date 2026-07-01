using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class InstrumentIncomeSetEventBuilder
{
    public static Result<InstrumentIncomeSetEvent> Create(InstrumentIncomeSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        return CreateSeed(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Income);
    }

    public static Result<InstrumentIncomeSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentIncome income)
    {
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID);
        if (income is null) validationErrors.Add("Income is required.");

        return validationErrors.Count == 0
            ? Result<InstrumentIncomeSetEvent>.Success(new InstrumentIncomeSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, income!))
            : Result<InstrumentIncomeSetEvent>.Invalid(validationErrors);
    }
}
