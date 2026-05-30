using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkItem(Guid BookmarkID, UserBookmarkType BookmarkType, string Url, DisplayOrder DisplayOrder)
{
    public string ToData() => $"{BookmarkID}|{BookmarkType}|{Url}|{DisplayOrder}";

    public string ToDetail() => $"{nameof(UserBookmarkItem)}: (BookmarkID: {BookmarkID}, BookmarkType: {BookmarkType}, Url: {Url}, DisplayOrder: {DisplayOrder})";
}
