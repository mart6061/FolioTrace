using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class TransactionCancellationEventBuilder
{
    public static Result<IReadOnlyList<TransactionCancellationEvent>> Create(TransactionCancellationRequest request, IReadOnlyList<ITransactionEvent> transactionEvents)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (transactionEvents is null)
            throw new ArgumentNullException(nameof(transactionEvents));

        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
            return Result<IReadOnlyList<TransactionCancellationEvent>>.Invalid(validationErrors);

        var originalEvents = transactionEvents
            .OfType<ITransactionMovementEvent>()
            .Where(@event => @event.EventSetID == request.EventSetID)
            .ToList();

        if (originalEvents.Count == 0)
            return Result<IReadOnlyList<TransactionCancellationEvent>>.Invalid([$"No transaction events found for EventSetID '{request.EventSetID}'."]);

        var cancelledEventIds = originalEvents
            .Select(@event => @event.EventID.Value)
            .ToHashSet();

        if (transactionEvents
            .OfType<TransactionCancellationEvent>()
            .Any(@event => @event.CancelledIDGroup.Any(cancelled => cancelledEventIds.Contains(cancelled.Value))))
        {
            throw new InvalidOperationException($"Transaction set '{request.EventSetID}' has already been cancelled.");
        }

        validationErrors.AddRange(ValidateOriginalSet(originalEvents));
        if (validationErrors.Count > 0)
            return Result<IReadOnlyList<TransactionCancellationEvent>>.Invalid(validationErrors);

        var eventDateTime = originalEvents[0].EventDateTime;
        var settlementDateTime = originalEvents[0].SettlementDateTime;
        var accountID = originalEvents[0].AccountID;
        var auditDateTime = AuditDateTimeBuilder.Create();
        var cancellationEventSetID = EventSetIDBuilder.Create();
        var eventIDGroup = Enumerable.Range(0, originalEvents.Count)
            .Select(_ => new EventID(Guid.NewGuid()))
            .ToList();
        var cancelledIDGroup = originalEvents.Select(@event => @event.EventID).ToList();

        var cancellationEvents = originalEvents
            .Select((@event, index) => new TransactionCancellationEvent(
                eventIDGroup[index],
                request.UserID,
                eventDateTime,
                settlementDateTime,
                auditDateTime,
                request.Reason,
                cancellationEventSetID,
                eventIDGroup,
                accountID,
                @event.EventID,
                cancelledIDGroup))
            .ToList();

        return Result<IReadOnlyList<TransactionCancellationEvent>>.Success(cancellationEvents);
    }

    private static List<string> ValidateRequest(TransactionCancellationRequest request)
    {
        var messages = new List<string>();
        if (request.UserID is null) messages.Add("UserID is required.");
        if (string.IsNullOrWhiteSpace(request.Reason)) messages.Add("Reason is required.");
        if (request.EventSetID is null) messages.Add("EventSetID is required.");
        return messages;
    }

    private static IReadOnlyList<string> ValidateOriginalSet(IReadOnlyList<ITransactionMovementEvent> originalEvents)
    {
        var messages = new List<string>();
        if (originalEvents.Count < 2)
            messages.Add("At least two original transaction events are required.");

        if (originalEvents.Select(@event => @event.EventDateTime.Value).Distinct().Count() != 1)
            messages.Add("All original transaction events must have the same EventDateTime.");

        if (originalEvents.Select(@event => @event.SettlementDateTime.Value).Distinct().Count() != 1)
            messages.Add("All original transaction events must have the same SettlementDateTime.");

        if (originalEvents.Select(@event => @event.AccountID.Value).Distinct().Count() != 1)
            messages.Add("All original transaction events must have the same AccountID.");

        var expectedEventIds = originalEvents.Select(@event => @event.EventID).ToList();
        foreach (var @event in originalEvents)
        {
            if (@event.EventIDGroup is null || @event.EventIDGroup.Count == 0)
            {
                messages.Add($"Transaction event '{@event.EventID}' has no EventIDGroup.");
                continue;
            }

            if (!ContainsSameEventIds(@event.EventIDGroup, expectedEventIds))
                messages.Add($"Transaction event '{@event.EventID}' does not contain a complete EventIDGroup.");
        }

        return messages;
    }

    private static bool ContainsSameEventIds(IReadOnlyList<EventID> left, IReadOnlyList<EventID> right)
    {
        var leftIds = left.Select(eventId => eventId.Value).OrderBy(value => value).ToArray();
        var rightIds = right.Select(eventId => eventId.Value).OrderBy(value => value).ToArray();
        return leftIds.SequenceEqual(rightIds);
    }
}
