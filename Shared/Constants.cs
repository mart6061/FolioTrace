using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace;

public static class Constants
{
    public static class Initialisation
    {
        public const string Reason = "Setup";

        public static readonly Guid CountriesStreamId = Guid.Parse("6b9326f8-5d39-4f56-9817-b86cf8ed1e5a");

        public static readonly EventDateTime EventDateTime = EventDateTimeBuilder.Create(DateTime.MinValue.AddTicks(1));

        public static readonly AuditDateTime AuditDateTime = AuditDateTimeBuilder.Create(DateTime.MinValue.AddTicks(1));
    }

    public static class Valuation
    {
        [UIDetails(Short: "Today", Long: "End of today", Order: 1)]
        public static readonly EventDateTime Today = EventDateTimeBuilder.Create(DateTime.Now.AddDays(1).AddTicks(-1));

        [UIDetails(Short: "Month End", Long: "End of the current calendar month", Order: 2)]
        public static readonly EventDateTime MonthEnd = EventDateTimeBuilder.Create(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddTicks(-1));

        [UIDetails(Short: "Last Month End", Long: "End of the previous calendar month", Order: 3)]
        public static readonly EventDateTime LastMonthEnd = EventDateTimeBuilder.Create(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddTicks(-1));

        [UIDetails(Short: "Last Year End", Long: "End of the previous calendar year", Order: 4)]
        public static readonly EventDateTime LastYearEnd = EventDateTimeBuilder.Create(new DateTime(DateTime.Now.Year, 1, 1).AddTicks(-1));

        [UIDetails(Short: "Last Financial Year End", Long: "End of the previous financial year ending 31 March", Order: 5)]
        public static readonly EventDateTime LastFinancialYearEnd = EventDateTimeBuilder.Create(DateTime.Now.Date > new DateTime(DateTime.Now.Year, 3, 31) ? new DateTime(DateTime.Now.Year, 4, 1).AddTicks(-1) : new DateTime(DateTime.Now.Year - 1, 4, 1).AddTicks(-1));
    }
}
