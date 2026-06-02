using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<TicketSide>))]
public enum TicketSide
{
    Buy,
    Sell
}
