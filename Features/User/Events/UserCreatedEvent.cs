using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "User Created Event")]
public sealed record UserCreatedEvent : EventBase, IUserEvent
{
    [EventProperty(Description = "Display Name")]
    public string DisplayName { get; init; } = string.Empty;

    [EventProperty(Description = "Display Preferences")]
    public UserDisplayPreferences DisplayPreferences { get; init; } = null!;

    [EventProperty(Description = "Valuation Preferences")]
    public UserProfileValuationPreferences ValuationPreferences { get; init; } = null!;

    [JsonConstructor]
    private UserCreatedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public UserCreatedEvent(UserID userId, EventDateTime eventDateTime, string reason, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, displayName, displayPreferences, valuationPreferences)
    {
    }

    internal UserCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        DisplayName = displayName;
        DisplayPreferences = displayPreferences;
        ValuationPreferences = valuationPreferences;
    }

    public override string Type => nameof(UserCreatedEvent); // TODO: Remind me to create a universal constant for this event type.

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, string? displayName, UserDisplayPreferences? displayPreferences, UserProfileValuationPreferences? valuationPreferences)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);

        if (string.IsNullOrWhiteSpace(displayName))
            messages.Add("DisplayName is required.");

        if (displayPreferences is null)
            messages.Add("DisplayPreferences is required.");

        if (valuationPreferences is null)
            messages.Add("ValuationPreferences is required.");

        return messages;
    }
}
