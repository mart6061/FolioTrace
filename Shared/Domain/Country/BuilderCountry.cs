using AILibrary.Types;

namespace AILibrary.Domain;

public static class BuilderCountry
{
    // Create a new Country from provided values (validation enforced by Country constructor)
    public static Country Create(ISO2 iso2, ISO3 iso3, short numeric, LastUpdatedDateTime lastUpdateDateTime) => new Country(iso2, iso3, numeric, lastUpdateDateTime);

    // Create a new Country from a CountryCreatedEvent
    public static Country Create(CountryCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Country(createdEvent.ISO2, createdEvent.ISO3, createdEvent.Numeric, createdEvent.AuditDateTime);
    }

    // Apply a CountryModifiedEvent to an existing Country and use the audit timestamp as the last update time
    public static Country Apply(this Country country, CountryModifiedEvent modifiedEvent)
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
            LastUpdateDateTime = modifiedEvent.AuditDateTime
        };
    }
}


