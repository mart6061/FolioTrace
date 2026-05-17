using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class CountryBuilder
{
    // Create a new Country from provided alues (validation enforced by Country constructor)
    public static Country Create(Alpha2 alpha2, Alpha3 alpha3, short numeric, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime) => new Country(alpha2, alpha3, numeric, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime);

    // Create a new Country from a CountryCreatedEvent
    public static Country Create(CountryCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Country(createdEvent.Alpha2, createdEvent.Alpha3, createdEvent.Numeric, createdEvent.EventDateTime, createdEvent.AuditDateTime, createdEvent.EventID, createdEvent.AuditDateTime);
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
                Alpha2 = modifiedEvent.Alpha2,
                Alpha3 = modifiedEvent.Alpha3,
                Numeric = modifiedEvent.Numeric,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }
    }
}


