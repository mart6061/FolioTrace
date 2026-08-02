using API;
using API.FoleoTrader;
using API.TradeFiles;
using Microsoft.Extensions.Logging;
using Repository;
using Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId |
        ActivityTrackingOptions.Baggage |
        ActivityTrackingOptions.Tags;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v0", new()
    {
        Title = "FolioTrace API",
        Version = "v0"
    });
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});
builder.Services.AddSingleton<ApiVersionInfo>();
builder.Services.Configure<ApiReadinessOptions>(builder.Configuration.GetSection(ApiReadinessOptions.SectionName));
builder.Services.AddSingleton<ApiReadinessState>();
builder.Services.AddSingleton<FixStartupHealthState>();
builder.Services.AddHostedService<EventStoreStartupHostedService>();
builder.Services.AddSingleton<BuildCoordinator>();
builder.Services.Configure<FoleoTraderConnectionOptions>(builder.Configuration.GetSection(FoleoTraderConnectionOptions.SectionName));
builder.Services.AddSingleton<FoleoTraderOrderProcessor>();
builder.Services.AddSingleton<FoleoTraderFIXOperationRecorder>();
builder.Services.AddSingleton<FoleoTraderFixClient>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<FoleoTraderFixClient>());
builder.Services.Configure<TradeFileOptions>(builder.Configuration.GetSection(TradeFileOptions.SectionName));
builder.Services.AddSingleton<TradeFileWorkbookGenerator>();
builder.Services.AddHttpClient<ITradeFileSender, FoleoTraderTradeFileSender>();
builder.Services.AddSingleton<TradeFileWorkflowService>();
builder.Services.AddHostedService<TradeFileProcessingHostedService>();
builder.Services.AddSingleton(
    builder.Configuration
        .GetSection(AggregateMaintenanceOptions.SectionName)
        .Get<AggregateMaintenanceOptions>() ?? new AggregateMaintenanceOptions());
builder.Services.AddSingleton(
    builder.Configuration
        .GetSection(HoldingPositionSnapshotVerificationOptions.SectionName)
        .Get<HoldingPositionSnapshotVerificationOptions>() ?? new HoldingPositionSnapshotVerificationOptions());
builder.Services.AddSingleton(
    builder.Configuration
        .GetSection(ProfitLossSnapshotVerificationOptions.SectionName)
        .Get<ProfitLossSnapshotVerificationOptions>() ?? new ProfitLossSnapshotVerificationOptions());
builder.Services.AddFolioTraceRepository(builder.Configuration);
builder.Services.AddFolioTraceServices();
builder.Services.Configure<RequestTraceOptions>(builder.Configuration.GetSection(RequestTraceOptions.SectionName));
builder.Services.AddSingleton<RequestTraceSettingsService>();
builder.Services.AddSingleton<RequestTraceLogQueue>();
builder.Services.AddHostedService<RequestTraceLogBackgroundService>();
builder.Services.AddSingleton<ICurrentUserContext, FixedCurrentUserContext>();
builder.AddApiObservability();
builder.Services.AddHostedService<AggregateMaintenanceHostedService>();

var app = builder.Build();

AggregateCacheInvalidatorCompletenessCheck.Validate(app.Services);

app.UsePathBase("/API");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v0/swagger.json", "FolioTrace API v0");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseApiUnhandledExceptionLogging();
app.UseRequestTraceCapture();
app.UseApiRequestLogging();
app.UseMiddleware<ApiReadinessMiddleware>();

app.MapFolioTraceApi();

app.Run();
