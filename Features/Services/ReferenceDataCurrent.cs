using FolioTrace.Types;

namespace Services;

public static class ReferenceDataCurrent
{
    public static EventDateTime EndOfToday() =>
        EventDateTimeBuilder.Create(AggregateMaintenanceDateCalculator.EndOfDay(DateTime.Now));
}
