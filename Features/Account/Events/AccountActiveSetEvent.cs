using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Account Active Set Event")]
public sealed record AccountActiveSetEvent : EventBase, IAccountEvent
{
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Active")]
    public Active Active { get; init; } = false;

    [JsonConstructor]
    private AccountActiveSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal AccountActiveSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, Active active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AccountID = accountID;
        Active = active;
    }

    public override string Type => nameof(AccountActiveSetEvent);
}
