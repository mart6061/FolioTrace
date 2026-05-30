using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserValuationPreferences : IModel
{
    public required UserID UserID { get; init; }

    public UserValuationDateOption ValuationDateOption { get; private set; }

    public ValuationDateBasis ValuationDateBasis { get; private set; }

    public bool ShowZeroBalances { get; private set; }

    public bool HasStoredPreferences { get; private set; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public UserValuationPreferences(
        UserID userID,
        UserValuationDateOption valuationDateOption,
        ValuationDateBasis valuationDateBasis,
        bool showZeroBalances,
        bool hasStoredPreferences,
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        EventID lastEventID,
        LastAuditDateTime lastAuditDateTime)
    {
        UserID = userID;
        ValuationDateOption = valuationDateOption;
        ValuationDateBasis = valuationDateBasis;
        ShowZeroBalances = showZeroBalances;
        HasStoredPreferences = hasStoredPreferences;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public UserValuationPreferences(UserID userID, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IUserValuationPreferencesEvent> items)
    {
        if (userID is null)
            throw new ArgumentNullException(nameof(userID));
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null user valuation preference events.", nameof(items));

        var orderedItems = items
            .Where(@event => @event.UserID == userID && @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        UserID = userID;
        ValuationDateOption = UserValuationPreferenceDefaults.ValuationDateOption;
        ValuationDateBasis = UserValuationPreferenceDefaults.ValuationDateBasis;
        ShowZeroBalances = UserValuationPreferenceDefaults.ShowZeroBalances;
        HasStoredPreferences = false;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = new LastAuditDateTime(asOfDateTime.Value);

        foreach (var item in orderedItems)
            Apply(item);
    }

    public string ToData() => $"{UserID.ToData()}|{ValuationDateOption}|{ValuationDateBasis}|{ShowZeroBalances}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(UserValuationPreferences)}: (UserID: {UserID.ToDetail()}, ValuationDateOption: {ValuationDateOption}, ValuationDateBasis: {ValuationDateBasis}, ShowZeroBalances: {ShowZeroBalances})";

    private void Apply(IUserValuationPreferencesEvent userValuationPreferencesEvent)
    {
        switch (userValuationPreferencesEvent)
        {
            case UserValuationPreferencesCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case UserValuationPreferencesModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported user valuation preference event type '{userValuationPreferencesEvent.GetType().Name}'.");
        }
    }

    private void Apply(UserValuationPreferencesCreatedEvent createdEvent)
    {
        ValuationDateOption = createdEvent.ValuationDateOption;
        ValuationDateBasis = createdEvent.ValuationDateBasis;
        ShowZeroBalances = createdEvent.ShowZeroBalances;
        HasStoredPreferences = true;
        UpdateProvenance(createdEvent);
    }

    private void Apply(UserValuationPreferencesModifiedEvent modifiedEvent)
    {
        ValuationDateOption = modifiedEvent.ValuationDateOption;
        ValuationDateBasis = modifiedEvent.ValuationDateBasis;
        ShowZeroBalances = modifiedEvent.ShowZeroBalances;
        HasStoredPreferences = true;
        UpdateProvenance(modifiedEvent);
    }

    private void UpdateProvenance(IUserValuationPreferencesEvent @event)
    {
        LastEventID = @event.EventID;
        LastAuditDateTime = @event.AuditDateTime;
    }
}
