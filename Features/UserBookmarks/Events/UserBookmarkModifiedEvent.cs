using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "User Bookmark Modified Event")]
public sealed record UserBookmarkModifiedEvent : EventBase, IUserBookmarksEvent
{
    [EventProperty(Description = "Bookmark ID")]
    public Guid BookmarkID { get; init; }

    [EventProperty(Description = "Bookmark Type")]
    public UserBookmarkType BookmarkType { get; init; }

    [EventProperty(Description = "Url")]
    public string Url { get; init; } = string.Empty;

    [EventProperty(Description = "Display Order")]
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
