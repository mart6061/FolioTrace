using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class BrokerEventValidation
{
    public static List<string> ValidateBase(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, LegalEntityIdentifier? lei)
    {
        var messages = new List<string>();

        if (eventId is null)
            messages.Add("EventID is required.");

        if (userId is null)
            messages.Add("UserID is required.");

        if (eventDateTime is null)
            messages.Add("EventDateTime is required.");

        if (auditDateTime is null)
            messages.Add("AuditDateTime is required.");

        if (string.IsNullOrWhiteSpace(reason))
            messages.Add("Reason is required.");

        if (lei is null)
            messages.Add("LEI is required.");

        return messages;
    }

    public static void ValidateName(List<string> messages, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            messages.Add("Name is required.");
    }

    public static void ValidateCommission(List<string> messages, FeeRate? commission)
    {
        if (commission is null)
            messages.Add("Commission is required.");
    }

    public static void ValidateApprovedDateTime(List<string> messages, EventDateTime? approvedDateTime)
    {
        if (approvedDateTime is null)
            messages.Add("ApprovedDateTime is required.");
    }

    public static void ValidateNextReview(List<string> messages, EventDateTime? nextReview)
    {
        if (nextReview is null)
            messages.Add("NextReview is required.");
    }
}
