using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountActiveModifiedEvent : EventBase, IAccountEvent
{
    public AccountID AccountID { get; init; } = null!;
    public Active Active { get; init; } = false;

    [JsonConstructor]
    private AccountActiveModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal AccountActiveModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, Active active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AccountID = accountID;
        Active = active;
    }

    public override string Type => nameof(AccountActiveModifiedEvent);
}
