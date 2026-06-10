using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Account Display Order Set Event")]
public sealed record AccountDisplayOrderSetEvent : EventBase, IAccountEvent
{
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Display Order")]
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
