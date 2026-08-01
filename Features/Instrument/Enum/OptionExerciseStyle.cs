using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<OptionExerciseStyle>))]
public enum OptionExerciseStyle
{
    American,
    European
}
