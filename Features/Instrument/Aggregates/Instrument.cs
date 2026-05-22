using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Instrument : IModel
{
    public required InstrumentID InstrumentID { get; init; }

    public required string Name { get; init; }

    public required string FormalName { get; init; }

    public required Exchange Exchange { get; init; }

    public required CFI CFI { get; init; }

    public InstrumentLogo? Logo { get; init; }

    public required bool Active { get; init; }

    public required Alpha2 IncomeCountry { get; init; }

    public required Alpha2 PriceCountry { get; init; }

    public required List<InstrumentIdentifier> Identifiers { get; init; }

    public IInstrumentTerms? Terms { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required EventID LastEventID { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Instrument(
        InstrumentID instrumentID,
        string name,
        string formalName,
        Exchange exchange,
        CFI cfi,
        InstrumentLogo? logo,
        bool active,
        Alpha2 incomeCountry,
        Alpha2 priceCountry,
        List<InstrumentIdentifier> identifiers,
        IInstrumentTerms? terms,
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        EventID lastEventID,
        LastAuditDateTime lastAuditDateTime)
    {
        InstrumentID = instrumentID ?? throw new ArgumentNullException(nameof(instrumentID));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        FormalName = string.IsNullOrWhiteSpace(formalName) ? throw new ArgumentException("FormalName is required.", nameof(formalName)) : formalName.Trim();
        Exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        CFI = cfi ?? throw new ArgumentNullException(nameof(cfi));
        Logo = logo;
        Active = active;
        IncomeCountry = incomeCountry ?? throw new ArgumentNullException(nameof(incomeCountry));
        PriceCountry = priceCountry ?? throw new ArgumentNullException(nameof(priceCountry));
        Identifiers = identifiers ?? [];
        Terms = terms;
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        LastEventID = lastEventID ?? throw new ArgumentNullException(nameof(lastEventID));
        LastAuditDateTime = lastAuditDateTime ?? throw new ArgumentNullException(nameof(lastAuditDateTime));
    }

    public string ToData() => $"{InstrumentID.ToData()}|{Name}|{FormalName}|{Exchange.ToData()}|{CFI.ToData()}|{Active}|{IncomeCountry.ToData()}|{PriceCountry.ToData()}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Instrument)}: (InstrumentID: {InstrumentID.ToDetail()}, Name: {Name}, FormalName: {FormalName}, Exchange: {Exchange.ToDetail()}, CFI: {CFI.ToDetail()}, Active: {Active}, IncomeCountry: {IncomeCountry.ToDetail()}, PriceCountry: {PriceCountry.ToDetail()}, Identifiers: {Identifiers.Count}, Terms: {Terms?.ToDetail()}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}

public static class InstrumentBuilder
{
    public static Instrument Create(InstrumentCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Instrument(
            createdEvent.InstrumentID,
            createdEvent.Name,
            createdEvent.FormalName,
            createdEvent.Exchange,
            createdEvent.CFI,
            createdEvent.Logo,
            createdEvent.Active,
            createdEvent.IncomeCountry,
            createdEvent.PriceCountry,
            [],
            null,
            createdEvent.EventDateTime,
            createdEvent.AuditDateTime,
            createdEvent.EventID,
            createdEvent.AuditDateTime);
    }

    extension(Instrument instrument)
    {
        public Instrument Apply(InstrumentModifiedEvent modifiedEvent) =>
            instrument with
            {
                Name = modifiedEvent.Name,
                FormalName = modifiedEvent.FormalName,
                Exchange = modifiedEvent.Exchange,
                CFI = modifiedEvent.CFI,
                Logo = modifiedEvent.Logo,
                IncomeCountry = modifiedEvent.IncomeCountry,
                PriceCountry = modifiedEvent.PriceCountry,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };

        public Instrument Apply(InstrumentActiveModifiedEvent modifiedEvent) =>
            instrument with
            {
                Active = modifiedEvent.Active,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };

        public Instrument Apply(InstrumentIdentifierSetEvent setEvent)
        {
            var identifiers = instrument.Identifiers
                .Where(identifier => identifier.Type != setEvent.Identifier.Type)
                .Append(setEvent.Identifier)
                .OrderBy(identifier => identifier.Type)
                .ToList();

            return instrument with
            {
                Identifiers = identifiers,
                ValuationDateTime = setEvent.EventDateTime,
                AsOfDateTime = setEvent.AuditDateTime,
                LastEventID = setEvent.EventID,
                LastAuditDateTime = setEvent.AuditDateTime
            };
        }

        public Instrument Apply(InstrumentIdentifierUnsetEvent unsetEvent)
        {
            var identifiers = instrument.Identifiers
                .Where(identifier => identifier.Type != unsetEvent.IdentifierType)
                .ToList();

            return instrument with
            {
                Identifiers = identifiers,
                ValuationDateTime = unsetEvent.EventDateTime,
                AsOfDateTime = unsetEvent.AuditDateTime,
                LastEventID = unsetEvent.EventID,
                LastAuditDateTime = unsetEvent.AuditDateTime
            };
        }

        public Instrument Apply(InstrumentTermsSetEvent setEvent) =>
            instrument with
            {
                Terms = setEvent.Terms,
                ValuationDateTime = setEvent.EventDateTime,
                AsOfDateTime = setEvent.AuditDateTime,
                LastEventID = setEvent.EventID,
                LastAuditDateTime = setEvent.AuditDateTime
            };
    }
}
