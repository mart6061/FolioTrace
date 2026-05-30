using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkDisplayOrderSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    Guid BookmarkID,
    DisplayOrder DisplayOrder);
