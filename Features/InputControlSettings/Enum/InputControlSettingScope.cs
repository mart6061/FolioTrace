using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<InputControlSettingScope>))]
public enum InputControlSettingScope
{
    User,
    Global,
    Account
}
