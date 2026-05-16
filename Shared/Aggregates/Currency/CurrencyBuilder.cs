using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class CurrencyBuilder
{
    // Create a new Currency from provided values (validation enforced by Currency constructor)
    public static Currency Create(Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, LastAuditDateTime lastAuditDateTime) => new Currency(alphabeticCode, numericCode, decimalPlace, name, valuationDateTime, asOfDateTime, lastAuditDateTime);

    // Create a new Currency from a CurrencyCreatedEvent
    public static Currency Create(CurrencyCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Currency(createdEvent.AlphabeticCode, createdEvent.NumericCode, createdEvent.DecimalPlace, createdEvent.Name, createdEvent.EventDateTime, createdEvent.AuditDateTime);
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
            LastAuditDateTime = modifiedEvent.AuditDateTime
        };
        }
    }
}
