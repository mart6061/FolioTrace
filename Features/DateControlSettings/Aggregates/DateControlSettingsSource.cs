using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<DateControlSettingsSource>))]
public enum DateControlSettingsSource { Global, User }
