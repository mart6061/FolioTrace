using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserDateControlSettings : IModel
{
    public required UserID UserID { get; init; }
    public DateControlConfiguration Configuration { get; private set; }
    public bool HasStoredConfiguration { get; private set; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    [JsonConstructor, SetsRequiredMembers]
    public UserDateControlSettings(UserID userID, DateControlConfiguration configuration, bool hasStoredConfiguration, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    { UserID = userID; Configuration = configuration; HasStoredConfiguration = hasStoredConfiguration; ValuationDateTime = valuationDateTime; AsOfDateTime = asOfDateTime; LastEventID = lastEventID; LastAuditDateTime = lastAuditDateTime; }
    [SetsRequiredMembers]
    public UserDateControlSettings(UserID userID, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, IEnumerable<IUserDateControlSettingsEvent> events)
    {
        UserID = userID; ValuationDateTime = valuationDateTime; AsOfDateTime = asOfDateTime; Configuration = DateControlConfiguration.Empty; HasStoredConfiguration = false;
        LastEventID = Constants.Initialisation.EmptyViewEventID; LastAuditDateTime = new(asOfDateTime.Value);
        foreach (var item in events.Where(item => item.UserID == userID && item.EventDateTime.Value <= valuationDateTime.Value && item.AuditDateTime.Value <= asOfDateTime.Value).OrderBy(item => item.EventDateTime.Value).ThenBy(item => item.AuditDateTime.Value).ThenBy(item => item.EventID.Value))
        { Configuration = item.Configuration; HasStoredConfiguration = item is not UserDateControlSettingsClearedEvent; LastEventID = item.EventID; LastAuditDateTime = item.AuditDateTime; }
    }
}
