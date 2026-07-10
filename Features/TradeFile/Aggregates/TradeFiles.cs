using System.Diagnostics.CodeAnalysis;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Trade files")]
public sealed record TradeFiles : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<TradeFile> Items { get; init; }

    [SetsRequiredMembers]
    public TradeFiles(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, IReadOnlyList<ITradeFileEvent> events)
    {
        ValuationDateTime = valuationDateTime; AsOfDateTime = asOfDateTime; Items = [];
        LastEventID = Constants.Initialisation.EmptyViewEventID; LastAuditDateTime = new(asOfDateTime.Value);
        foreach (var @event in events.Where(item => item.EventDateTime.Value <= valuationDateTime.Value && item.AuditDateTime.Value <= asOfDateTime.Value).OrderBy(item => item.EventDateTime.Value).ThenBy(item => item.AuditDateTime.Value).ThenBy(item => item.EventID.Value)) Apply(@event);
    }

    public void Apply(ITradeFileEvent @event)
    {
        if (@event is TradeFileRequestedEvent requested)
            Items.Add(new TradeFile { TradeFileID = requested.TradeFileID, BrokerLEI = requested.BrokerLEI, BrokerName = requested.BrokerName, Status = TradeFileStatus.Requested, FileNameTemplate = requested.FileNameTemplate, Columns = requested.Columns, SendConfig = requested.SendConfig, Tickets = requested.Tickets, ConfirmedTickets = [], LastEventID = requested.EventID, LastAuditDateTime = new(requested.AuditDateTime.Value) });
        else
        {
            var index = Items.FindIndex(item => item.TradeFileID == @event.TradeFileID);
            if (index < 0) throw new InvalidOperationException($"Trade file '{@event.TradeFileID}' was not found.");
            var item = Items[index];
            Items[index] = item with
            {
                Status = @event switch { TradeFileCreatedEvent => TradeFileStatus.Created, TradeFileSentEvent => TradeFileStatus.Sent, TradeFileAcknowledgedEvent => TradeFileStatus.Acknowledged, TradeFileTicketConfirmedEvent confirmed when item.ConfirmedTickets.Count + 1 < item.Tickets.Count => TradeFileStatus.InProgress, TradeFileTicketConfirmedEvent => TradeFileStatus.Completed, TradeFileCompletedEvent => TradeFileStatus.Completed, TradeFileFailedEvent => TradeFileStatus.Failed, _ => item.Status },
                StoredFileID = @event is TradeFileCreatedEvent created ? created.StoredFileID : item.StoredFileID,
                FileName = @event is TradeFileCreatedEvent fileCreated ? fileCreated.FileName : item.FileName,
                Error = @event is TradeFileFailedEvent failed ? failed.Error : item.Error,
                ConfirmedTickets = @event is TradeFileTicketConfirmedEvent confirmedEvent ? [.. item.ConfirmedTickets.Where(ticket => ticket != confirmedEvent.TicketNumber), confirmedEvent.TicketNumber] : item.ConfirmedTickets,
                LastEventID = @event.EventID,
                LastAuditDateTime = new(@event.AuditDateTime.Value)
            };
        }
        LastEventID = @event.EventID; LastAuditDateTime = new(@event.AuditDateTime.Value);
    }

    public TradeFile? Find(TradeFileID id) => Items.SingleOrDefault(item => item.TradeFileID == id);
}
