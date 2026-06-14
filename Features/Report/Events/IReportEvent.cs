using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonDerivedType(typeof(ReportCreatedEvent), nameof(ReportCreatedEvent))]
[JsonDerivedType(typeof(ReportModifiedEvent), nameof(ReportModifiedEvent))]
public interface IReportEvent : IEventBase
{
    ReportID ReportID { get; }
}
