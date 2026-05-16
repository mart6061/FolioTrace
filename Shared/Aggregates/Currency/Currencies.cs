using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Currencies
{
    public required EventDateTime EventDate { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    public required List<Currency> Items { get; init; }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Currencies(EventDateTime eventDate, LastAuditDateTime lastAuditDateTime, List<Currency> items)
    {
        if (eventDate is null)
            throw new ArgumentNullException(nameof(eventDate));

        if (lastAuditDateTime is null)
            throw new ArgumentNullException(nameof(lastAuditDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(currency => currency is null))
            throw new ArgumentException("Value must not contain null currencies.", nameof(items));

        EventDate = eventDate;
        LastAuditDateTime = lastAuditDateTime;
        Items = items;
    }
}

