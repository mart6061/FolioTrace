using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InstrumentCreatedEventBuilder
{
    public static Result<InstrumentCreatedEvent> Create(InstrumentCreatedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.InstrumentID ?? InstrumentIDBuilder.Create(), request.Name, request.FormalName, request.Exchange, request.CFI, request.Logo, request.Active, request.IncomeCountry, request.PriceCountry);
    }

    public static Result<InstrumentCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo, bool active, Alpha2 incomeCountry, Alpha2 priceCountry)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        return CreateSeed(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, name, formalName, exchange, cfi, logo, active, incomeCountry, priceCountry);
    }

    public static Result<InstrumentCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo, bool active, Alpha2 incomeCountry, Alpha2 priceCountry)
    {
        var validationErrors = ValidateCommon(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID);
        ValidateDefinition(validationErrors, name, formalName, exchange, cfi, incomeCountry, priceCountry);

        return validationErrors.Count == 0
            ? Result<InstrumentCreatedEvent>.Success(new InstrumentCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, name, formalName, exchange, cfi, logo, active, incomeCountry, priceCountry))
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

    internal static void ValidateDefinition(List<string> messages, string? name, string? formalName, Exchange? exchange, CFI? cfi, Alpha2? incomeCountry, Alpha2? priceCountry)
    {
        if (string.IsNullOrWhiteSpace(name)) messages.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(formalName)) messages.Add("FormalName is required.");
        if (exchange is null) messages.Add("Exchange is required.");
        if (cfi is null) messages.Add("CFI is required.");
        if (incomeCountry is null) messages.Add("IncomeCountry is required.");
        if (priceCountry is null) messages.Add("PriceCountry is required.");
    }
}

public static class InstrumentModifiedEventBuilder
{
    public static Result<InstrumentModifiedEvent> Create(InstrumentModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID);
        InstrumentCreatedEventBuilder.ValidateDefinition(validationErrors, request.Name, request.FormalName, request.Exchange, request.CFI, request.IncomeCountry, request.PriceCountry);

        return validationErrors.Count == 0
            ? Result<InstrumentModifiedEvent>.Success(new InstrumentModifiedEvent(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Name, request.FormalName, request.Exchange, request.CFI, request.Logo, request.IncomeCountry, request.PriceCountry))
            : Result<InstrumentModifiedEvent>.Invalid(validationErrors);
    }
}

public static class InstrumentActiveModifiedEventBuilder
{
    public static Result<InstrumentActiveModifiedEvent> Create(InstrumentActiveModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID);

        return validationErrors.Count == 0
            ? Result<InstrumentActiveModifiedEvent>.Success(new InstrumentActiveModifiedEvent(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Active))
            : Result<InstrumentActiveModifiedEvent>.Invalid(validationErrors);
    }
}

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

public static class InstrumentIdentifierUnsetEventBuilder
{
    public static Result<InstrumentIdentifierUnsetEvent> Create(InstrumentIdentifierUnsetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID);

        return validationErrors.Count == 0
            ? Result<InstrumentIdentifierUnsetEvent>.Success(new InstrumentIdentifierUnsetEvent(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.IdentifierType))
            : Result<InstrumentIdentifierUnsetEvent>.Invalid(validationErrors);
    }
}

public static class InstrumentTermsSetEventBuilder
{
    public static Result<InstrumentTermsSetEvent> Create(InstrumentTermsSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        return CreateSeed(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Terms);
    }

    public static Result<InstrumentTermsSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentTerms terms)
    {
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID);
        if (terms is null) validationErrors.Add("Terms are required.");

        return validationErrors.Count == 0
            ? Result<InstrumentTermsSetEvent>.Success(new InstrumentTermsSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, terms!))
            : Result<InstrumentTermsSetEvent>.Invalid(validationErrors);
    }
}

public static class InstrumentPriceSetEventBuilder
{
    public static Result<InstrumentPriceSetEvent> Create(InstrumentPriceSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        return CreateSeed(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Price);
    }

    public static Result<InstrumentPriceSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentPrice price)
    {
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID);
        if (price is null) validationErrors.Add("Price is required.");

        return validationErrors.Count == 0
            ? Result<InstrumentPriceSetEvent>.Success(new InstrumentPriceSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, price!))
            : Result<InstrumentPriceSetEvent>.Invalid(validationErrors);
    }
}

public static class InstrumentIncomeSetEventBuilder
{
    public static Result<InstrumentIncomeSetEvent> Create(InstrumentIncomeSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
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
