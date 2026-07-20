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
        UserValuationDateOption startValuationDateOption,
        UserValuationDateOption endValuationDateOption,
        HoldingDateBasis holdingDateBasis)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventID, userID, eventDateTime, auditDateTime, reason);

        if (!Enum.IsDefined(startValuationDateOption))
            messages.Add($"StartValuationDateOption '{startValuationDateOption}' is not supported.");

        if (!Enum.IsDefined(endValuationDateOption))
            messages.Add($"EndValuationDateOption '{endValuationDateOption}' is not supported.");

        if (!Enum.IsDefined(holdingDateBasis))
            messages.Add($"HoldingDateBasis '{holdingDateBasis}' is not supported.");

        return messages;
    }
}
