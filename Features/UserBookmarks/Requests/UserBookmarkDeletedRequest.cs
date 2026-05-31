using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkDeletedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    Guid BookmarkID);
