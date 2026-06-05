using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class BrokersBuilder
{
    public static IReadOnlyList<Type> GetBrokerEventTypes() =>
    [
        typeof(BrokerCreatedEvent),
        typeof(BrokerModifiedEvent),
        typeof(BrokerActiveSetEvent),
        typeof(BrokerApprovedDateTimeSetEvent),
        typeof(BrokerNextReviewSetEvent),
        typeof(BrokerNotesSetEvent)
    ];

    public static Brokers Create(EventDateTime eventDate, AuditDateTime auditDateTime, List<IBrokerEvent> brokerEvents)
    {
        if (eventDate is null)
            throw new ArgumentNullException(nameof(eventDate));

        if (auditDateTime is null)
            throw new ArgumentNullException(nameof(auditDateTime));

        if (brokerEvents is null)
            throw new ArgumentNullException(nameof(brokerEvents));

        if (brokerEvents.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null broker events.", nameof(brokerEvents));

        return new Brokers(eventDate.Value, auditDateTime, brokerEvents);
    }
}
