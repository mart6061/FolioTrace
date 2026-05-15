using AILibrary.Types;

namespace AILibrary.Domain;

public static class BuilderCurrency
{
    // Create a new Currency from provided values (validation enforced by Currency constructor)
    public static Currency Create(ISO3 alphabeticCode, int numericCode, short decimalPlace, string name, LastUpdatedDateTime lastUpdateDateTime) => new Currency(alphabeticCode, numericCode, decimalPlace, name, lastUpdateDateTime);

    // Create a new Currency from a CurrencyCreatedEvent
    public static Currency Create(CurrencyCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Currency(createdEvent.AlphabeticCode, createdEvent.NumericCode, createdEvent.DecimalPlace, createdEvent.Name, createdEvent.AuditDateTime);
    }

    // Apply a CurrencyModifiedEvent to an existing Currency and use the audit timestamp as the last update time
    public static Currency Apply(this Currency currency, CurrencyModifiedEvent modifiedEvent)
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
            LastUpdateDateTime = modifiedEvent.AuditDateTime
        };
    }
}
