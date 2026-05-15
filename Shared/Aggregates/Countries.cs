using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AILibrary.Domain;
using AILibrary.Types;

namespace AILibrary.Aggregates;

public sealed record Countries
{
    public required EventDateTime EventDate { get; init; }

    public required LastUpdatedDateTime LastUpdateDateTime { get; init; }

    public required List<Country> Items { get; init; }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Countries(EventDateTime eventDate, LastUpdatedDateTime lastUpdateDateTime, List<Country> items)
    {
        if (eventDate is null)
            throw new ArgumentNullException(nameof(eventDate));

        if (lastUpdateDateTime is null)
            throw new ArgumentNullException(nameof(lastUpdateDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(country => country is null))
            throw new ArgumentException("Value must not contain null countries.", nameof(items));

        EventDate = eventDate;
        LastUpdateDateTime = lastUpdateDateTime;
        Items = items;
    }
}

