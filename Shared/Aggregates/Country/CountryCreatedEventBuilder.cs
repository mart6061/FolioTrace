using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class CountryCreatedEventBuilder
{
    public static CountryCreatedEvent Create(EventID eventId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha2 alpha2, Alpha3 alpha3, short numeric) =>
        new CountryCreatedEvent(eventId, eventDateTime, auditDateTime, reason, alpha2, alpha3, numeric);
}
