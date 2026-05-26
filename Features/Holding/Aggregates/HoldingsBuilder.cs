using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingsBuilder
{
    public static Holdings Create(EventDateTime valuationDateTime, IEnumerable<IHoldingEvent> events) =>
        new(valuationDateTime, events.ToList());

    public static Holdings Create(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, IEnumerable<IHoldingEvent> events) =>
        new(valuationDateTime, asOfDateTime, events.ToList());
}
