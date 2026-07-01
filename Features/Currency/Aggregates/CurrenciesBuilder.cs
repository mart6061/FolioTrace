using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class CurrenciesBuilder
{
    public static IReadOnlyList<Type> GetCurrencyEventTypes() =>
    [
        typeof(CurrencyCreatedEvent),
        typeof(CurrencyModifiedEvent)
    ];

    public static Currencies Create(EventDateTime eventDate, AuditDateTime auditDateTime, List<ICurrencyEvent> currencyEvents)
    {
        if (eventDate is null)
            throw new ArgumentNullException(nameof(eventDate));

        if (auditDateTime is null)
            throw new ArgumentNullException(nameof(auditDateTime));

        if (currencyEvents is null)
            throw new ArgumentNullException(nameof(currencyEvents));

        if (currencyEvents.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null currency events.", nameof(currencyEvents));

        if (!currencyEvents.Any())
            throw new ArgumentException("Value must contain at least one currency event.", nameof(currencyEvents));

        return new Currencies(eventDate.Value, auditDateTime, currencyEvents);
    }
}
