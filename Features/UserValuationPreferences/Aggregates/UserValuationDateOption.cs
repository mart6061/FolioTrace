using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<UserValuationDateOption>))]
public enum UserValuationDateOption
{
    Now,
    TodayEndOfDay,
    YesterdayEndOfDay,
    LastWeekEndOfDay,
    LastMonthEndOfDay,
    LastQuarterEndOfDay
}
