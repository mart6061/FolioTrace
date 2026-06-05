using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkItem(Guid BookmarkID, UserBookmarkType BookmarkType, string Url, DisplayOrder DisplayOrder)
{
}
