using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentCreatedEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public string FormalName { get; init; } = string.Empty;
    public Exchange Exchange { get; init; } = null!;
    public CFI CFI { get; init; } = null!;
    public InstrumentLogo? Logo { get; init; }
    public bool Active { get; init; }
    public Alpha2 IncomeCountry { get; init; } = null!;
    public Alpha2 PriceCountry { get; init; } = null!;

    [JsonConstructor]
    private InstrumentCreatedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo, bool active, Alpha2 incomeCountry, Alpha2 priceCountry)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Name = name;
        FormalName = formalName;
        Exchange = exchange;
        CFI = cfi;
        Logo = logo;
        Active = active;
        IncomeCountry = incomeCountry;
        PriceCountry = priceCountry;
    }

    public override string Type => nameof(InstrumentCreatedEvent);

    public override string ToData() => $"{base.ToData()}|{InstrumentID.ToData()}|{Name}|{FormalName}|{Exchange.ToData()}|{CFI.ToData()}|{Active}|{IncomeCountry.ToData()}|{PriceCountry.ToData()}";

    public override string ToDetail() => $"{nameof(InstrumentCreatedEvent)}: ({base.ToDetail()}, InstrumentID: {InstrumentID.ToDetail()}, Name: {Name}, FormalName: {FormalName}, Exchange: {Exchange.ToDetail()}, CFI: {CFI.ToDetail()}, Active: {Active})";
}

public sealed record InstrumentModifiedEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public string FormalName { get; init; } = string.Empty;
    public Exchange Exchange { get; init; } = null!;
    public CFI CFI { get; init; } = null!;
    public InstrumentLogo? Logo { get; init; }
    public Alpha2 IncomeCountry { get; init; } = null!;
    public Alpha2 PriceCountry { get; init; } = null!;

    [JsonConstructor]
    private InstrumentModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo, Alpha2 incomeCountry, Alpha2 priceCountry)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Name = name;
        FormalName = formalName;
        Exchange = exchange;
        CFI = cfi;
        Logo = logo;
        IncomeCountry = incomeCountry;
        PriceCountry = priceCountry;
    }

    public override string Type => nameof(InstrumentModifiedEvent);
}

public sealed record InstrumentActiveModifiedEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public bool Active { get; init; }

    [JsonConstructor]
    private InstrumentActiveModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentActiveModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, bool active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Active = active;
    }

    public override string Type => nameof(InstrumentActiveModifiedEvent);
}

public sealed record InstrumentIdentifierSetEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public InstrumentIdentifier Identifier { get; init; } = null!;

    [JsonConstructor]
    private InstrumentIdentifierSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentIdentifierSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, InstrumentIdentifier identifier)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Identifier = identifier;
    }

    public override string Type => nameof(InstrumentIdentifierSetEvent);
}

public sealed record InstrumentIdentifierUnsetEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public InstrumentIdentifierType IdentifierType { get; init; }

    [JsonConstructor]
    private InstrumentIdentifierUnsetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentIdentifierUnsetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, InstrumentIdentifierType identifierType)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        IdentifierType = identifierType;
    }

    public override string Type => nameof(InstrumentIdentifierUnsetEvent);
}

public sealed record InstrumentTermsSetEvent : EventBase, IInstrumentEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public IInstrumentTerms Terms { get; init; } = null!;

    [JsonConstructor]
    private InstrumentTermsSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentTermsSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentTerms terms)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Terms = terms;
    }

    public override string Type => nameof(InstrumentTermsSetEvent);
}

public sealed record InstrumentPriceSetEvent : EventBase, IInstrumentPriceEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public IInstrumentPrice Price { get; init; } = null!;

    [JsonConstructor]
    private InstrumentPriceSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentPriceSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentPrice price)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Price = price;
    }

    public override string Type => nameof(InstrumentPriceSetEvent);
}

public sealed record InstrumentIncomeSetEvent : EventBase, IInstrumentIncomeEvent
{
    public InstrumentID InstrumentID { get; init; } = null!;
    public IInstrumentIncome Income { get; init; } = null!;

    [JsonConstructor]
    private InstrumentIncomeSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentIncomeSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentIncome income)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        InstrumentID = instrumentID;
        Income = income;
    }

    public override string Type => nameof(InstrumentIncomeSetEvent);
}
