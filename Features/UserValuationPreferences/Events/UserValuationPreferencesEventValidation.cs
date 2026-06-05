using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class UserValuationPreferencesEventValidation
{
    public static IReadOnlyList<string> Validate(
        EventID? eventID,
        UserID? userID,
        EventDateTime? eventDateTime,
        AuditDateTime? auditDateTime,
        string? reason,
        UserValuationDateOption valuationDateOption,
        HoldingDateBasis holdingDateBasis)
    {
        var messages = new List<string>();

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

        if (!Enum.IsDefined(valuationDateOption))
            messages.Add($"ValuationDateOption '{valuationDateOption}' is not supported.");

        if (!Enum.IsDefined(holdingDateBasis))
            messages.Add($"HoldingDateBasis '{holdingDateBasis}' is not supported.");

        return messages;
    }
}
