using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Input control settings")]
public sealed record InputControlSettings : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<InputControlSetting> Items { get; init; }

    [SetsRequiredMembers]
    public InputControlSettings(EventDateTime valuationDateTime, List<IInputControlSettingsEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InputControlSettings(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IInputControlSettingsEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null input control setting events.", nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = includedItems.LastOrDefault()?.EventID ?? Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = includedItems.Count == 0
            ? new LastAuditDateTime(asOfDateTime.Value)
            : new LastAuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
        Items = [];

        foreach (var item in includedItems)
            Apply(item);
    }

    public void Apply(IInputControlSettingsEvent inputControlSettingsEvent)
    {
        switch (inputControlSettingsEvent)
        {
            case InputControlSettingsCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case InputControlSettingsModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported input control settings event type '{inputControlSettingsEvent.GetType().Name}'.");
        }
    }

    private void Apply(InputControlSettingsCreatedEvent createdEvent) => Replace(createdEvent.Settings, createdEvent);

    private void Apply(InputControlSettingsModifiedEvent modifiedEvent) => Replace(modifiedEvent.Settings, modifiedEvent);

    private void Replace(IReadOnlyList<InputControlSettingDefinition> settings, IEventBase source)
    {
        Items.Clear();
        Items.AddRange(settings.Select(setting => new InputControlSetting(setting, source)));
        LastEventID = source.EventID;
        LastAuditDateTime = new LastAuditDateTime(source.AuditDateTime.Value);
    }

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IInputControlSettingsEvent> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Any()
            ? new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value))
            : Constants.Initialisation.AuditDateTime;
    }
}
