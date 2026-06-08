using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserBookmarkModifiedEventBuilder
{
    public static Result<UserBookmarkModifiedEvent> Create(UserBookmarkRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.BookmarkID ?? Guid.Empty, request.BookmarkType, request.Url, request.DisplayOrder);
    }

    public static Result<UserBookmarkModifiedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, Guid bookmarkID, UserBookmarkType bookmarkType, string url, DisplayOrder displayOrder)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID, bookmarkType, url, displayOrder);
    }

    public static Result<UserBookmarkModifiedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Guid bookmarkID, UserBookmarkType bookmarkType, string url, DisplayOrder displayOrder)
    {
        var validationErrors = UserBookmarkEventValidation.Validate(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID, bookmarkType, url, displayOrder);

        return validationErrors.Count == 0
            ? Result<UserBookmarkModifiedEvent>.Success(new UserBookmarkModifiedEvent(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID, bookmarkType, url, displayOrder))
            : Result<UserBookmarkModifiedEvent>.Invalid(validationErrors);
    }
}
