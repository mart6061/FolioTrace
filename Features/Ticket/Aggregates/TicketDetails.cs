using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketDetails : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<TicketDetail> Items { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public TicketDetails(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime, List<TicketDetail> items)
    {
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        LastEventID = lastEventID ?? throw new ArgumentNullException(nameof(lastEventID));
        LastAuditDateTime = lastAuditDateTime ?? throw new ArgumentNullException(nameof(lastAuditDateTime));
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    [SetsRequiredMembers]
    public TicketDetails(Tickets tickets, Instruments instruments, Accounts accounts, bool includeClosed = false)
    {
        if (tickets is null)
            throw new ArgumentNullException(nameof(tickets));
        if (instruments is null)
            throw new ArgumentNullException(nameof(instruments));
        if (accounts is null)
            throw new ArgumentNullException(nameof(accounts));

        ValuationDateTime = tickets.ValuationDateTime;
        AsOfDateTime = tickets.AsOfDateTime;
        LastEventID = tickets.LastEventID;
        LastAuditDateTime = tickets.LastAuditDateTime;
        Items = tickets.Items
            .Where(ticket => includeClosed || ticket.IsActive)
            .Select(ticket => TicketDetail.From(
                ticket,
                instruments.Items.SingleOrDefault(instrument => instrument.InstrumentID == ticket.InstrumentID),
                ticket.AccountIDs
                    .Select(accountID => accounts.Items.SingleOrDefault(account => account.AccountID == accountID))
                    .Where(account => account is not null)
                    .Cast<Account>()
                    .ToList()))
            .ToList();
    }

    public TicketDetail? Find(TicketNumber ticketNumber) =>
        Items.SingleOrDefault(ticket => ticket.TicketNumber == ticketNumber);
}
