using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class TransactionEventSelector
{
    public static IReadOnlyList<ITransactionMovementEvent> GetActiveMovements(IEnumerable<ITransactionEvent> events, AuditDateTime? asAtDateTime = null)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var eventData = events
            .Where(@event => asAtDateTime is null || @event.AuditDateTime.Value <= asAtDateTime.Value)
            .ToList();
        var cancellationEvents = eventData
            .OfType<TransactionCancellationEvent>()
            .ToList();
        var cancelledEventIds = cancellationEvents
            .SelectMany(@event => @event.CancelledIDGroup)
            .Select(eventId => eventId.Value)
            .ToHashSet();
        var cancelledEventSetIds = eventData
            .OfType<ITransactionMovementEvent>()
            .Where(@event => cancelledEventIds.Contains(@event.EventID.Value))
            .Select(@event => @event.EventSetID.Value)
            .ToHashSet();
        var adjustmentsByEventSet = eventData
            .OfType<TransactionBookCostAdjustedEvent>()
            .Where(@event => !cancelledEventSetIds.Contains(@event.EventSetID.Value))
            .GroupBy(@event => @event.EventSetID.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(@event => @event.EventDateTime.Value)
                    .ThenBy(@event => @event.AuditDateTime.Value)
                    .ThenBy(@event => @event.EventID.Value)
                    .Last());

        return eventData
            .OfType<ITransactionMovementEvent>()
            .Where(@event => !cancelledEventIds.Contains(@event.EventID.Value))
            .Where(@event => !cancelledEventSetIds.Contains(@event.EventSetID.Value))
            .Select(@event => adjustmentsByEventSet.TryGetValue(@event.EventSetID.Value, out var adjustment)
                ? ApplyAdjustment(@event, adjustment)
                : @event)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();
    }

    public static bool IsCredit(ITransactionMovementEvent movement) =>
        movement.Type == nameof(TransactionCreditEvent);

    public static bool IsDebit(ITransactionMovementEvent movement) =>
        movement.Type == nameof(TransactionDebitEvent);

    private static ITransactionMovementEvent ApplyAdjustment(ITransactionMovementEvent movement, TransactionBookCostAdjustedEvent adjustment) =>
        new EffectiveTransactionMovement(
            movement.Type,
            adjustment.EventID,
            movement.UserID,
            movement.EventDateTime,
            adjustment.AuditDateTime,
            adjustment.Reason,
            movement.SettlementDateTime,
            movement.EventSetID,
            movement.EventIDGroup,
            movement.HoldingID,
            movement.InstrumentID,
            movement.AccountID,
            movement.Quantity,
            movement.LocalCost,
            movement.LocalCostCurrency,
            adjustment.BookCost,
            adjustment.BookCostSource,
            adjustment.BookCostEstimated);

    private sealed record EffectiveTransactionMovement(
        string Type,
        EventID EventID,
        UserID UserID,
        EventDateTime EventDateTime,
        AuditDateTime AuditDateTime,
        string Reason,
        SettlementDateTime SettlementDateTime,
        EventSetID EventSetID,
        IReadOnlyList<EventID> EventIDGroup,
        HoldingID HoldingID,
        InstrumentID InstrumentID,
        AccountID AccountID,
        TransactionQuantity Quantity,
        TransactionLocalCost LocalCost,
        Alpha3 LocalCostCurrency,
        TransactionBookCost BookCost,
        BookCostSource BookCostSource,
        bool BookCostEstimated) : ITransactionMovementEvent;
}
