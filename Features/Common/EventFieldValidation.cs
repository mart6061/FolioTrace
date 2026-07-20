using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class EventFieldValidation
{
    public static void AddAuditFieldMessages(List<string> messages, EventID? eventID, UserID? userID, AuditDateTime? auditDateTime)
    {
        if (eventID is null)
            messages.Add("EventID is required.");

        if (userID is null)
            messages.Add("UserID is required.");

        if (auditDateTime is null)
            messages.Add("AuditDateTime is required.");
    }

    public static void AddCommonFieldMessages(List<string> messages, EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason)
    {
        if (eventID is null)
            messages.Add("EventID is required.");

        if (userID is null)
            messages.Add("UserID is required.");

        if (eventDateTime is null)
            messages.Add("EventDateTime is required.");

        if (auditDateTime is null)
            messages.Add("AuditDateTime is required.");

        if (string.IsNullOrWhiteSpace(reason))
            messages.Add("Reason is required.");
    }

    public static List<string> CommonFieldMessages(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason)
    {
        var messages = new List<string>();
        AddCommonFieldMessages(messages, eventID, userID, eventDateTime, auditDateTime, reason);
        return messages;
    }
}
