using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Account Created Event")]
public sealed record AccountCreatedEvent : EventBase, IAccountEvent
{
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;
    [EventProperty(Description = "Formal Name")]
    public string FormalName { get; init; } = string.Empty;
    [EventProperty(Order = 40, Description = "Book Currency")]
    public Alpha3 BookCurrency { get; init; } = null!;
    [EventProperty(Description = "Active")]
    public Active Active { get; init; } = false;

    [JsonConstructor]
    private AccountCreatedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal AccountCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AccountID accountID, string name, string formalName, Alpha3 bookCurrency, Active active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AccountID = accountID;
        Name = name;
        FormalName = formalName;
        BookCurrency = bookCurrency;
        Active = active;
    }

    public override string Type => nameof(AccountCreatedEvent);

    public static List<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, AccountID? accountID, string? name, string? formalName, Alpha3? bookCurrency)
    {
        var messages = AccountEventValidation.ValidateAccountChange(eventId, userId, eventDateTime, auditDateTime, reason, accountID, name, formalName);
        if (bookCurrency is null) messages.Add("BookCurrency is required.");
        return messages;
    }
}
