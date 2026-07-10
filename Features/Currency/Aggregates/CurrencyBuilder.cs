using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class CurrencyBuilder
{
    public static Currency Create(UserID userId, Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
    {
        var createdEvent = CurrencyCreatedEventBuilder.Create(
            userId,
            EventDateTimeBuilder.Create(),
            $"Create {nameof(Currency)}",
            alphabeticCode,
            numericCode,
            decimalPlace,
            name);

        return Create(createdEvent.Value!);
    }

    public static Currency CreateSeed(Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name)
    {
        var createdEvent = CurrencyCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            Constants.Initialisation.AuditDateTime,
            Constants.Initialisation.Reason,
            alphabeticCode,
            numericCode,
            decimalPlace,
            name);

        return Create(createdEvent.Value!);
    }

    // Create a new Currency from a CurrencyCreatedEvent
    public static Currency Create(CurrencyCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Currency(createdEvent.AlphabeticCode, createdEvent.NumericCode, createdEvent.DecimalPlace, createdEvent.Name, createdEvent.EventDateTime, createdEvent.AuditDateTime, createdEvent.EventID);
    }

    extension(Currency currency)
    {
        // Apply a CurrencyModifiedEvent to an existing Currency and use the audit timestamp as the last audit time
        public Currency Apply(CurrencyModifiedEvent modifiedEvent)
        {
            if (currency is null)
                throw new ArgumentNullException(nameof(currency));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return currency with
            {
                AlphabeticCode = modifiedEvent.AlphabeticCode,
                NumericCode = modifiedEvent.NumericCode,
                DecimalPlace = modifiedEvent.DecimalPlace,
                Name = modifiedEvent.Name,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }
    }
}
