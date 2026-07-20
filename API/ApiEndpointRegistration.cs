using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using API.Auth;
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

        api.MapAuthEndpoints();
        api.MapSystemHealthEndpoint();
        api.MapTradeFileCallbackEndpoints();

        var protectedApi = api.MapGroup("")
            .RequireAuthorization()
            .AddEndpointFilter<UserConsistencyEndpointFilter>();

        protectedApi.MapDiagnosticsEndpoints();
        protectedApi.MapNotificationEndpoints();
        protectedApi.MapSystemEndpoints();
        protectedApi.MapAccountEndpoints();
        protectedApi.MapBrokerEndpoints();
        protectedApi.MapCountryEndpoints();
        protectedApi.MapCurrencyEndpoints();
        protectedApi.MapFXEndpoints();
        protectedApi.MapFXRateEndpoints();
        protectedApi.MapFoleoTraderEndpoints();
        protectedApi.MapHoldingEndpoints();
        protectedApi.MapValuationEndpoints();
        protectedApi.MapProfitLossEndpoints();
        protectedApi.MapValuationSettingEndpoints();
        protectedApi.MapAssetAllocationMappingEndpoints();
        protectedApi.MapReportConfigEndpoints();
        protectedApi.MapInstrumentEndpoints();
        protectedApi.MapInstrumentValueEndpoints();
        protectedApi.MapTicketEndpoints();
        protectedApi.MapTradeFileEndpoints();
        protectedApi.MapUserEndpoints();
        protectedApi.MapUserMenuPreferencesEndpoints();
        protectedApi.MapUserValuationPreferencesEndpoints();
        protectedApi.MapUserBookmarksEndpoints();
        protectedApi.MapInputControlSettingsEndpoints();
        protectedApi.MapInputPolicyEndpoints();
        protectedApi.MapAccountEventEndpoints();
        protectedApi.MapBrokerEventEndpoints();
        protectedApi.MapCountryEventEndpoints();
        protectedApi.MapCurrencyEventEndpoints();
        protectedApi.MapFXEventEndpoints();
        protectedApi.MapFXRateEventEndpoints();
        protectedApi.MapHoldingEventEndpoints();
        protectedApi.MapInstrumentEventEndpoints();
        protectedApi.MapInstrumentPriceEventEndpoints();
        protectedApi.MapInstrumentIncomeEventEndpoints();
        protectedApi.MapTransactionEventEndpoints();
        protectedApi.MapTicketEventEndpoints();
        protectedApi.MapUserEventEndpoints();
        protectedApi.MapUserMenuPreferencesEventEndpoints();
        protectedApi.MapUserValuationPreferencesEventEndpoints();
        protectedApi.MapUserBookmarksEventEndpoints();
        protectedApi.MapInputControlSettingsEventEndpoints();
        protectedApi.MapValuationSettingEventEndpoints();
        protectedApi.MapAssetAllocationMappingEventEndpoints();
        protectedApi.MapReportEventEndpoints();

        return app;
    }
}
