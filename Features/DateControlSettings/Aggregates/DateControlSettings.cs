using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Date control settings")]
public sealed record DateControlSettings : IAggregate
{
    public DateControlConfiguration Configuration { get; private set; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public DateControlSettings(DateControlConfiguration configuration, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        Configuration = configuration;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public DateControlSettings(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, IEnumerable<IDateControlSettingsEvent> events)
    {
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        Configuration = DateControlConfigurationDefaults.Create();
        LastEventID = Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = new(asOfDateTime.Value);

        foreach (var item in events.Where(item => item.EventDateTime.Value <= valuationDateTime.Value && item.AuditDateTime.Value <= asOfDateTime.Value)
                     .OrderBy(item => item.EventDateTime.Value).ThenBy(item => item.AuditDateTime.Value).ThenBy(item => item.EventID.Value))
        {
            Configuration = item.Configuration;
            LastEventID = item.EventID;
            LastAuditDateTime = item.AuditDateTime;
        }
    }
}
