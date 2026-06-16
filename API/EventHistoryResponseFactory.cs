using FolioTrace.Common;

namespace API;

public static class EventHistoryResponseFactory
{
    public static IReadOnlyList<object> Create<TEvent>(
        IEnumerable<TEvent> events,
        DateTime? valuationDateTime,
        DateTime? auditDateTime,
        Func<TEvent, object> createResponse)
        where TEvent : IAuditEventBase =>
        events
            .Where(@event => @event is not IEventBase timedEvent || !valuationDateTime.HasValue || timedEvent.EventDateTime.Value <= valuationDateTime.Value)
            .OrderBy(@event => @event is IEventBase timedEvent ? timedEvent.EventDateTime.Value : @event.AuditDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .Select(@event => WithApplicationStatus(createResponse(@event), @event, auditDateTime))
            .ToList();

    private static object WithApplicationStatus(object response, IAuditEventBase @event, DateTime? auditDateTime)
    {
        var applicationStatus = !auditDateTime.HasValue || @event.AuditDateTime.Value <= auditDateTime.Value
            ? "applied"
            : "omitted";

        if (response is EventResponseWithPropertyDetails eventResponse)
        {
            eventResponse.Properties["applicationStatus"] = applicationStatus;
            return eventResponse;
        }

        return new
        {
            Event = response,
            ApplicationStatus = applicationStatus
        };
    }
}
