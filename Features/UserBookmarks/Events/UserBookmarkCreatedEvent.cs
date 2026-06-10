using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "User Bookmark Created Event")]
public sealed record UserBookmarkCreatedEvent : EventBase, IUserBookmarksEvent
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
    private UserBookmarkCreatedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal UserBookmarkCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Guid bookmarkID, UserBookmarkType bookmarkType, string url, DisplayOrder displayOrder)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        BookmarkID = bookmarkID;
        BookmarkType = bookmarkType;
        Url = url;
        DisplayOrder = displayOrder;
    }

    public override string Type => nameof(UserBookmarkCreatedEvent);
}
