using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class TransactionBookCostAdjustedEventBuilder
{
    public static Result<TransactionBookCostAdjustedEvent> Create(TransactionBookCostAdjustmentRequest request, IReadOnlyList<ITransactionEvent> transactionEvents)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (transactionEvents is null)
            throw new ArgumentNullException(nameof(transactionEvents));

        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
            return Result<TransactionBookCostAdjustedEvent>.Invalid(validationErrors);

        var originalEvents = transactionEvents
            .OfType<ITransactionMovementEvent>()
            .Where(@event => @event.EventSetID == request.EventSetID)
            .ToList();

        if (originalEvents.Count == 0)
            return Result<TransactionBookCostAdjustedEvent>.Invalid([$"No transaction events found for EventSetID '{request.EventSetID}'."]);

        if (IsCancelled(request.EventSetID, transactionEvents))
            return Result<TransactionBookCostAdjustedEvent>.Invalid([$"Transaction set '{request.EventSetID}' has been cancelled."]);

        validationErrors.AddRange(ValidateOriginalSet(originalEvents));
        if (validationErrors.Count > 0)
            return Result<TransactionBookCostAdjustedEvent>.Invalid(validationErrors);

        return Result<TransactionBookCostAdjustedEvent>.Success(new TransactionBookCostAdjustedEvent(
            new EventID(Guid.CreateGuid7()),
            request.UserID,
            originalEvents[0].EventDateTime,
            originalEvents[0].SettlementDateTime,
            AuditDateTimeBuilder.Create(),
            request.Reason,
            request.EventSetID,
            originalEvents.Select(@event => @event.EventID).ToList(),
            originalEvents[0].AccountID,
            request.BookCost,
            request.BookCostSource,
            request.BookCostEstimated));
    }

    private static List<string> ValidateRequest(TransactionBookCostAdjustmentRequest request)
    {
        var messages = new List<string>();
        if (request.UserID is null) messages.Add("UserID is required.");
        if (string.IsNullOrWhiteSpace(request.Reason)) messages.Add("Reason is required.");
        if (request.EventSetID is null) messages.Add("EventSetID is required.");
        if (request.BookCost is null) messages.Add("BookCost is required.");
        if (request.BookCost?.Value <= 0m) messages.Add("BookCost must be greater than zero.");
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

        return messages;
    }

    private static bool IsCancelled(EventSetID eventSetID, IReadOnlyList<ITransactionEvent> transactionEvents)
    {
        var movementIds = transactionEvents
            .OfType<ITransactionMovementEvent>()
            .Where(@event => @event.EventSetID == eventSetID)
            .Select(@event => @event.EventID.Value)
            .ToHashSet();

        return transactionEvents
            .OfType<TransactionCancellationEvent>()
            .Any(@event => @event.CancelledIDGroup.Any(cancelled => movementIds.Contains(cancelled.Value)));
    }
}
