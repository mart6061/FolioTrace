using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<DateControlOptionKind>))]
public enum DateControlOptionKind { Rule, Custom, Separator }
