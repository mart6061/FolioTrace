using AILibrary.Types;

namespace AILibrary.Aggregates;

public static class CountryBuilder
{
    // Create a new Country from provided alues (validation enforced by Country constructor)
    public static Country Create(ISO2 iso2, ISO3 iso3, short numeric, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, LastAuditDateTime lastAuditDateTime) => new Country(iso2, iso3, numeric, valuationDateTime, asOfDateTime, lastAuditDateTime);

    // Create a new Country from a CountryCreatedEvent
    public static Country Create(CountryCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Country(createdEvent.ISO2, createdEvent.ISO3, createdEvent.Numeric, createdEvent.EventDateTime, createdEvent.AuditDateTime, createdEvent.AuditDateTime);
    }

    extension(Country country)
    {
        // Apply a CountryModifiedEvent to an existing Country and use the audit timestamp as the last audit time
        public Country Apply(CountryModifiedEvent modifiedEvent)
        {
            if (country is null)
                throw new ArgumentNullException(nameof(country));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return country with
            {
                ISO2 = modifiedEvent.ISO2,
                ISO3 = modifiedEvent.ISO3,
                Numeric = modifiedEvent.Numeric,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }
    }
}


