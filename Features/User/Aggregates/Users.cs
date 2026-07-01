using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;


namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Users")]
public sealed record Users : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<User> Items { get; init; }

    [SetsRequiredMembers]
    public Users(EventDateTime valuationDateTime, List<IUserEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Users(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IUserEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null user events.", nameof(items));

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = LastAuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value);
        Items = [];

        var orderedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        foreach (var item in orderedItems)
            Apply(item);
    }

    public void Apply(IUserEvent userEvent)
    {
        switch (userEvent)
        {
            case UserCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case UserModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            case UserSignedInEvent signedInEvent:
                Apply(signedInEvent);
                break;
            case UserSignedOutEvent signedOutEvent:
                Apply(signedOutEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported user event type '{userEvent.GetType().Name}'.");
        }
    }

    public User? Find(UserID userID) =>
        Items.SingleOrDefault(user => user.UserID == userID);

    private void Apply(UserCreatedEvent createdEvent)
    {
        if (Items.Any(user => user.UserID == createdEvent.UserID))
            throw new InvalidOperationException($"User already exists for UserID '{createdEvent.UserID}'.");

        Items.Add(UserBuilder.Create(createdEvent));
        UpdateProvenance(createdEvent);
    }

    private void Apply(UserModifiedEvent modifiedEvent)
    {
        var index = Items.FindIndex(user => user.UserID == modifiedEvent.UserID);
        if (index < 0)
            throw new InvalidOperationException($"No matching user found for UserID '{modifiedEvent.UserID}'.");

        Items[index] = Items[index].Apply(modifiedEvent);
        UpdateProvenance(modifiedEvent);
    }

    private void Apply(UserSignedInEvent signedInEvent)
    {
        var index = Items.FindIndex(user => user.UserID == signedInEvent.UserID);
        if (index < 0)
            throw new InvalidOperationException($"No matching user found for UserID '{signedInEvent.UserID}'.");

        Items[index] = Items[index].Apply(signedInEvent);
        UpdateProvenance(signedInEvent);
    }

    private void Apply(UserSignedOutEvent signedOutEvent)
    {
        var index = Items.FindIndex(user => user.UserID == signedOutEvent.UserID);
        if (index < 0)
            throw new InvalidOperationException($"No matching user found for UserID '{signedOutEvent.UserID}'.");

        Items[index] = Items[index].Apply(signedOutEvent);
        UpdateProvenance(signedOutEvent);
    }

    private void UpdateProvenance(IUserEvent @event)
    {
        LastEventID = @event.EventID;
        LastAuditDateTime = LastAuditDateTimeBuilder.Create(Items.Max(user => user.LastAuditDateTime.Value));
    }

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IUserEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null user events.", nameof(items));

        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Any()
            ? AuditDateTimeBuilder.Create(includedItems.Max(@event => @event.AuditDateTime.Value))
            : AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value);
    }
}
