using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserBookmarkDisplayOrderSetEventBuilder
{
    public static Result<UserBookmarkDisplayOrderSetEvent> Create(UserBookmarkDisplayOrderSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.BookmarkID, request.DisplayOrder);
    }

    public static Result<UserBookmarkDisplayOrderSetEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, Guid bookmarkID, DisplayOrder displayOrder)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID, displayOrder);
    }

    public static Result<UserBookmarkDisplayOrderSetEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Guid bookmarkID, DisplayOrder displayOrder)
    {
        var validationErrors = UserBookmarkEventValidation.ValidateDisplayOrder(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID, displayOrder);

        return validationErrors.Count == 0
            ? Result<UserBookmarkDisplayOrderSetEvent>.Success(new UserBookmarkDisplayOrderSetEvent(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID, displayOrder))
            : Result<UserBookmarkDisplayOrderSetEvent>.Invalid(validationErrors);
    }
}
