using System;
using System.Collections.Generic;
using System.Linq;
using AILibrary.Types;

namespace AILibrary.Aggregates;

public static class CountriesBuilder
{
    public static Countries Create(EventDateTime eventDate, AuditDateTime auditDateTime, List<ICountryEvent> countryEvents)
    {
        if (eventDate is null)
            throw new ArgumentNullException(nameof(eventDate));

        if (auditDateTime is null)
            throw new ArgumentNullException(nameof(auditDateTime));

        if (countryEvents is null)
            throw new ArgumentNullException(nameof(countryEvents));

        if (countryEvents.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null country events.", nameof(countryEvents));

        if (!countryEvents.Any())
            throw new ArgumentException("Value must contain at least one country event.", nameof(countryEvents));

        return new Countries(eventDate.Value, auditDateTime, countryEvents);
    }
}
