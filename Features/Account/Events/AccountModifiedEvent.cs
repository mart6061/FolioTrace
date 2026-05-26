using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountModifiedEvent : EventBase, IAccountEvent
{
    public AccountID AccountID { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public string FormalName { get; init; } = string.Empty;

    [JsonConstructor]
    private AccountModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal AccountModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, string name, string formalName)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AccountID = accountID;
        Name = name;
        FormalName = formalName;
    }

    public override string Type => nameof(AccountModifiedEvent);
}
