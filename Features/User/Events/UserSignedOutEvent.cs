using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserSignedOutEvent : EventBase, IUserEvent
{
    [JsonConstructor]
    private UserSignedOutEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public UserSignedOutEvent(UserID userId, EventDateTime eventDateTime, string reason)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason)
    {
    }

    internal UserSignedOutEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
    }

    public override string Type => nameof(UserSignedOutEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason)
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

        return messages;
    }
}
