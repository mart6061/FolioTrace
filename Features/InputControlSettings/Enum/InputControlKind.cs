using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<InputControlKind>))]
public enum InputControlKind
{
    Quantity,
    Money,
    Price,
    Percent
}
