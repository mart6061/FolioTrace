using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class UserBookmarkDeletedEventBuilder
{
    public static Result<UserBookmarkDeletedEvent> Create(UserBookmarkDeletedRequest request)
    {
        return Create(request.UserID, request.EventDateTime, request.Reason, request.BookmarkID);
    }

    public static Result<UserBookmarkDeletedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, Guid bookmarkID)
    {
        return CreateSeed(Guid.CreateGuid7(), userID, eventDateTime, AuditDateTimeBuilder.Create(DateTime.UtcNow), reason, bookmarkID);
    }

    public static Result<UserBookmarkDeletedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Guid bookmarkID)
    {
        var validationErrors = UserBookmarkEventValidation.ValidateDelete(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID);
        return validationErrors.Count == 0
            ? Result<UserBookmarkDeletedEvent>.Success(new UserBookmarkDeletedEvent(eventID, userID, eventDateTime, auditDateTime, reason, bookmarkID))
            : Result<UserBookmarkDeletedEvent>.Invalid(validationErrors);
    }
}
