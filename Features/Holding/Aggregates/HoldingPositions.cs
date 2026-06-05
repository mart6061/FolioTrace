using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingPositions : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required HoldingDateBasis HoldingDateBasis { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<HoldingPosition> Items { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingPositions(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, Holdings holdings, Accounts accounts, Instruments instruments, IReadOnlyList<ITransactionEvent> transactionEvents, HoldingPositionFilter? filter = null, HoldingDateBasis holdingDateBasis = HoldingDateBasis.EventDateTime)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));
        if (holdings is null)
            throw new ArgumentNullException(nameof(holdings));
        if (accounts is null)
            throw new ArgumentNullException(nameof(accounts));
        if (instruments is null)
            throw new ArgumentNullException(nameof(instruments));
        if (transactionEvents is null)
            throw new ArgumentNullException(nameof(transactionEvents));

        filter ??= HoldingPositionFilter.Default;

        ValuationDateTime = valuationDateTime;
        HoldingDateBasis = holdingDateBasis;
        AsOfDateTime = asOfDateTime;

        var accountById = accounts.Items.ToDictionary(account => account.AccountID.Value);
        var instrumentById = instruments.Items.ToDictionary(instrument => instrument.InstrumentID.Value);
        var selectedHoldings = holdings.Items
            .Where(holding => holding.Active)
            .Where(holding => filter.IncludeExcluded || holding.IncludeInValuation)
            .Where(holding => filter.AccountID is null || holding.AccountID == filter.AccountID)
            .Where(holding => filter.InstrumentID is null || holding.InstrumentID == filter.InstrumentID)
            .Where(holding => filter.HoldingID is null || holding.HoldingID == filter.HoldingID)
            .ToDictionary(holding => holding.HoldingID.Value);

        var scopedTransactionEvents = transactionEvents
            .Where(@event => GetHoldingDate(@event, holdingDateBasis) <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .ToList();
        var activeMovements = TransactionEventSelector.GetActiveMovements(scopedTransactionEvents, asOfDateTime)
            .Where(movement => movement.HoldingID is not null && selectedHoldings.ContainsKey(movement.HoldingID.Value))
            .ToList();

        var movementByHolding = activeMovements
            .GroupBy(movement => movement.HoldingID.Value)
            .ToDictionary(
                group => group.Key,
                group => new HoldingPositionTotals(
                    group.Sum(GetSignedQuantity),
                    group.Sum(GetSignedBookCost),
                    LatestEvent(group.Cast<ITransactionEvent>())?.EventID,
                    LatestEvent(group.Cast<ITransactionEvent>())?.AuditDateTime));

        Items = [];
        foreach (var holding in selectedHoldings.Values.OrderBy(holding => holding.AccountID.Value).ThenBy(holding => holding.InstrumentID.Value).ThenBy(holding => holding.Name))
        {
            movementByHolding.TryGetValue(holding.HoldingID.Value, out var totals);
            var quantity = totals?.Quantity ?? 0m;
            var bookCost = totals?.BookCost ?? 0m;
            if (!filter.IncludeZero && quantity == 0m && bookCost == 0m)
                continue;

            if (!accountById.TryGetValue(holding.AccountID.Value, out var account))
                throw new InvalidOperationException($"No matching Account found for AccountID '{holding.AccountID}'.");

            if (!instrumentById.TryGetValue(holding.InstrumentID.Value, out var instrument))
                throw new InvalidOperationException($"No matching Instrument found for InstrumentID '{holding.InstrumentID}'.");

            Items.Add(new HoldingPosition(
                holding,
                account.Name,
                instrument.Name,
                quantity,
                bookCost,
                valuationDateTime,
                holdingDateBasis,
                asOfDateTime,
                totals?.LastEventID ?? holding.LastEventID,
                new LastAuditDateTime((totals?.LastAuditDateTime ?? holding.LastAuditDateTime).Value)));
        }

        var latestTransactionEvent = LatestEvent(scopedTransactionEvents.Cast<IEventBase>());
        if (latestTransactionEvent is not null && latestTransactionEvent.AuditDateTime.Value > holdings.LastAuditDateTime.Value)
        {
            LastEventID = latestTransactionEvent.EventID;
            LastAuditDateTime = new LastAuditDateTime(latestTransactionEvent.AuditDateTime.Value);
        }
        else
        {
            LastEventID = holdings.LastEventID;
            LastAuditDateTime = holdings.LastAuditDateTime;
        }
    }

    private static DateTime GetHoldingDate(ITransactionEvent @event, HoldingDateBasis holdingDateBasis) =>
        holdingDateBasis switch
        {
            HoldingDateBasis.SettlementDateTime => @event.SettlementDateTime.Value,
            _ => @event.EventDateTime.Value
        };

    private static decimal GetSignedQuantity(ITransactionMovementEvent movement) =>
        movement switch
        {
            TransactionCreditEvent => movement.Quantity.Value,
            TransactionDebitEvent => -movement.Quantity.Value,
            _ => 0m
        };

    private static decimal GetSignedBookCost(ITransactionMovementEvent movement) =>
        movement switch
        {
            TransactionCreditEvent => movement.BookCost.Value,
            TransactionDebitEvent => -movement.BookCost.Value,
            _ => 0m
        };

    private static IEventBase? LatestEvent(IEnumerable<IEventBase> events) =>
        events
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .LastOrDefault();

    private sealed record HoldingPositionTotals(decimal Quantity, decimal BookCost, EventID? LastEventID, AuditDateTime? LastAuditDateTime);
}

public sealed record HoldingPositionFilter(HoldingID? HoldingID, AccountID? AccountID, InstrumentID? InstrumentID, bool IncludeExcluded, bool IncludeZero)
{
    public static HoldingPositionFilter Default { get; } = new(null, null, null, false, false);
}
