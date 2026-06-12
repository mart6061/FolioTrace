using API;
using API.FoleoTrader;
using Microsoft.Extensions.Logging;
using Repository;
using Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<BuildCoordinator>();
builder.Services.Configure<FoleoTraderOptions>(builder.Configuration.GetSection(FoleoTraderOptions.SectionName));
builder.Services.AddSingleton<FoleoTraderOrderProcessor>();
builder.Services.AddSingleton<FoleoTraderFIXOperationRecorder>();
builder.Services.AddSingleton<FoleoTraderFixClient>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<FoleoTraderFixClient>());
builder.Services.AddSingleton(
    builder.Configuration
        .GetSection(AggregateMaintenanceOptions.SectionName)
        .Get<AggregateMaintenanceOptions>() ?? new AggregateMaintenanceOptions());
builder.Services.AddFolioTraceRepository(builder.Configuration);
builder.Services.AddFolioTraceServices();
builder.Services.AddApiObservability(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<AggregateMaintenanceHostedService>();

var app = builder.Build();

app.UsePathBase("/API");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v0/swagger.json", "FolioTrace API v0");
        options.RoutePrefix = "swagger";
    });
    app.UseHttpsRedirection();
}

app.UseApiRequestLogging();
app.UseApiExchangeCapture();
app.UseApiUnhandledExceptionLogging();

app.MapHealthChecks("/health");
app.MapFolioTraceApi();

app.Run();
