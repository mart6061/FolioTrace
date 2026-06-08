using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InstrumentCreatedEventBuilder
{
    public static Result<InstrumentCreatedEvent> Create(InstrumentCreatedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.InstrumentID ?? InstrumentIDBuilder.Create(), request.Name, request.FormalName, request.Exchange, request.CFI, request.Logo, request.Active, request.IncomeCountry, request.PriceCountry, request.PriceCurrency);
    }

    public static Result<InstrumentCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo, Active active, Alpha2 incomeCountry, Alpha2 priceCountry, Alpha3 priceCurrency)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, name, formalName, exchange, cfi, logo, active, incomeCountry, priceCountry, priceCurrency);
    }

    public static Result<InstrumentCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo, Active active, Alpha2 incomeCountry, Alpha2 priceCountry, Alpha3 priceCurrency)
    {
        var validationErrors = ValidateCommon(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID);
        ValidateDefinition(validationErrors, name, formalName, exchange, cfi, incomeCountry, priceCountry, priceCurrency);

        return validationErrors.Count == 0
            ? Result<InstrumentCreatedEvent>.Success(new InstrumentCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, name, formalName, exchange, cfi, logo, active, incomeCountry, priceCountry, priceCurrency))
            : Result<InstrumentCreatedEvent>.Invalid(validationErrors);
    }

    internal static List<string> ValidateCommon(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, InstrumentID? instrumentID)
    {
        var messages = new List<string>();
        if (eventId is null) messages.Add("EventID is required.");
        if (userId is null) messages.Add("UserID is required.");
        if (eventDateTime is null) messages.Add("EventDateTime is required.");
        if (auditDateTime is null) messages.Add("AuditDateTime is required.");
        if (string.IsNullOrWhiteSpace(reason)) messages.Add("Reason is required.");
        if (instrumentID is null) messages.Add("InstrumentID is required.");
        return messages;
    }

    internal static void ValidateDefinition(List<string> messages, string? name, string? formalName, Exchange? exchange, CFI? cfi, Alpha2? incomeCountry, Alpha2? priceCountry, Alpha3? priceCurrency)
    {
        if (string.IsNullOrWhiteSpace(name)) messages.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(formalName)) messages.Add("FormalName is required.");
        if (exchange is null) messages.Add("Exchange is required.");
        if (cfi is null) messages.Add("CFI is required.");
        if (incomeCountry is null) messages.Add("IncomeCountry is required.");
        if (priceCountry is null) messages.Add("PriceCountry is required.");
        if (priceCurrency is null) messages.Add("PriceCurrency is required.");
    }
}
