using FolioTrace.Common;
using FolioTrace.Types;
using System.Diagnostics.CodeAnalysis;

namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Foleo Trader orders")]
public sealed record FoleoTraderOrders : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<FoleoTraderOrder> Items { get; init; }

    [SetsRequiredMembers]
    public FoleoTraderOrders(EventDateTime valuationDateTime, IReadOnlyList<IFoleoTraderOrderEvent> events)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, events), events)
    {
    }

    [SetsRequiredMembers]
    public FoleoTraderOrders(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, IReadOnlyList<IFoleoTraderOrderEvent> events)
    {
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        Items = [];
        LastEventID = Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = LastAuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value);

        foreach (var @event in events
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value))
        {
            Apply(@event);
        }
    }

    public FoleoTraderOrder? Find(TicketNumber ticketNumber) =>
        Items.LastOrDefault(order => order.TicketNumber == ticketNumber);

    public FoleoTraderOrder? FindByClOrdID(string clOrdID) =>
        Items.LastOrDefault(order => string.Equals(order.ClOrdID, clOrdID, StringComparison.Ordinal));

    public bool HasExecution(string execID) =>
        !string.IsNullOrWhiteSpace(execID) &&
        Items.Any(order => string.Equals(order.LastExecID, execID, StringComparison.Ordinal));

    private void Apply(IFoleoTraderOrderEvent @event)
    {
        switch (@event)
        {
            case FoleoTraderOrderSubmittedEvent submitted:
                Items.Add(new FoleoTraderOrder(
                    submitted.TicketNumber,
                    submitted.BrokerLEI,
                    submitted.ClOrdID,
                    FoleoTraderOrderStatus.Submitted,
                    submitted.OrderQuantity,
                    0m,
                    submitted.Price,
                    submitted.Currency,
                    submitted.Side,
                    submitted.SecurityID,
                    submitted.SecurityIDSource,
                    submitted.Symbol,
                    null,
                    null,
                    submitted.AuditDateTime.Value,
                    submitted.AuditDateTime.Value));
                break;

            case FoleoTraderExecutionReceivedEvent execution:
                Update(execution, order => order with
                {
                    Status = execution.LeavesQuantity <= 0 ? FoleoTraderOrderStatus.Filled : FoleoTraderOrderStatus.PartiallyFilled,
                    FilledQuantity = execution.CumQuantity,
                    LastExecID = execution.ExecID,
                    UpdatedAt = execution.AuditDateTime.Value
                });
                break;

            case FoleoTraderOrderFailedEvent failed:
                Update(failed, order => order with
                {
                    Status = FoleoTraderOrderStatus.Failed,
                    LastError = failed.Error,
                    UpdatedAt = failed.AuditDateTime.Value
                });
                break;
        }

        LastEventID = @event.EventID;
        LastAuditDateTime = LastAuditDateTimeBuilder.Create(@event.AuditDateTime.Value);
    }

    private void Update(IFoleoTraderOrderEvent @event, Func<FoleoTraderOrder, FoleoTraderOrder> update)
    {
        var index = Items.FindLastIndex(order => string.Equals(order.ClOrdID, @event.ClOrdID, StringComparison.Ordinal));
        if (index < 0)
            return;

        Items[index] = update(Items[index]);
    }

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, IReadOnlyList<IFoleoTraderOrderEvent> events)
    {
        var includedItems = events.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Count > 0
            ? AuditDateTimeBuilder.Create(includedItems.Max(@event => @event.AuditDateTime.Value))
            : AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value);
    }
}
