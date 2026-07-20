using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Country Modified Event")]
public sealed record CountryModifiedEvent : EventBase, ICountryEvent
{
    [EventProperty(Description = "Alpha2")]
    public Alpha2 Alpha2 { get; init; } = null!;

    [EventProperty(Description = "Alpha3")]
    public Alpha3 Alpha3 { get; init; } = null!;

    [EventProperty(Description = "Numeric")]
    public short Numeric { get; init; }

    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;

    [JsonConstructor]
    private CountryModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public CountryModifiedEvent(UserID userId, EventDateTime eventDateTime, string reason, Alpha2 alpha2, Alpha3 alpha3, short numeric, string name)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, alpha2, alpha3, numeric, name)
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
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);

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
