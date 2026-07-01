using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[Builder]
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
            createdEvent.PriceCurrency,
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
                PriceCurrency = modifiedEvent.PriceCurrency,
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
