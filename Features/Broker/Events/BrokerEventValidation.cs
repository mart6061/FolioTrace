using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class BrokerEventValidation
{
    public static List<string> ValidateBase(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, LegalEntityIdentifier? lei)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);

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
