using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "User Bookmark Display Order Set Event")]
public sealed record UserBookmarkDisplayOrderSetEvent : EventBase, IUserBookmarksEvent
{
    [EventProperty(Description = "Bookmark ID")]
    public Guid BookmarkID { get; init; }

    [EventProperty(Description = "Display Order")]
    public DisplayOrder DisplayOrder { get; init; } = null!;

    [JsonConstructor]
    private UserBookmarkDisplayOrderSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal UserBookmarkDisplayOrderSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Guid bookmarkID, DisplayOrder displayOrder)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        BookmarkID = bookmarkID;
        DisplayOrder = displayOrder;
    }

    public override string Type => nameof(UserBookmarkDisplayOrderSetEvent);
}
