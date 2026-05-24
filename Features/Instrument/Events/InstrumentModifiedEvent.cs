using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

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
