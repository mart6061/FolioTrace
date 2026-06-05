using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkDeletedEvent : EventBase, IUserBookmarksEvent
{
    public Guid BookmarkID { get; init; }

    [JsonConstructor]
    private UserBookmarkDeletedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal UserBookmarkDeletedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Guid bookmarkID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        BookmarkID = bookmarkID;
    }

    public override string Type => nameof(UserBookmarkDeletedEvent);
}
