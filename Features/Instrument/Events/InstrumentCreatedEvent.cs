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
    public Alpha3 PriceCurrency { get; init; } = null!;

    [JsonConstructor]
    private InstrumentCreatedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo, bool active, Alpha2 incomeCountry, Alpha2 priceCountry, Alpha3 priceCurrency)
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
        PriceCurrency = priceCurrency;
    }

    public override string Type => nameof(InstrumentCreatedEvent);

    public override string ToData() => $"{base.ToData()}|{InstrumentID.ToData()}|{Name}|{FormalName}|{Exchange.ToData()}|{CFI.ToData()}|{Active}|{IncomeCountry.ToData()}|{PriceCountry.ToData()}|{PriceCurrency.ToData()}";

    public override string ToDetail() => $"{nameof(InstrumentCreatedEvent)}: ({base.ToDetail()}, InstrumentID: {InstrumentID.ToDetail()}, Name: {Name}, FormalName: {FormalName}, Exchange: {Exchange.ToDetail()}, CFI: {CFI.ToDetail()}, Active: {Active})";
}
