using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class CountryBuilder
{
    public static Country Create(UserID userId, Alpha2 alpha2, Alpha3 alpha3, short numeric, string name)
    {
        var createdEvent = CountryCreatedEventBuilder.Create(
            userId,
            EventDateTimeBuilder.Create(),
            $"Create {nameof(Country)}",
            alpha2,
            alpha3,
            numeric,
            name);

        return Create(createdEvent.Value!);
    }

    public static Country CreateSeed(Alpha2 alpha2, Alpha3 alpha3, short numeric, string name)
    {
        var createdEvent = CountryCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            Constants.Initialisation.AuditDateTime,
            Constants.Initialisation.Reason,
            alpha2,
            alpha3,
            numeric,
            name);

        return Create(createdEvent.Value!);
    }

    // Create a new Country from a CountryCreatedEvent
    public static Country Create(CountryCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Country(createdEvent.Alpha2, createdEvent.Alpha3, createdEvent.Numeric, createdEvent.Name, createdEvent.EventDateTime, createdEvent.AuditDateTime, createdEvent.EventID, createdEvent.AuditDateTime);
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
                Name = modifiedEvent.Name,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }

        public Country Apply(CountryFlagModifiedEvent modifiedEvent)
        {
            if (country is null)
                throw new ArgumentNullException(nameof(country));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return country with
            {
                Flag = modifiedEvent.Flag,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }
    }
}


