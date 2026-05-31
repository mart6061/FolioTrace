using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarkCreatedEvent : EventBase, IUserBookmarksEvent
{
    public Guid BookmarkID { get; init; }

    public UserBookmarkType BookmarkType { get; init; }

    public string Url { get; init; } = string.Empty;

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

    public override string ToData() => $"{base.ToData()}|{BookmarkID}|{BookmarkType}|{Url}|{DisplayOrder}";

    public override string ToDetail() => $"{nameof(UserBookmarkCreatedEvent)}: ({base.ToDetail()}, BookmarkID: {BookmarkID}, BookmarkType: {BookmarkType}, Url: {Url}, DisplayOrder: {DisplayOrder})";
}
