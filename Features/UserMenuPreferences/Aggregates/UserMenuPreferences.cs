using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserMenuPreferences : IModel
{
    public required UserID UserID { get; init; }

    public List<UserMenuPreferenceItem> Items { get; private set; }

    public bool HasStoredPreferences { get; private set; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public UserMenuPreferences(
        UserID userID,
        List<UserMenuPreferenceItem> items,
        bool hasStoredPreferences,
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        EventID lastEventID,
        LastAuditDateTime lastAuditDateTime)
    {
        UserID = userID;
        Items = UserMenuPreferenceDefaults.Normalize(items).ToList();
        HasStoredPreferences = hasStoredPreferences;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public UserMenuPreferences(UserID userID, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IUserMenuPreferencesEvent> items)
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
            throw new ArgumentException("Value must not contain null user menu preference events.", nameof(items));

        var orderedItems = items
            .Where(@event => @event.UserID == userID && @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        UserID = userID;
        Items = UserMenuPreferenceDefaults.CreateVisibleItems().ToList();
        HasStoredPreferences = false;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = new LastAuditDateTime(asOfDateTime.Value);

        foreach (var item in orderedItems)
            Apply(item);
    }

    public bool IsVisible(string menuItemID) =>
        !UserMenuPreferenceDefaults.IsControlled(menuItemID) || Items.Single(item => item.MenuItemID == menuItemID).Visible;

    public string ToData() => $"{UserID.ToData()}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(UserMenuPreferences)}: (UserID: {UserID.ToDetail()}, Items: {Items.Count})";

    private void Apply(IUserMenuPreferencesEvent userMenuPreferencesEvent)
    {
        switch (userMenuPreferencesEvent)
        {
            case UserMenuPreferencesCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case UserMenuPreferencesModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported user menu preference event type '{userMenuPreferencesEvent.GetType().Name}'.");
        }
    }

    private void Apply(UserMenuPreferencesCreatedEvent createdEvent)
    {
        Items = UserMenuPreferenceDefaults.Normalize(createdEvent.Items).ToList();
        HasStoredPreferences = true;
        UpdateProvenance(createdEvent);
    }

    private void Apply(UserMenuPreferencesModifiedEvent modifiedEvent)
    {
        Items = UserMenuPreferenceDefaults.Normalize(modifiedEvent.Items).ToList();
        HasStoredPreferences = true;
        UpdateProvenance(modifiedEvent);
    }

    private void UpdateProvenance(IUserMenuPreferencesEvent @event)
    {
        LastEventID = @event.EventID;
        LastAuditDateTime = @event.AuditDateTime;
    }
}
