using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace;

public static class Constants
{
    public static class Initialisation
    {
        public const string Reason = "Setup";

        public static readonly UserID UserID = Guid.Parse("334f6bb3-762d-4d10-9752-f913d75f7c6c");

        public static readonly Guid AccountsStreamId = Guid.Parse("425f51c3-6886-4bfe-a7a9-f34de60bd916");

        public static readonly Guid CountriesStreamId = Guid.Parse("6b9326f8-5d39-4f56-9817-b86cf8ed1e5a");

        public static readonly Guid CurrenciesStreamId = Guid.Parse("dc34cb31-f7f8-4c0d-88b1-f6f60413bc55");

        public static readonly Guid FXsStreamId = Guid.Parse("87487872-101c-4fec-bf37-0ddd9a7efb47");

        public static readonly Guid FXRatesStreamId = Guid.Parse("f7a242f1-96ad-4328-a903-670a78c8b2f4");

        public static readonly Guid HoldingsStreamId = Guid.Parse("44a970fc-bd46-4cd8-96a2-6eb023b0e5c6");

        public static readonly Guid InstrumentsStreamId = Guid.Parse("ba970d3e-fad9-4fc6-9d42-432630f3dcb0");

        public static readonly Guid InstrumentPricesStreamId = Guid.Parse("d8612e8c-e954-4a95-a8d3-7fd85d4a4d20");

        public static readonly Guid InstrumentIncomesStreamId = Guid.Parse("af774bcb-5e34-4b4c-87a5-ecf0ba14e783");

        public static readonly Guid TransactionsStreamId = Guid.Parse("9e6cc40d-df4f-4e6a-aa0e-d624635bcf68");

        public static readonly Guid TicketsStreamId = Guid.Parse("f389de8b-08e0-4e05-8b46-76f37f0dfb72");

        public static readonly Guid UsersStreamId = Guid.Parse("02ff99c6-dfe9-4e61-aa02-bd1ca3a540a2");

        public static readonly Guid UserMenuPreferencesStreamId = Guid.Parse("a1868c31-31ab-4d0b-93f4-77dfc7b8e2b5");

        public static readonly Guid UserValuationPreferencesStreamId = Guid.Parse("f680675f-7966-4a7f-bc61-4c1d846455f1");

        public static readonly Guid UserBookmarksStreamId = Guid.Parse("4f8e4ab1-7cc9-43f8-b11b-ec8932f1b8b7");

        public static readonly EventID EmptyViewEventID = Guid.Parse("11111111-1111-4111-8111-111111111111");

        public static readonly EventDateTime EventDateTime = EventDateTimeBuilder.Create(DateTime.MinValue.AddTicks(1));

        public static readonly AuditDateTime AuditDateTime = AuditDateTimeBuilder.Create(DateTime.MinValue.AddTicks(1));
    }


    public static class Valuation
    {
        [UIDetails(Short: "Today", Long: "End of today", Order: 1)]
        public static readonly EventDateTime Today = EventDateTimeBuilder.Create(DateTime.Now.AddDays(1).AddTicks(-1));

        [UIDetails(Short: "Yesterday", Long: "Yesterday or last working day", Order: 2)]
        public static readonly EventDateTime Yesterday = EventDateTimeBuilder.Create(DateTime.Now.Date.AddDays(DateTime.Now.DayOfWeek == DayOfWeek.Monday ? -2 : 0).AddTicks(-1));

        [UIDetails(Short: "Month End", Long: "End of the current calendar month", Order: 3)]
        public static readonly EventDateTime MonthEnd = EventDateTimeBuilder.Create(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddTicks(-1));

        [UIDetails(Short: "Last Month End", Long: "End of the previous calendar month", Order: 4)]
        public static readonly EventDateTime LastMonthEnd = EventDateTimeBuilder.Create(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddTicks(-1));

        [UIDetails(Short: "Last Year End", Long: "End of the previous calendar year", Order: 5)]
        public static readonly EventDateTime LastYearEnd = EventDateTimeBuilder.Create(new DateTime(DateTime.Now.Year, 1, 1).AddTicks(-1));

        [UIDetails(Short: "Last Financial Year End", Long: "End of the previous financial year ending 31 March", Order: 6)]
        public static readonly EventDateTime LastFinancialYearEnd = EventDateTimeBuilder.Create(DateTime.Now.Date > new DateTime(DateTime.Now.Year, 3, 31) ? new DateTime(DateTime.Now.Year, 4, 1).AddTicks(-1) : new DateTime(DateTime.Now.Year - 1, 4, 1).AddTicks(-1));
    }
}
