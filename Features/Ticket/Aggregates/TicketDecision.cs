using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<TicketDecision>))]
public enum TicketDecision
{
    [Description("In progress")]
    InProgress,

    [Description("Pending Approval")]
    PendingApproval,

    [Description("Approved")]
    Approved,

    [Description("Not approved")]
    NotApproved
}
