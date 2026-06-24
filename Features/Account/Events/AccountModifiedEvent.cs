using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Account Modified Event")]
public sealed record AccountModifiedEvent : EventBase, IAccountEvent
{
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;
    [EventProperty(Description = "Formal Name")]
    public string FormalName { get; init; } = string.Empty;
    [EventProperty(Order = 40, Description = "Book Cost Basis")]
    public ProfitLossMethod BookCostBasis { get; init; } = ProfitLossMethod.FIFO;

    [JsonConstructor]
    private AccountModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal AccountModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, string name, string formalName, ProfitLossMethod bookCostBasis)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AccountID = accountID;
        Name = name;
        FormalName = formalName;
        BookCostBasis = bookCostBasis;
    }

    public override string Type => nameof(AccountModifiedEvent);
}
