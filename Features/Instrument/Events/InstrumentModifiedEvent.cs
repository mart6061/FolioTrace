using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Instrument Modified Event")]
public sealed record InstrumentModifiedEvent : EventBase, IInstrumentEvent
{
    [EventProperty(Description = "Instrument ID")]
    public InstrumentID InstrumentID { get; init; } = null!;
    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;
    [EventProperty(Description = "Formal Name")]
    public string FormalName { get; init; } = string.Empty;
    [EventProperty(Description = "Exchange")]
    public Exchange Exchange { get; init; } = null!;
    [EventProperty(Description = "CFI")]
    public CFI CFI { get; init; } = null!;
    [EventProperty(Description = "Logo")]
    public InstrumentLogo? Logo { get; init; }
    [EventProperty(Description = "Income Country")]
    public Alpha2 IncomeCountry { get; init; } = null!;
    [EventProperty(Description = "Price Country")]
    public Alpha2 PriceCountry { get; init; } = null!;
    [EventProperty(Description = "Price Currency")]
    public Alpha3 PriceCurrency { get; init; } = null!;

    [JsonConstructor]
    private InstrumentModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal InstrumentModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo, Alpha2 incomeCountry, Alpha2 priceCountry, Alpha3 priceCurrency)
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
        PriceCurrency = priceCurrency;
    }

    public override string Type => nameof(InstrumentModifiedEvent);
}
