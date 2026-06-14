using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Accounts : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<Account> Items { get; init; }

    [SetsRequiredMembers]
    public Accounts(EventDateTime valuationDateTime, List<IAccountEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Accounts(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IAccountEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null account events.", nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .ToList();
        if (!includedItems.Any())
        {
            ValuationDateTime = valuationDateTime;
            AsOfDateTime = asOfDateTime;
            LastEventID = Constants.Initialisation.EmptyViewEventID;
            LastAuditDateTime = new LastAuditDateTime(asOfDateTime.Value);
            Items = [];
            return;
        }

        var orderedItems = includedItems
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = orderedItems.Last().EventID;
        LastAuditDateTime = new LastAuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
        Items = [];

        foreach (var item in orderedItems)
            Apply(item);
    }

    public void Apply(IAccountEvent accountEvent)
    {
        switch (accountEvent)
        {
            case AccountCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case AccountModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            case AccountActiveSetEvent activeModifiedEvent:
                Apply(activeModifiedEvent);
                break;
            case AccountDisplayOrderSetEvent displayOrderSetEvent:
                Apply(displayOrderSetEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported account event type '{accountEvent.GetType().Name}'.");
        }
    }

    public void Apply(AccountCreatedEvent createdEvent)
    {
        if (Items.Any(account => account.AccountID == createdEvent.AccountID))
            throw new InvalidOperationException($"Account already exists for AccountID '{createdEvent.AccountID}'.");
        if (Items.Any(account => string.Equals(account.Name, createdEvent.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Account already exists for Name '{createdEvent.Name}'.");

        Items.Add(AccountBuilder.Create(createdEvent, new DisplayOrder(Items.Count + 1)));
        SortItems();
        LastEventID = createdEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(AccountModifiedEvent modifiedEvent)
    {
        var index = Items.FindIndex(account => account.AccountID == modifiedEvent.AccountID);
        if (index < 0)
            throw new InvalidOperationException($"No matching account found for AccountID '{modifiedEvent.AccountID}'.");
        if (Items.Where((_, itemIndex) => itemIndex != index).Any(account => string.Equals(account.Name, modifiedEvent.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Account already exists for Name '{modifiedEvent.Name}'.");

        Items[index] = Items[index].Apply(modifiedEvent);
        SortItems();
        LastEventID = modifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(AccountActiveSetEvent activeModifiedEvent)
    {
        var index = Items.FindIndex(account => account.AccountID == activeModifiedEvent.AccountID);
        if (index < 0)
            throw new InvalidOperationException($"No matching account found for AccountID '{activeModifiedEvent.AccountID}'.");

        Items[index] = Items[index].Apply(activeModifiedEvent);
        SortItems();
        LastEventID = activeModifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(AccountDisplayOrderSetEvent displayOrderSetEvent)
    {
        var index = Items.FindIndex(account => account.AccountID == displayOrderSetEvent.AccountID);
        if (index < 0)
            throw new InvalidOperationException($"No matching account found for AccountID '{displayOrderSetEvent.AccountID}'.");

        Items[index] = Items[index].Apply(displayOrderSetEvent);
        SortItems();
        LastEventID = displayOrderSetEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    private static LastAuditDateTime GetLastAuditDateTime(List<Account> items) =>
        new(items.Max(account => account.LastAuditDateTime.Value));

    private void SortItems() =>
        Items.Sort((left, right) =>
        {
            var displayOrder = left.DisplayOrder.Value.CompareTo(right.DisplayOrder.Value);
            return displayOrder != 0
                ? displayOrder
                : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        });

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IAccountEvent> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Any()
            ? new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value))
            : Constants.Initialisation.AuditDateTime;
    }
}
