using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<UserBookmarkType>))]
public enum UserBookmarkType
{
    Base = 1,
    Query = 2
}
