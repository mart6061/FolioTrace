using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Holding positions")]
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

        var selectedHoldings = SelectHoldings(holdings, filter);

        var scopedTransactionEvents = transactionEvents
            .Where(@event => GetHoldingDate(@event, holdingDateBasis) <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .ToList();
        var movementByHolding = BuildMovementTotals(scopedTransactionEvents, asOfDateTime, selectedHoldings);

        Items = BuildItems(selectedHoldings, accounts, instruments, valuationDateTime, holdingDateBasis, asOfDateTime, movementByHolding, filter.IncludeZero);

        var latestTransactionEvent = LatestEvent(scopedTransactionEvents.Cast<IEventBase>());
        (LastEventID, LastAuditDateTime) = ResolveLastEvent(
            latestTransactionEvent?.EventID,
            latestTransactionEvent?.AuditDateTime.Value,
            holdings);
    }

    /// <summary>
    /// Seeded-reconstruction path (Aggregate-Snapshot-Scaling-Plan.md 3.3): instead of scanning the whole
    /// transaction stream, starts each holding's totals from a previously-persisted snapshot and applies only
    /// the delta transaction events since the snapshot's boundary. Safe because a bitemporal correction
    /// (TransactionBookCostAdjustedEvent/TransactionCancellationEvent) always carries the same EventDateTime as
    /// the original movement it corrects (see the builders), so 3.4's snapshot retirement - using the exact
    /// same InvalidateFrom condition as the in-memory cache - always retires any snapshot whose baseline could
    /// have included a since-corrected movement before it would ever reach this constructor.
    /// </summary>
    [SetsRequiredMembers]
    internal HoldingPositions(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        Holdings holdings,
        Accounts accounts,
        Instruments instruments,
        HoldingPositionFilter filter,
        IReadOnlyDictionary<Guid, HoldingPositionTotals> baselineTotals,
        EventID snapshotLastEventID,
        AuditDateTime snapshotLastAuditDateTime,
        IReadOnlyList<ITransactionEvent> deltaTransactionEvents)
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
        if (baselineTotals is null)
            throw new ArgumentNullException(nameof(baselineTotals));
        if (deltaTransactionEvents is null)
            throw new ArgumentNullException(nameof(deltaTransactionEvents));

        ValuationDateTime = valuationDateTime;
        HoldingDateBasis = holdingDateBasis;
        AsOfDateTime = asOfDateTime;

        var selectedHoldings = SelectHoldings(holdings, filter);

        var scopedDeltaEvents = deltaTransactionEvents
            .Where(@event => GetHoldingDate(@event, holdingDateBasis) <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .ToList();
        var deltaByHolding = BuildMovementTotals(scopedDeltaEvents, asOfDateTime, selectedHoldings);

        var movementByHolding = MergeTotals(baselineTotals, deltaByHolding);

        Items = BuildItems(selectedHoldings, accounts, instruments, valuationDateTime, holdingDateBasis, asOfDateTime, movementByHolding, filter.IncludeZero);

        var latestDeltaEvent = LatestEvent(scopedDeltaEvents.Cast<IEventBase>());
        var latestOverallEventID = snapshotLastEventID;
        var latestOverallAuditDateTime = snapshotLastAuditDateTime.Value;
        if (latestDeltaEvent is not null && latestDeltaEvent.AuditDateTime.Value > latestOverallAuditDateTime)
        {
            latestOverallEventID = latestDeltaEvent.EventID;
            latestOverallAuditDateTime = latestDeltaEvent.AuditDateTime.Value;
        }

        (LastEventID, LastAuditDateTime) = ResolveLastEvent(latestOverallEventID, latestOverallAuditDateTime, holdings);
    }

    private static Dictionary<Guid, HoldingBase> SelectHoldings(Holdings holdings, HoldingPositionFilter filter) =>
        holdings.Items
            .Where(holding => holding.Active)
            .Where(holding => filter.IncludeExcluded || holding.IncludeInValuation)
            .Where(holding => filter.AccountID is null || holding.AccountID == filter.AccountID)
            .Where(holding => filter.InstrumentID is null || holding.InstrumentID == filter.InstrumentID)
            .Where(holding => filter.HoldingID is null || holding.HoldingID == filter.HoldingID)
            .ToDictionary(holding => holding.HoldingID.Value);

    private static Dictionary<Guid, HoldingPositionTotals> BuildMovementTotals(
        IReadOnlyList<ITransactionEvent> scopedTransactionEvents,
        AuditDateTime asOfDateTime,
        IReadOnlyDictionary<Guid, HoldingBase> selectedHoldings)
    {
        var activeMovements = TransactionEventSelector.GetActiveMovements(scopedTransactionEvents, asOfDateTime)
            .Where(movement => movement.HoldingID is not null && selectedHoldings.ContainsKey(movement.HoldingID.Value))
            .ToList();

        return activeMovements
            .GroupBy(movement => movement.HoldingID.Value)
            .ToDictionary(
                group => group.Key,
                group => new HoldingPositionTotals(
                    group.Sum(GetSignedQuantity),
                    group.Sum(GetSignedBookCost),
                    LatestEvent(group.Cast<ITransactionEvent>())?.EventID,
                    LatestEvent(group.Cast<ITransactionEvent>())?.AuditDateTime));
    }

    private static Dictionary<Guid, HoldingPositionTotals> MergeTotals(
        IReadOnlyDictionary<Guid, HoldingPositionTotals> baseline,
        IReadOnlyDictionary<Guid, HoldingPositionTotals> delta)
    {
        var merged = new Dictionary<Guid, HoldingPositionTotals>();

        foreach (var holdingID in baseline.Keys.Concat(delta.Keys).Distinct())
        {
            baseline.TryGetValue(holdingID, out var baselineTotals);
            delta.TryGetValue(holdingID, out var deltaTotals);

            var quantity = (baselineTotals?.Quantity ?? 0m) + (deltaTotals?.Quantity ?? 0m);
            var bookCost = (baselineTotals?.BookCost ?? 0m) + (deltaTotals?.BookCost ?? 0m);

            var useDelta = deltaTotals is not null
                && (baselineTotals is null || deltaTotals.LastAuditDateTime?.Value > baselineTotals.LastAuditDateTime?.Value);
            var lastEventID = useDelta ? deltaTotals!.LastEventID : baselineTotals?.LastEventID;
            var lastAuditDateTime = useDelta ? deltaTotals!.LastAuditDateTime : baselineTotals?.LastAuditDateTime;

            merged[holdingID] = new HoldingPositionTotals(quantity, bookCost, lastEventID, lastAuditDateTime);
        }

        return merged;
    }

    private static List<HoldingPosition> BuildItems(
        IReadOnlyDictionary<Guid, HoldingBase> selectedHoldings,
        Accounts accounts,
        Instruments instruments,
        EventDateTime valuationDateTime,
        HoldingDateBasis holdingDateBasis,
        AuditDateTime asOfDateTime,
        IReadOnlyDictionary<Guid, HoldingPositionTotals> movementByHolding,
        bool includeZero)
    {
        var accountById = accounts.Items.ToDictionary(account => account.AccountID.Value);
        var instrumentById = instruments.Items.ToDictionary(instrument => instrument.InstrumentID.Value);

        var items = new List<HoldingPosition>();
        foreach (var holding in selectedHoldings.Values.OrderBy(holding => holding.AccountID.Value).ThenBy(holding => holding.InstrumentID.Value).ThenBy(holding => holding.Name))
        {
            movementByHolding.TryGetValue(holding.HoldingID.Value, out var totals);
            var quantity = totals?.Quantity ?? 0m;
            var bookCost = totals?.BookCost ?? 0m;

            if (!includeZero && quantity == 0m && bookCost == 0m)
                continue;

            if (!accountById.TryGetValue(holding.AccountID.Value, out var account))
                throw new InvalidOperationException($"No matching Account found for AccountID '{holding.AccountID}'.");

            if (!instrumentById.TryGetValue(holding.InstrumentID.Value, out var instrument))
                throw new InvalidOperationException($"No matching Instrument found for InstrumentID '{holding.InstrumentID}'.");

            items.Add(new HoldingPosition(
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

        return items;
    }

    private static (EventID LastEventID, LastAuditDateTime LastAuditDateTime) ResolveLastEvent(
        EventID? candidateEventID,
        DateTime? candidateAuditDateTime,
        Holdings holdings) =>
        candidateEventID is not null && candidateAuditDateTime > holdings.LastAuditDateTime.Value
            ? (candidateEventID, new LastAuditDateTime(candidateAuditDateTime!.Value))
            : (holdings.LastEventID, holdings.LastAuditDateTime);

    private static DateTime GetHoldingDate(ITransactionEvent @event, HoldingDateBasis holdingDateBasis) =>
        holdingDateBasis switch
        {
            HoldingDateBasis.SettlementDateTime => @event.SettlementDateTime.Value,
            _ => @event.EventDateTime.Value
        };

    private static decimal GetSignedQuantity(ITransactionMovementEvent movement) =>
        TransactionEventSelector.IsCredit(movement)
            ? movement.Quantity.Value
            : TransactionEventSelector.IsDebit(movement)
                ? -movement.Quantity.Value
                : 0m;

    private static decimal GetSignedBookCost(ITransactionMovementEvent movement) =>
        TransactionEventSelector.IsCredit(movement)
            ? movement.BookCost.Value
            : TransactionEventSelector.IsDebit(movement)
                ? -movement.BookCost.Value
                : 0m;

    private static IEventBase? LatestEvent(IEnumerable<IEventBase> events) =>
        events
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .LastOrDefault();

    internal sealed record HoldingPositionTotals(decimal Quantity, decimal BookCost, EventID? LastEventID, AuditDateTime? LastAuditDateTime);
}
