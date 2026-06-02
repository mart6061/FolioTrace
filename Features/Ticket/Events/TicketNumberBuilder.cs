using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static TicketNumber NextTicketNumber(IEnumerable<ITicket> events)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var max = events
            .OfType<TicketCreatedEvent>()
            .Select(@event => @event.TicketNumber.Value)
            .DefaultIfEmpty(0)
            .Max();
        return new TicketNumber(max + 1);
    }
}
