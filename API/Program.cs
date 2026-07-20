using API;
using API.Auth;
using API.FoleoTrader;
using API.TradeFiles;
using Microsoft.AspNetCore.Authentication.Cookies;
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
builder.Services.Configure<WorkOSAuthOptions>(builder.Configuration.GetSection(WorkOSAuthOptions.SectionName));
var workOSAuthOptions = builder.Configuration
    .GetSection(WorkOSAuthOptions.SectionName)
    .Get<WorkOSAuthOptions>() ?? new WorkOSAuthOptions();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = workOSAuthOptions.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Math.Max(5, workOSAuthOptions.SessionLifetimeMinutes));
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
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
builder.Services.Configure<FoleoTraderOptions>(builder.Configuration.GetSection(FoleoTraderOptions.SectionName));
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
builder.Services.AddFolioTraceRepository(builder.Configuration);
builder.Services.AddFolioTraceServices();
builder.Services.Configure<RequestTraceOptions>(builder.Configuration.GetSection(RequestTraceOptions.SectionName));
builder.Services.AddSingleton<RequestTraceSettingsService>();
builder.Services.AddSingleton<RequestTraceLogQueue>();
builder.Services.AddHostedService<RequestTraceLogBackgroundService>();
builder.Services.AddSingleton<IWorkOSAuthKitClient, WorkOSAuthKitClient>();
builder.Services.AddSingleton<IWorkOSSsoClient>(sp => (IWorkOSSsoClient)sp.GetRequiredService<IWorkOSAuthKitClient>());
builder.Services.AddSingleton<WorkOSAuthorizationStateService>();
builder.Services.AddSingleton<FolioTraceUserIdentityService>();
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
app.UseAuthentication();
app.UseAuthorization();

app.MapFolioTraceApi();

app.Run();
