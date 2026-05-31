using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    Guid? BookmarkID,
    UserBookmarkType BookmarkType,
    string Url,
    DisplayOrder DisplayOrder);
