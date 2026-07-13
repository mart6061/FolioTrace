using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class TicketTradeExecutionEventBuilder
{
    public static Result<ITicket> Request(TicketTradeExecutionRequest request, Tickets tickets, Brokers brokers)
    {
        var errors = new List<string>();
        var ticket = tickets.Find(request.TicketNumber);
        var broker = brokers.Items.SingleOrDefault(item => item.LEI == request.BrokerLEI);
        if (ticket is null) errors.Add("Ticket was not found.");
        if (broker is null || !broker.Active) errors.Add("Broker was not found or is inactive.");
        if (broker is not null && broker.TradeMethods.All(method => method.Type != request.TradeMethodType)) errors.Add($"Broker does not support {request.TradeMethodType} trading.");
        if (ticket is not null)
        {
            if (ticket.Stage != TicketStage.Trade || ticket.ProposalDecision != TicketDecision.Approved || ticket.TradeDecision != TicketDecision.InProgress)
                errors.Add("Ticket is not ready for trade execution.");
            if (ticket.ProposalTargetPrice is null || ticket.ProposalAllocations.Count == 0 || ticket.TradePrice is not null || ticket.TradeAllocations.Count > 0 || ticket.Fills.Count > 0)
                errors.Add("Ticket must have approved proposal details and no existing trade or fills.");
            if (ticket.TradeExecutionStatus is not TicketTradeExecutionStatus.Ready and not TicketTradeExecutionStatus.Failed)
                errors.Add($"Ticket execution status is {ticket.TradeExecutionStatus}.");
        }
        if (errors.Count > 0) return Result<ITicket>.Invalid(errors);

        var eventID = new EventID(Guid.CreateGuid7());
        var auditDateTime = AuditDateTimeBuilder.Create();
        ITicket @event = request.TradeMethodType switch
        {
            TradeMethodType.FIX => new TicketFIXRequestedEvent(eventID, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.TicketNumber, request.BrokerLEI),
            TradeMethodType.TradeFile => new TicketTradeFilePendingEvent(eventID, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.TicketNumber, request.BrokerLEI),
            _ => throw new InvalidOperationException("Only FIX and TradeFile execution requests are supported.")
        };
        return Result<ITicket>.Success(@event);
    }
}
