using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserBookmarks : IModel
{
    public required UserID UserID { get; init; }

    public List<UserBookmarkItem> Items { get; private set; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public UserBookmarks(
        UserID userID,
        List<UserBookmarkItem> items,
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        EventID lastEventID,
        LastAuditDateTime lastAuditDateTime)
    {
        UserID = userID;
        Items = Order(items);
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public UserBookmarks(UserID userID, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IUserBookmarksEvent> items)
    {
        if (userID is null)
            throw new ArgumentNullException(nameof(userID));
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null user bookmark events.", nameof(items));

        var orderedItems = items
            .Where(@event => @event.UserID == userID && @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        UserID = userID;
        Items = [];
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = new LastAuditDateTime(asOfDateTime.Value);

        foreach (var item in orderedItems)
            Apply(item);
    }

    public UserBookmarkItem? Find(Guid bookmarkID) => Items.SingleOrDefault(item => item.BookmarkID == bookmarkID);

    public UserBookmarkItem? Find(UserBookmarkType bookmarkType, string url) =>
        Items.SingleOrDefault(item => item.BookmarkType == bookmarkType && string.Equals(item.Url, url, StringComparison.Ordinal));

    public string ToData() => $"{UserID.ToData()}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}|{string.Join(';', Items.Select(item => item.ToData()))}";

    public string ToDetail() => $"{nameof(UserBookmarks)}: (UserID: {UserID.ToDetail()}, Items: {Items.Count})";

    private void Apply(IUserBookmarksEvent userBookmarksEvent)
    {
        switch (userBookmarksEvent)
        {
            case UserBookmarkCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case UserBookmarkModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            case UserBookmarkDisplayOrderSetEvent displayOrderSetEvent:
                Apply(displayOrderSetEvent);
                break;
            case UserBookmarkDeletedEvent deletedEvent:
                Apply(deletedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported user bookmark event type '{userBookmarksEvent.GetType().Name}'.");
        }
    }

    private void Apply(UserBookmarkCreatedEvent createdEvent)
    {
        Upsert(new UserBookmarkItem(createdEvent.BookmarkID, createdEvent.BookmarkType, createdEvent.Url, createdEvent.DisplayOrder));
        UpdateProvenance(createdEvent);
    }

    private void Apply(UserBookmarkModifiedEvent modifiedEvent)
    {
        var current = Find(modifiedEvent.BookmarkID);
        Upsert(new UserBookmarkItem(modifiedEvent.BookmarkID, modifiedEvent.BookmarkType, modifiedEvent.Url, current?.DisplayOrder ?? modifiedEvent.DisplayOrder));
        UpdateProvenance(modifiedEvent);
    }

    private void Apply(UserBookmarkDisplayOrderSetEvent displayOrderSetEvent)
    {
        var current = Find(displayOrderSetEvent.BookmarkID);
        if (current is not null)
            Upsert(current with { DisplayOrder = displayOrderSetEvent.DisplayOrder });

        UpdateProvenance(displayOrderSetEvent);
    }

    private void Apply(UserBookmarkDeletedEvent deletedEvent)
    {
        Items = Items.Where(existing => existing.BookmarkID != deletedEvent.BookmarkID).ToList();
        UpdateProvenance(deletedEvent);
    }

    private void Upsert(UserBookmarkItem item)
    {
        Items = Items.Where(existing => existing.BookmarkID != item.BookmarkID).Append(item).ToList();
        Items = Order(Items);
    }

    private static List<UserBookmarkItem> Order(IEnumerable<UserBookmarkItem> items) =>
        items.OrderBy(item => item.DisplayOrder.Value).ThenBy(item => item.Url, StringComparer.Ordinal).ThenBy(item => item.BookmarkID).ToList();

    private void UpdateProvenance(IUserBookmarksEvent @event)
    {
        LastEventID = @event.EventID;
        LastAuditDateTime = @event.AuditDateTime;
    }
}
