using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkDisplayOrderSetEvent : EventBase, IUserBookmarksEvent
{
    public Guid BookmarkID { get; init; }

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

    public override string ToData() => $"{base.ToData()}|{BookmarkID}|{DisplayOrder}";

    public override string ToDetail() => $"{nameof(UserBookmarkDisplayOrderSetEvent)}: ({base.ToDetail()}, BookmarkID: {BookmarkID}, DisplayOrder: {DisplayOrder})";
}
