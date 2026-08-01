using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<OptionType>))]
public enum OptionType
{
    Call,
    Put
}
