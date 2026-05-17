using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class CountryFlagModifiedEventBuilder
{
    public static CountryFlagModifiedEvent Create(EventID eventId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, Alpha2 alpha2, CountryFlag flag) =>
        new CountryFlagModifiedEvent(eventId, eventDateTime, auditDateTime, reason, alpha2, flag);
}
