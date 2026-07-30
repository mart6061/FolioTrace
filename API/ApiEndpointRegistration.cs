using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using API.FoleoTrader;
using API.TradeFiles;
using Repository;
using Services;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace API;

public static partial class ApiEndpointRegistration
{
    private static readonly JsonSerializerOptions NotificationJsonOptions = new(JsonSerializerDefaults.Web);

    private const string AccountEventsRoute = "/API/Events/Account";
    private const string BrokerEventsRoute = "/API/Events/Broker";
    private const string CountryEventsRoute = "/API/Events/Country";
    private const string CurrencyEventsRoute = "/API/Events/Currency";
    private const string FXEventsRoute = "/API/Events/FX";
    private const string FXRateEventsRoute = "/API/Events/FXRate";
    private const string HoldingEventsRoute = "/API/Events/Holding";
    private const string InstrumentEventsRoute = "/API/Events/Instrument";
    private const string InstrumentPriceEventsRoute = "/API/Events/InstrumentPrice";
    private const string InstrumentIncomeEventsRoute = "/API/Events/InstrumentIncome";
    private const string TransactionEventsRoute = "/API/Events/Transaction";
    private const string TicketEventsRoute = "/API/Events/Ticket";
    private const string UserEventsRoute = "/API/Events/User";
    private const string UserMenuPreferencesEventsRoute = "/API/Events/UserMenuPreferences";
    private const string UserValuationPreferencesEventsRoute = "/API/Events/UserValuationPreferences";
    private const string UserBookmarksEventsRoute = "/API/Events/UserBookmarks";
    private const string InputControlSettingsEventsRoute = "/API/Events/InputControlSettings";
    private const string ValuationSettingEventsRoute = "/API/Events/ValuationSetting";
    private const string AssetAllocationMappingEventsRoute = "/API/Events/AssetAllocationMapping";
    private const string ReportEventsRoute = "/API/Events/Report";

    public static WebApplication MapFolioTraceApi(this WebApplication app)
    {
        var api = app.MapGroup("");

        api.MapSystemHealthEndpoint();
        api.MapTradeFileCallbackEndpoints();

        var applicationApi = api.MapGroup("")
            .AddEndpointFilter<UserConsistencyEndpointFilter>();

        applicationApi.MapDiagnosticsEndpoints();
        applicationApi.MapNotificationEndpoints();
        applicationApi.MapSystemEndpoints();
        applicationApi.MapAccountEndpoints();
        applicationApi.MapBrokerEndpoints();
        applicationApi.MapCountryEndpoints();
        applicationApi.MapCurrencyEndpoints();
        applicationApi.MapFXEndpoints();
        applicationApi.MapFXRateEndpoints();
        applicationApi.MapFoleoTraderEndpoints();
        applicationApi.MapHoldingEndpoints();
        applicationApi.MapValuationEndpoints();
        applicationApi.MapProfitLossEndpoints();
        applicationApi.MapValuationSettingEndpoints();
        applicationApi.MapAssetAllocationMappingEndpoints();
        applicationApi.MapReportConfigEndpoints();
        applicationApi.MapInstrumentEndpoints();
        applicationApi.MapInstrumentValueEndpoints();
        applicationApi.MapTicketEndpoints();
        applicationApi.MapTradeFileEndpoints();
        applicationApi.MapUserEndpoints();
        applicationApi.MapUserMenuPreferencesEndpoints();
        applicationApi.MapUserValuationPreferencesEndpoints();
        applicationApi.MapUserBookmarksEndpoints();
        applicationApi.MapInputControlSettingsEndpoints();
        applicationApi.MapInputPolicyEndpoints();
        applicationApi.MapAccountEventEndpoints();
        applicationApi.MapBrokerEventEndpoints();
        applicationApi.MapCountryEventEndpoints();
        applicationApi.MapCurrencyEventEndpoints();
        applicationApi.MapFXEventEndpoints();
        applicationApi.MapFXRateEventEndpoints();
        applicationApi.MapHoldingEventEndpoints();
        applicationApi.MapInstrumentEventEndpoints();
        applicationApi.MapInstrumentPriceEventEndpoints();
        applicationApi.MapInstrumentIncomeEventEndpoints();
        applicationApi.MapTransactionEventEndpoints();
        applicationApi.MapTicketEventEndpoints();
        applicationApi.MapUserEventEndpoints();
        applicationApi.MapUserMenuPreferencesEventEndpoints();
        applicationApi.MapUserValuationPreferencesEventEndpoints();
        applicationApi.MapUserBookmarksEventEndpoints();
        applicationApi.MapInputControlSettingsEventEndpoints();
        applicationApi.MapValuationSettingEventEndpoints();
        applicationApi.MapAssetAllocationMappingEventEndpoints();
        applicationApi.MapReportEventEndpoints();

        return app;
    }
}
