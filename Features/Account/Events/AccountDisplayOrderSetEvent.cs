using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountDisplayOrderSetEvent : EventBase, IAccountEvent
{
    public AccountID AccountID { get; init; } = null!;
    public DisplayOrder DisplayOrder { get; init; } = null!;

    [JsonConstructor]
    private AccountDisplayOrderSetEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal AccountDisplayOrderSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, DisplayOrder displayOrder)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AccountID = accountID;
        DisplayOrder = displayOrder;
    }

    public override string Type => nameof(AccountDisplayOrderSetEvent);
}
