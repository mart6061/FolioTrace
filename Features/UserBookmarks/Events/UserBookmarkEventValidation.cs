using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class UserBookmarkEventValidation
{
    public static IReadOnlyList<string> Validate(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, Guid bookmarkID, UserBookmarkType bookmarkType, string? url, DisplayOrder? displayOrder)
    {
        var messages = ValidateCommon(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID);

        if (!Enum.IsDefined(bookmarkType))
            messages.Add("BookmarkType is invalid.");

        if (string.IsNullOrWhiteSpace(url))
            messages.Add("Url is required.");
        else
        {
            if (!url.StartsWith('/'))
                messages.Add("Url must be a path.");
            if (url.Contains('?'))
                messages.Add("Url must not include query parameters.");
        }

        if (displayOrder is null || displayOrder.Value < 1)
            messages.Add("DisplayOrder must be positive.");

        return messages;
    }

    public static IReadOnlyList<string> ValidateDisplayOrder(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, Guid bookmarkID, DisplayOrder? displayOrder)
    {
        var messages = ValidateCommon(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID);

        if (displayOrder is null || displayOrder.Value < 1)
            messages.Add("DisplayOrder must be positive.");

        return messages;
    }

    public static IReadOnlyList<string> ValidateDelete(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, Guid bookmarkID) =>
        ValidateCommon(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID);

    private static List<string> ValidateCommon(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, Guid bookmarkID)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventID, userID, eventDateTime, auditDateTime, reason);

        if (bookmarkID == Guid.Empty)
            messages.Add("BookmarkID is required.");

        return messages;
    }
}
