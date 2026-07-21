using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentValue : IModel
{
    public required InstrumentID InstrumentID { get; init; }
    public required string Name { get; init; }
    public required string FormalName { get; init; }
    public required Exchange Exchange { get; init; }
    public required CFI CFI { get; init; }
    public InstrumentLogo? Logo { get; init; }
    public required Active Active { get; init; }
    public required Alpha2 IncomeCountry { get; init; }
    public required Alpha2 PriceCountry { get; init; }
    public required Alpha3 PriceCurrency { get; init; }
    public required List<InstrumentIdentifier> Identifiers { get; init; }
    public IInstrumentTerms? Terms { get; init; }
    public IInstrumentPrice? Price { get; init; }
    public EventDateTime? PriceValuationDateTime { get; init; }
    public IInstrumentIncome? Income { get; init; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [SetsRequiredMembers]
    public InstrumentValue(Instrument instrument, IInstrumentPrice? price, EventDateTime? priceValuationDateTime, IInstrumentIncome? income, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        if (instrument is null)
            throw new ArgumentNullException(nameof(instrument));

        InstrumentID = instrument.InstrumentID;
        Name = instrument.Name;
        FormalName = instrument.FormalName;
        Exchange = instrument.Exchange;
        CFI = instrument.CFI;
        Logo = instrument.Logo;
        Active = instrument.Active;
        IncomeCountry = instrument.IncomeCountry;
        PriceCountry = instrument.PriceCountry;
        PriceCurrency = instrument.PriceCurrency;
        Identifiers = instrument.Identifiers;
        Terms = instrument.Terms;
        EnsureValidValuePair(price, income);

        Price = price;
        PriceValuationDateTime = priceValuationDateTime;
        Income = income;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentValue(InstrumentID instrumentID, string name, string formalName, Exchange exchange, CFI cfi, InstrumentLogo? logo,
        Active active, Alpha2 incomeCountry, Alpha2 priceCountry, Alpha3 priceCurrency, List<InstrumentIdentifier> identifiers,
        IInstrumentTerms? terms, IInstrumentPrice? price, EventDateTime? priceValuationDateTime, IInstrumentIncome? income,
        EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
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
        Identifiers = identifiers;
        Terms = terms;
        EnsureValidValuePair(price, income);
        Price = price;
        PriceValuationDateTime = priceValuationDateTime;
        Income = income;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    private static void EnsureValidValuePair(IInstrumentPrice? price, IInstrumentIncome? income)
    {
        if (price is null && income is null)
            return;

        if (price is null || income is null)
            throw new ArgumentException("Instrument value price and income must both be provided, or both be omitted.");

        if (!IsValidValuePair(price, income))
            throw new ArgumentException($"Instrument value price type '{price.PriceType}' cannot be paired with income type '{income.IncomeType}'.");
    }

    private static bool IsValidValuePair(IInstrumentPrice price, IInstrumentIncome income) =>
        (price, income) switch
        {
            (InstrumentPriceCash, InstrumentIncomeCash) => true,
            (InstrumentPriceFixedIncome, InstrumentIncomeFixedIncome) => true,
            (InstrumentPriceEquity, InstrumentIncomeEquity) => true,
            _ => false
        };
}
