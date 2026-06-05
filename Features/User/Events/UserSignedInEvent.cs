using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserSignedInEvent : EventBase, IUserEvent
{
    [JsonConstructor]
    private UserSignedInEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public UserSignedInEvent(UserID userId, EventDateTime eventDateTime, string reason)
        : this(Guid.NewGuid(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason)
    {
    }

    internal UserSignedInEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
    }

    public override string Type => nameof(UserSignedInEvent);

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
