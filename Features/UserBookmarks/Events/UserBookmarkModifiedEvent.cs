using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkModifiedEvent : EventBase, IUserBookmarksEvent
{
    public Guid BookmarkID { get; init; }

    public UserBookmarkType BookmarkType { get; init; }

    public string Url { get; init; } = string.Empty;

    public DisplayOrder DisplayOrder { get; init; } = null!;

    [JsonConstructor]
    private UserBookmarkModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal UserBookmarkModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Guid bookmarkID, UserBookmarkType bookmarkType, string url, DisplayOrder displayOrder)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        BookmarkID = bookmarkID;
        BookmarkType = bookmarkType;
        Url = url;
        DisplayOrder = displayOrder;
    }

    public override string Type => nameof(UserBookmarkModifiedEvent);
}
