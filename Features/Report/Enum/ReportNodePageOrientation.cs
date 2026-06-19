using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<ReportNodePageOrientation>))]
public enum ReportNodePageOrientation
{
    Portrait,
    Landscape
}
