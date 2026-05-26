using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class TransactionEventSelector
{
    public static IReadOnlyList<ITransactionMovementEvent> GetActiveMovements(IEnumerable<ITransactionEvent> events, AuditDateTime? asAtDateTime = null)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var eventData = events.ToList();
        var cancelledEventIds = eventData
            .OfType<TransactionCancellationEvent>()
            .Where(@event => asAtDateTime is null || @event.AuditDateTime.Value <= asAtDateTime.Value)
            .SelectMany(@event => @event.CancelledIDGroup)
            .Select(eventId => eventId.Value)
            .ToHashSet();

        return eventData
            .OfType<ITransactionMovementEvent>()
            .Where(@event => !cancelledEventIds.Contains(@event.EventID.Value))
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();
    }
}
