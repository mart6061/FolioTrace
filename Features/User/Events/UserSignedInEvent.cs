using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Authentication, Description = "User Signed In Event")]
public sealed record UserSignedInEvent : EventBase, IUserEvent
{
    [JsonConstructor]
    private UserSignedInEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    public UserSignedInEvent(UserID userId, EventDateTime eventDateTime, string reason)
        : this(Guid.CreateGuid7(), userId, eventDateTime, AuditDateTimeBuilder.Create(), reason)
    {
    }

    internal UserSignedInEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
    }

    public override string Type => nameof(UserSignedInEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);

        return messages;
    }
}
