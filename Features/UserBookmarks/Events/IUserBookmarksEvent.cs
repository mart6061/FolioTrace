using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonDerivedType(typeof(UserBookmarkCreatedEvent), nameof(UserBookmarkCreatedEvent))]
[JsonDerivedType(typeof(UserBookmarkModifiedEvent), nameof(UserBookmarkModifiedEvent))]
[JsonDerivedType(typeof(UserBookmarkDisplayOrderSetEvent), nameof(UserBookmarkDisplayOrderSetEvent))]
[JsonDerivedType(typeof(UserBookmarkDeletedEvent), nameof(UserBookmarkDeletedEvent))]
public interface IUserBookmarksEvent : IEventBase
{
}
