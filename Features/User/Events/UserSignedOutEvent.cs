using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Authentication, Description = "User Signed Out Event")]
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
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);

        return messages;
    }
}
