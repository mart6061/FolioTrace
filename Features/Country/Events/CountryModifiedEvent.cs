using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CountryModifiedEvent : EventBase, ICountryEvent
{
    public Alpha2 Alpha2 { get; init; } = null!;

    public Alpha3 Alpha3 { get; init; } = null!;

    public short Numeric { get; init; }

    public string Name { get; init; } = string.Empty;

    [JsonConstructor]
    private CountryModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public CountryModifiedEvent(UserID userId, EventDateTime eventDateTime, string reason, Alpha2 alpha2, Alpha3 alpha3, short numeric, string name)
        : this(Guid.NewGuid(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, alpha2, alpha3, numeric, name)
    {
    }

    internal CountryModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha2 alpha2, Alpha3 alpha3, short numeric, string name)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        Alpha2 = alpha2;
        Alpha3 = alpha3;
        Numeric = numeric;
        Name = name;
    }

    public override string Type => nameof(CountryModifiedEvent); // TODO: Remind me to create a universal constant for this event type.

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, Alpha2? alpha2, Alpha3? alpha3, short numeric, string? name)
    {
        var messages = new List<string>();

        if (eventId is null)
            messages.Add("EventID is required.");

        if (userId is null)
            messages.Add("UserID is required.");

        if (eventDateTime is null)
            messages.Add("EventDateTime is required.");

        if (auditDateTime is null)
            messages.Add("AuditDateTime is required.");

        if (string.IsNullOrWhiteSpace(reason))
            messages.Add("Reason is required.");

        if (alpha2 is null)
            messages.Add("Alpha2 is required.");

        if (alpha3 is null)
            messages.Add("Alpha3 is required.");

        if (numeric < 0 || numeric > 999)
            messages.Add("Numeric must be between 0 and 999.");

        if (string.IsNullOrWhiteSpace(name))
            messages.Add("Name is required.");

        return messages;
    }
}
