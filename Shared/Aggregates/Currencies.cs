using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AILibrary.Domain;
using AILibrary.Types;

namespace AILibrary.Aggregates;

public sealed record Currencies
{
    public required EventDateTime EventDate { get; init; }

    public required LastUpdatedDateTime LastUpdateDateTime { get; init; }

    public required List<Currency> Items { get; init; }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Currencies(EventDateTime eventDate, LastUpdatedDateTime lastUpdateDateTime, List<Currency> items)
    {
        if (eventDate is null)
            throw new ArgumentNullException(nameof(eventDate));

        if (lastUpdateDateTime is null)
            throw new ArgumentNullException(nameof(lastUpdateDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(currency => currency is null))
            throw new ArgumentException("Value must not contain null currencies.", nameof(items));

        EventDate = eventDate;
        LastUpdateDateTime = lastUpdateDateTime;
        Items = items;
    }
}

