using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserModifiedEvent : EventBase, IUserEvent
{
    public string DisplayName { get; init; } = string.Empty;

    public UserDisplayPreferences DisplayPreferences { get; init; } = null!;

    public UserProfileValuationPreferences ValuationPreferences { get; init; } = null!;

    [JsonConstructor]
    private UserModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public UserModifiedEvent(UserID userId, EventDateTime eventDateTime, string reason, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
        : this(Guid.NewGuid(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason, displayName, displayPreferences, valuationPreferences)
    {
    }

    internal UserModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        DisplayName = displayName;
        DisplayPreferences = displayPreferences;
        ValuationPreferences = valuationPreferences;
    }

    public override string Type => nameof(UserModifiedEvent); // TODO: Remind me to create a universal constant for this event type.

    public override string ToData() =>
        $"{base.ToData()}|{DisplayName}|{DisplayPreferences.ToData()}|{ValuationPreferences.ToData()}";

    public override string ToDetail() =>
        $"{nameof(UserModifiedEvent)}: ({base.ToDetail()}, DisplayName: {DisplayName}, DisplayPreferences: {DisplayPreferences.ToDetail()}, ValuationPreferences: {ValuationPreferences.ToDetail()})";

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, string? displayName, UserDisplayPreferences? displayPreferences, UserProfileValuationPreferences? valuationPreferences)
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

        if (string.IsNullOrWhiteSpace(displayName))
            messages.Add("DisplayName is required.");

        if (displayPreferences is null)
            messages.Add("DisplayPreferences is required.");

        if (valuationPreferences is null)
            messages.Add("ValuationPreferences is required.");

        return messages;
    }
}
