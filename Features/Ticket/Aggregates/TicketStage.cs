using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<TicketStage>))]
public enum TicketStage
{
 
    [Description("Proposal")]
    Proposal,

    [Description("Trade")]
    Trade,

    [Description("Completed")]
    Completed,

    [Description("Cancelled")]
    Cancelled
}